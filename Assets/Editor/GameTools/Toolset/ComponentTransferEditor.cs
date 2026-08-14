using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class ComponentTransferEditor : EditorWindow
{
    private GameObject sourceObject;
    private GameObject targetObject;
    private bool includeInactive = true;
    private bool preserveExistingComponents = true;

    [MenuItem("SDK/Game Tools/Component Transfer")]
    public static void ShowWindow()
    {
        GetWindow<ComponentTransferEditor>("Component Transfer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Component Transfer Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Copies components from source GameObject hierarchy to target GameObject hierarchy "
                + "when GameObject names match 1:1. Only copies components that don't already exist on the target.\n\n"
                + "Uses Unity's built-in Copy/Paste Component functionality to preserve all values.",
            MessageType.Info
        );

        sourceObject =
            EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true)
            as GameObject;
        targetObject =
            EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true)
            as GameObject;

        GUILayout.Space(10);

        includeInactive = EditorGUILayout.Toggle("Include Inactive Objects", includeInactive);
        preserveExistingComponents = EditorGUILayout.Toggle(
            "Preserve Existing Components",
            preserveExistingComponents
        );

        EditorGUILayout.HelpBox(
            "Transform components are automatically excluded.",
            MessageType.None
        );

        GUILayout.Space(20);

        if (GUILayout.Button("Transfer Components", GUILayout.Height(40)))
        {
            if (sourceObject == null || targetObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Both Source and Target objects must be assigned.",
                    "OK"
                );
                return;
            }

            TransferComponents(
                sourceObject,
                targetObject,
                includeInactive,
                preserveExistingComponents
            );
        }
    }

    // Public static accessor for calling from other scripts
    public static void TransferComponents(
        GameObject source,
        GameObject target,
        bool includeInactive = true,
        bool preserveExistingComponents = true
    )
    {
        if (source == null || target == null)
        {
            EditorUtility.DisplayDialog("Error", "Source and Target cannot be null.", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(target, "Transfer Components");

        // Create dictionaries to handle multiple objects with same name
        Dictionary<string, List<GameObject>> sourceObjectsByName =
            new Dictionary<string, List<GameObject>>();
        Dictionary<string, List<GameObject>> targetObjectsByName =
            new Dictionary<string, List<GameObject>>();

        // Collect all source objects
        CollectAllObjects(source, sourceObjectsByName, includeInactive);

        // Collect all target objects
        CollectAllObjects(target, targetObjectsByName, includeInactive);

        int totalComponentsCopied = 0;
        int totalObjectsProcessed = 0;

        // Process each source object
        foreach (var kvp in sourceObjectsByName)
        {
            string objectName = kvp.Key;
            List<GameObject> sourceObjects = kvp.Value;

            // Check if target has objects with this name
            if (targetObjectsByName.TryGetValue(objectName, out List<GameObject> targetObjects))
            {
                // For simplicity, pair by index if there are multiple with same name
                int pairCount = Mathf.Min(sourceObjects.Count, targetObjects.Count);

                for (int i = 0; i < pairCount; i++)
                {
                    GameObject sourceObj = sourceObjects[i];
                    GameObject targetObj = targetObjects[i];

                    int componentsCopied = CopyComponentsUsingUnityAPI(
                        sourceObj,
                        targetObj,
                        preserveExistingComponents
                    );
                    totalComponentsCopied += componentsCopied;
                    totalObjectsProcessed++;

                    // Mark target object as dirty
                    EditorUtility.SetDirty(targetObj);
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Success",
            $"Processed {totalObjectsProcessed} object pairs.\nCopied {totalComponentsCopied} components.",
            "OK"
        );
    }

    private static void CollectAllObjects(
        GameObject root,
        Dictionary<string, List<GameObject>> dict,
        bool includeInactive
    )
    {
        if (root == null)
            return;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive);
        foreach (Transform t in transforms)
        {
            if (!dict.ContainsKey(t.name))
                dict[t.name] = new List<GameObject>();
            dict[t.name].Add(t.gameObject);
        }
    }

    private static int CopyComponentsUsingUnityAPI(
        GameObject source,
        GameObject target,
        bool preserveExistingComponents
    )
    {
        int componentsCopied = 0;

        // Get all components from source
        Component[] sourceComponents = source.GetComponents<Component>();

        // Get existing component types on target
        HashSet<System.Type> existingTargetTypes = new HashSet<System.Type>();
        if (preserveExistingComponents)
        {
            Component[] targetComponents = target.GetComponents<Component>();
            foreach (Component comp in targetComponents)
            {
                if (comp != null)
                    existingTargetTypes.Add(comp.GetType());
            }
        }

        foreach (Component sourceComponent in sourceComponents)
        {
            if (sourceComponent == null)
                continue;

            System.Type componentType = sourceComponent.GetType();

            // Skip Transform components
            if (componentType == typeof(Transform) || componentType == typeof(RectTransform))
                continue;

            // Skip if component already exists on target and we're preserving existing
            if (preserveExistingComponents && existingTargetTypes.Contains(componentType))
                continue;

            try
            {
                // Use ComponentUtility to copy the component
                if (ComponentUtility.CopyComponent(sourceComponent))
                {
                    // Add the component first if it doesn't exist
                    Component targetComponent = target.GetComponent(componentType);
                    if (targetComponent == null)
                    {
                        targetComponent = target.AddComponent(componentType);
                    }

                    // Paste component values
                    if (ComponentUtility.PasteComponentValues(targetComponent))
                    {
                        componentsCopied++;
                        Debug.Log(
                            $"Copied {componentType.Name} from {source.name} to {target.name}",
                            target
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Failed to paste values for {componentType.Name} from {source.name} to {target.name}"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"Failed to copy component {componentType.Name} from {source.name}"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"Failed to copy component {componentType.Name} from {source.name} to {target.name}: {e.Message}"
                );
            }
        }

        return componentsCopied;
    }
}
