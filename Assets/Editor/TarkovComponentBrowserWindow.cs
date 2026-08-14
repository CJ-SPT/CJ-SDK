using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TarkovSdk.Editor
{
    /// <summary>
    /// Lists attachable MonoBehaviours from the isolated Tarkov player assemblies.
    /// Unity's built-in Add Component menu intentionally omits many internal or
    /// menu-hidden game components, so SDK authors need a type-driven browser.
    /// </summary>
    public sealed class TarkovComponentBrowserWindow : EditorWindow
    {
        private const int MaxVisibleResults = 300;

        private sealed class ComponentEntry
        {
            public Type Type;
            public string FullName;
            public string AssemblyName;
        }

        private readonly List<ComponentEntry> _components = new List<ComponentEntry>();
        private readonly List<ComponentEntry> _filtered = new List<ComponentEntry>();
        private Vector2 _scroll;
        private string _search = string.Empty;
        private string _loadMessage = string.Empty;

        [MenuItem("SDK/Component Browser")]
        public static void ShowWindow()
        {
            TarkovComponentBrowserWindow window = GetWindow<TarkovComponentBrowserWindow>(
                "Tarkov Components"
            );
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshTypes();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Tarkov MonoBehaviours", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Width(76f)))
                {
                    RefreshTypes();
                }
            }

            EditorGUILayout.HelpBox(
                "Select a GameObject, search by class or namespace, then add the component. "
                    + "This includes valid Tarkov components that are hidden from Unity's normal Add Component menu.",
                MessageType.Info
            );

            string newSearch = EditorGUILayout.TextField("Search", _search);
            if (!string.Equals(newSearch, _search, StringComparison.Ordinal))
            {
                _search = newSearch;
                ApplyFilter();
            }

            GameObject selected = Selection.activeGameObject;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Target", selected, typeof(GameObject), true);
            }

            if (!string.IsNullOrEmpty(_loadMessage))
            {
                EditorGUILayout.HelpBox(_loadMessage, MessageType.Warning);
            }

            int shown = Math.Min(_filtered.Count, MaxVisibleResults);
            string resultLabel =
                _filtered.Count + " matching / " + _components.Count + " attachable";
            if (_filtered.Count > MaxVisibleResults)
            {
                resultLabel += " (showing first " + MaxVisibleResults + "; refine the search)";
            }
            EditorGUILayout.LabelField(resultLabel, EditorStyles.miniLabel);

            using (
                EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(
                    _scroll,
                    GUILayout.ExpandHeight(true)
                )
            )
            {
                _scroll = scroll.scrollPosition;
                for (int i = 0; i < shown; i++)
                {
                    DrawEntry(_filtered[i], selected);
                }
            }
        }

        private static void DrawEntry(ComponentEntry entry, GameObject selected)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.SelectableLabel(
                        entry.FullName,
                        EditorStyles.label,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)
                    );
                    EditorGUILayout.LabelField(entry.AssemblyName, EditorStyles.miniLabel);
                }

                using (new EditorGUI.DisabledScope(selected == null))
                {
                    if (GUILayout.Button("Add", GUILayout.Width(55f), GUILayout.Height(34f)))
                    {
                        AddComponent(selected, entry.Type);
                    }
                }
            }
        }

        private static void AddComponent(GameObject target, Type componentType)
        {
            try
            {
                Component added = Undo.AddComponent(target, componentType);
                if (added == null)
                {
                    throw new InvalidOperationException("Unity returned no component instance.");
                }

                Selection.activeObject = added;
                EditorGUIUtility.PingObject(added);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Cannot add Tarkov component",
                    componentType.FullName
                        + " could not be added.\n\n"
                        + ex.GetBaseException().Message,
                    "OK"
                );
            }
        }

        private void RefreshTypes()
        {
            _components.Clear();
            _loadMessage = string.Empty;

            List<Assembly> assemblies = AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(IsTarkovAssembly)
                .ToList();

            foreach (
                string assemblyName in new[] { "Tarkov.Assembly", "Tarkov.Assembly-firstpass" }
            )
            {
                if (assemblies.Any(a => a.GetName().Name == assemblyName))
                {
                    continue;
                }

                try
                {
                    Assembly loaded = Assembly.Load(assemblyName);
                    if (loaded != null && IsTarkovAssembly(loaded))
                    {
                        assemblies.Add(loaded);
                    }
                }
                catch (Exception ex)
                {
                    _loadMessage += assemblyName + ": " + ex.GetBaseException().Message + "\n";
                }
            }

            foreach (Assembly assembly in assemblies.Distinct())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (!IsAttachableMonoBehaviour(type))
                    {
                        continue;
                    }

                    _components.Add(
                        new ComponentEntry
                        {
                            Type = type,
                            FullName = type.FullName ?? type.Name,
                            AssemblyName = assembly.GetName().Name,
                        }
                    );
                }
            }

            _components.Sort(
                (left, right) =>
                    string.Compare(
                        left.FullName,
                        right.FullName,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
            ApplyFilter();
            Repaint();
        }

        private void ApplyFilter()
        {
            _filtered.Clear();
            string[] terms = (_search ?? string.Empty).Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (ComponentEntry entry in _components)
            {
                bool matches = true;
                foreach (string term in terms)
                {
                    if (
                        entry.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0
                        && entry.AssemblyName.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0
                    )
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    _filtered.Add(entry);
                }
            }

            _scroll = Vector2.zero;
        }

        private static bool IsTarkovAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            return string.Equals(name, "Tarkov.Assembly", StringComparison.Ordinal)
                || string.Equals(name, "Tarkov.Assembly-firstpass", StringComparison.Ordinal);
        }

        private static bool IsAttachableMonoBehaviour(Type type)
        {
            return type != null
                && type != typeof(MonoBehaviour)
                && typeof(MonoBehaviour).IsAssignableFrom(type)
                && type.IsClass
                && !type.IsAbstract
                && !type.ContainsGenericParameters;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }
    }
}
