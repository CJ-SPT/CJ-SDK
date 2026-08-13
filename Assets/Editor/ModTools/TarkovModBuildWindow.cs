using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using UnityEditor;
using UnityEngine;

namespace TarkovSdk.Editor
{
    /// <summary>
    /// Builds independent runtime exports for every mod asmdef under Assets/Mods.
    /// Each project chooses its own AssetBundles and produces its own managed DLL.
    /// </summary>
    public sealed class TarkovModBuildWindow : EditorWindow
    {
        private const string SdkAssemblyPrefix = "Tarkov.Assembly";
        private const string GameAssemblyPrefix = "Assembly-CSharp";
        private const string DefaultOutputPath = "Build/Tarkov";
        private const string CommonAssemblyName = "CJ.ModSdk";
        private const string CommonAssemblyGuid = "98490199017413d428cede408b179cec";
        private const string OutputPathKey = "TarkovSdk.Build.OutputPath";
        private const string SelectedProjectKey = "TarkovSdk.Build.SelectedProject";
        private const string BundleSelectionKeyPrefix = "TarkovSdk.Build.Bundles.";

        private sealed class ModProject
        {
            public string AssemblyName;
            public string AsmdefAssetPath;
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string name;
            public string[] includePlatforms;
        }

        private string _outputPath = DefaultOutputPath;
        private Vector2 _scroll;
        private Vector2 _bundleScroll;
        private readonly List<string> _log = new List<string>();
        private readonly List<ModProject> _projects = new List<ModProject>();
        private readonly HashSet<string> _selectedBundles = new HashSet<string>(
            StringComparer.Ordinal
        );
        private int _selectedProjectIndex;

        [MenuItem("Mod Tools/Mod Export")]
        public static void ShowWindow()
        {
            TarkovModBuildWindow window = GetWindow<TarkovModBuildWindow>("Tarkov Mod Export");
            window.minSize = new Vector2(650f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            _outputPath = EditorPrefs.GetString(OutputPathKey, DefaultOutputPath);
            RefreshProjects();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                "Runtime-ready Tarkov project export",
                EditorStyles.boldLabel
            );
            EditorGUILayout.HelpBox(
                "Each asmdef below is a separate mod project. It gets its own compiled DLL, "
                    + "AssetBundle selection, and output directory. SDK assembly aliases are remapped "
                    + "to the names used by EFT. CJ.ModSdk.dll is shared at the output root.",
                MessageType.Info
            );

            DrawProjectSelector();
            DrawOutputSelector();
            DrawBundleSelector();

            EditorGUILayout.Space(8f);
            using (
                new EditorGUI.DisabledScope(
                    EditorApplication.isCompiling
                        || string.IsNullOrWhiteSpace(_outputPath)
                        || CurrentProject == null
                )
            )
            {
                if (GUILayout.Button("Build Selected Project", GUILayout.Height(34f)))
                {
                    BuildExport(CurrentProject);
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            using (
                EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(
                    _scroll,
                    GUILayout.ExpandHeight(true)
                )
            )
            {
                _scroll = scroll.scrollPosition;
                foreach (string line in _log)
                {
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private void DrawProjectSelector()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Mod project", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_projects.Count == 0)
                {
                    EditorGUILayout.LabelField("No runtime asmdefs found under Assets/Mods.");
                }
                else
                {
                    string[] names = _projects.Select(project => project.AssemblyName).ToArray();
                    int newIndex = EditorGUILayout.Popup(_selectedProjectIndex, names);
                    if (newIndex != _selectedProjectIndex)
                    {
                        SaveBundleSelection();
                        _selectedProjectIndex = newIndex;
                        EditorPrefs.SetString(SelectedProjectKey, CurrentProject.AssemblyName);
                        LoadBundleSelection();
                    }
                }

                if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                {
                    SaveBundleSelection();
                    RefreshProjects();
                }
                if (GUILayout.Button("New Project", GUILayout.Width(92f)))
                {
                    CreateProjectFromCurrentTemplate();
                }
            }

            if (CurrentProject != null)
            {
                EditorGUILayout.LabelField(CurrentProject.AsmdefAssetPath, EditorStyles.miniLabel);
            }
        }

        private void DrawOutputSelector()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Output root", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                string newPath = EditorGUILayout.TextField(_outputPath);
                if (!string.Equals(newPath, _outputPath, StringComparison.Ordinal))
                {
                    _outputPath = newPath;
                    EditorPrefs.SetString(OutputPathKey, _outputPath);
                }

                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    string selected = EditorUtility.OpenFolderPanel(
                        "Select Tarkov exports root folder",
                        ResolveOutputPath(_outputPath),
                        string.Empty
                    );
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _outputPath = selected;
                        EditorPrefs.SetString(OutputPathKey, _outputPath);
                        GUI.FocusControl(null);
                    }
                }
            }

            string resolvedOutput = ResolveOutputPath(_outputPath);
            string preview =
                CurrentProject == null
                    ? resolvedOutput
                    : Path.Combine(resolvedOutput, CurrentProject.AssemblyName);
            EditorGUILayout.LabelField("Mod → " + preview, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "Shared → " + Path.Combine(resolvedOutput, CommonAssemblyName + ".dll"),
                EditorStyles.miniLabel
            );
        }

        private void DrawBundleSelector()
        {
            EditorGUILayout.Space(8f);
            string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    "Project AssetBundles ("
                        + _selectedBundles.Count
                        + "/"
                        + bundleNames.Length
                        + ")",
                    EditorStyles.boldLabel
                );
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("All", GUILayout.Width(44f)))
                {
                    _selectedBundles.Clear();
                    foreach (string bundleName in bundleNames)
                    {
                        _selectedBundles.Add(bundleName);
                    }
                    SaveBundleSelection();
                }
                if (GUILayout.Button("None", GUILayout.Width(48f)))
                {
                    _selectedBundles.Clear();
                    SaveBundleSelection();
                }
            }

            if (bundleNames.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No AssetBundle labels exist. Code-only projects can still be built.",
                    MessageType.None
                );
                return;
            }

            using (
                EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(
                    _bundleScroll,
                    EditorStyles.helpBox,
                    GUILayout.Height(Mathf.Min(120f, 24f + bundleNames.Length * 19f))
                )
            )
            {
                _bundleScroll = scroll.scrollPosition;
                foreach (string bundleName in bundleNames)
                {
                    bool selected = _selectedBundles.Contains(bundleName);
                    bool newSelected = EditorGUILayout.ToggleLeft(bundleName, selected);
                    if (newSelected == selected)
                    {
                        continue;
                    }

                    if (newSelected)
                    {
                        _selectedBundles.Add(bundleName);
                    }
                    else
                    {
                        _selectedBundles.Remove(bundleName);
                    }
                    SaveBundleSelection();
                }
            }
        }

        private ModProject CurrentProject
        {
            get
            {
                return _selectedProjectIndex >= 0 && _selectedProjectIndex < _projects.Count
                    ? _projects[_selectedProjectIndex]
                    : null;
            }
        }

        private void RefreshProjects()
        {
            string previousName =
                CurrentProject != null
                    ? CurrentProject.AssemblyName
                    : EditorPrefs.GetString(SelectedProjectKey, string.Empty);

            _projects.Clear();
            string modsRoot = Path.Combine(Application.dataPath, "Mods");
            if (Directory.Exists(modsRoot))
            {
                foreach (
                    string asmdefPath in Directory.GetFiles(
                        modsRoot,
                        "*.asmdef",
                        SearchOption.AllDirectories
                    )
                )
                {
                    try
                    {
                        AssemblyDefinitionData data = JsonUtility.FromJson<AssemblyDefinitionData>(
                            File.ReadAllText(asmdefPath)
                        );
                        if (
                            data == null
                            || string.IsNullOrWhiteSpace(data.name)
                            || IsEditorOnly(data)
                        )
                        {
                            continue;
                        }

                        _projects.Add(
                            new ModProject
                            {
                                AssemblyName = data.name,
                                AsmdefAssetPath = ToAssetPath(asmdefPath),
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            "Could not read mod asmdef " + asmdefPath + ": " + ex.Message
                        );
                    }
                }
            }

            _projects.Sort(
                (left, right) =>
                    string.Compare(
                        left.AssemblyName,
                        right.AssemblyName,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
            _selectedProjectIndex = Math.Max(
                0,
                _projects.FindIndex(project => project.AssemblyName == previousName)
            );
            if (CurrentProject != null)
            {
                EditorPrefs.SetString(SelectedProjectKey, CurrentProject.AssemblyName);
            }
            LoadBundleSelection();
            Repaint();
        }

        private static bool IsEditorOnly(AssemblyDefinitionData data)
        {
            return data.name.EndsWith(".Editor", StringComparison.OrdinalIgnoreCase)
                || (
                    data.includePlatforms != null
                    && data.includePlatforms.Length == 1
                    && string.Equals(
                        data.includePlatforms[0],
                        "Editor",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }

        private void LoadBundleSelection()
        {
            _selectedBundles.Clear();
            if (CurrentProject == null)
            {
                return;
            }

            string key = BundleSelectionKeyPrefix + CurrentProject.AssemblyName;
            if (!EditorPrefs.HasKey(key))
            {
                // Preserve the original single-project behavior on first use.
                if (_projects.Count == 1)
                {
                    foreach (string bundleName in AssetDatabase.GetAllAssetBundleNames())
                    {
                        _selectedBundles.Add(bundleName);
                    }
                }
                return;
            }

            foreach (
                string bundleName in EditorPrefs
                    .GetString(key, string.Empty)
                    .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            )
            {
                _selectedBundles.Add(bundleName);
            }
        }

        private void SaveBundleSelection()
        {
            if (CurrentProject == null)
            {
                return;
            }

            string[] selected = _selectedBundles
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            EditorPrefs.SetString(
                BundleSelectionKeyPrefix + CurrentProject.AssemblyName,
                string.Join("|", selected)
            );
        }

        private void CreateProjectFromCurrentTemplate()
        {
            if (CurrentProject == null)
            {
                EditorUtility.DisplayDialog(
                    "No template available",
                    "Create or import one mod asmdef under Assets/Mods first.",
                    "OK"
                );
                return;
            }

            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Create Tarkov mod project",
                "MyTarkovMod",
                "asmdef",
                "Choose a unique assembly name and save it in a new folder under Assets/Mods.",
                "Assets/Mods"
            );
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Mods/", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog(
                    "Invalid project location",
                    "Mod projects must be saved below Assets/Mods.",
                    "OK"
                );
                return;
            }

            string assemblyName = Path.GetFileNameWithoutExtension(assetPath);
            if (!Regex.IsMatch(assemblyName, "^[A-Za-z_][A-Za-z0-9_.-]*$"))
            {
                EditorUtility.DisplayDialog(
                    "Invalid assembly name",
                    "Use letters, numbers, dots, underscores, or hyphens, and start with a letter or underscore.",
                    "OK"
                );
                return;
            }

            string template = File.ReadAllText(AssetPathToAbsolute(CurrentProject.AsmdefAssetPath));
            template = Regex.Replace(
                template,
                "(\"name\"\\s*:\\s*)\"[^\"]*\"",
                match => match.Groups[1].Value + "\"" + assemblyName + "\"",
                RegexOptions.None,
                TimeSpan.FromSeconds(1)
            );
            string rootNamespace = Regex.Replace(assemblyName, "[^A-Za-z0-9_.]", "_");
            template = Regex.Replace(
                template,
                "(\"rootNamespace\"\\s*:\\s*)\"[^\"]*\"",
                match => match.Groups[1].Value + "\"" + rootNamespace + "\"",
                RegexOptions.None,
                TimeSpan.FromSeconds(1)
            );
            template = EnsureCommonAssemblyReference(template);

            string absoluteAsmdefPath = AssetPathToAbsolute(assetPath);
            string projectDirectory = Path.GetDirectoryName(absoluteAsmdefPath);
            Directory.CreateDirectory(projectDirectory);
            File.WriteAllText(absoluteAsmdefPath, template);

            string markerPath = Path.Combine(projectDirectory, assemblyName + "AssemblyMarker.cs");
            if (!File.Exists(markerPath))
            {
                File.WriteAllText(
                    markerPath,
                    "namespace "
                        + rootNamespace
                        + "\n{\n    internal static class AssemblyMarker { }\n}\n"
                );
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorPrefs.SetString(SelectedProjectKey, assemblyName);
            RefreshProjects();
            Log("Created project: " + assemblyName + " at " + assetPath);
        }

        private void BuildExport(ModProject project)
        {
            _log.Clear();
            SaveBundleSelection();
            string pluginsOutput = ResolveOutputPath(_outputPath);
            string projectOutput = Path.Combine(pluginsOutput, project.AssemblyName);
            string bundleOutput = Path.Combine(projectOutput, "AssetBundles");
            string managedOutput = Path.Combine(projectOutput, "Managed");

            try
            {
                Directory.CreateDirectory(pluginsOutput);
                Directory.CreateDirectory(bundleOutput);
                Directory.CreateDirectory(managedOutput);

                int patchedBundles = 0;
                int bundleReplacements = 0;
                string[] bundleNames = _selectedBundles
                    .Where(BundleStillExists)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

                if (bundleNames.Length > 0)
                {
                    AssetBundleBuild[] buildMap = bundleNames
                        .Select(bundleName => new AssetBundleBuild
                        {
                            assetBundleName = bundleName,
                            assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName),
                        })
                        .ToArray();

                    Log("Building " + bundleNames.Length + " Windows AssetBundle(s)...");
                    BuildAssetBundleOptions options =
                        BuildAssetBundleOptions.UncompressedAssetBundle
                        | BuildAssetBundleOptions.ForceRebuildAssetBundle;
                    AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                        bundleOutput,
                        buildMap,
                        options,
                        BuildTarget.StandaloneWindows64
                    );
                    if (manifest == null)
                    {
                        throw new InvalidOperationException("Unity's AssetBundle build failed.");
                    }

                    foreach (string bundleName in manifest.GetAllAssetBundles())
                    {
                        string bundlePath = Path.Combine(bundleOutput, bundleName);
                        int replacements = ReplaceEqualLengthAscii(
                            bundlePath,
                            SdkAssemblyPrefix,
                            GameAssemblyPrefix
                        );
                        if (replacements > 0)
                        {
                            patchedBundles++;
                            bundleReplacements += replacements;
                        }
                        Log(
                            "BUNDLE: "
                                + bundleName
                                + " (assembly refs remapped: "
                                + replacements
                                + ")"
                        );
                    }
                }
                else
                {
                    Log("No AssetBundles selected; building code-only project.");
                }

                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string dllName = project.AssemblyName + ".dll";
                string sdkAssemblyPath = Path.Combine(
                    projectRoot,
                    "Library",
                    "ScriptAssemblies",
                    dllName
                );
                string runtimeAssemblyPath = Path.Combine(managedOutput, dllName);
                int managedReferences = RewriteManagedAssemblyForRuntime(
                    sdkAssemblyPath,
                    runtimeAssemblyPath
                );
                Log("DLL: " + dllName + " (assembly refs remapped: " + managedReferences + ")");

                string sourcePdb = Path.ChangeExtension(sdkAssemblyPath, ".pdb");
                if (File.Exists(sourcePdb))
                {
                    File.Copy(
                        sourcePdb,
                        Path.Combine(managedOutput, project.AssemblyName + ".pdb"),
                        true
                    );
                }

                string commonDllName = CommonAssemblyName + ".dll";
                string commonSourcePath = Path.Combine(
                    projectRoot,
                    "Library",
                    "ScriptAssemblies",
                    commonDllName
                );
                string commonRuntimePath = Path.Combine(pluginsOutput, commonDllName);
                int commonManagedReferences = RewriteManagedAssemblyForRuntime(
                    commonSourcePath,
                    commonRuntimePath
                );
                Log(
                    "SHARED: "
                        + commonDllName
                        + " (assembly refs remapped: "
                        + commonManagedReferences
                        + ")"
                );

                string commonSourcePdb = Path.ChangeExtension(commonSourcePath, ".pdb");
                string commonRuntimePdb = Path.ChangeExtension(commonRuntimePath, ".pdb");
                if (File.Exists(commonSourcePdb))
                {
                    File.Copy(commonSourcePdb, commonRuntimePdb, true);
                }
                else
                {
                    DeleteOutputFile(commonRuntimePdb);
                }

                DeleteOutputFile(Path.Combine(managedOutput, commonDllName));
                DeleteOutputFile(Path.Combine(managedOutput, CommonAssemblyName + ".pdb"));

                string deploymentZipPath = CreateDeploymentZip(
                    pluginsOutput,
                    projectOutput,
                    project.AssemblyName,
                    commonRuntimePath
                );

                Log(
                    "DONE: "
                        + project.AssemblyName
                        + " — "
                        + patchedBundles
                        + " bundle(s), "
                        + bundleReplacements
                        + " serialized reference(s), "
                        + managedReferences
                        + " project DLL reference(s), "
                        + commonManagedReferences
                        + " shared DLL reference(s) remapped."
                );
                Log("Mod output: " + projectOutput);
                Log("Shared output: " + commonRuntimePath);
                Log("Deployment ZIP: " + deploymentZipPath);
                EditorUtility.RevealInFinder(deploymentZipPath);
            }
            catch (Exception ex)
            {
                Log("FAILED: " + ex.GetBaseException().Message);
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Tarkov export failed",
                    ex.GetBaseException().Message,
                    "OK"
                );
            }
        }

        private static bool BundleStillExists(string bundleName)
        {
            return AssetDatabase.GetAllAssetBundleNames().Contains(bundleName);
        }

        private static string EnsureCommonAssemblyReference(string asmdefJson)
        {
            string commonReference = "GUID:" + CommonAssemblyGuid;
            if (asmdefJson.Contains("\"" + commonReference + "\""))
            {
                return asmdefJson;
            }

            const string referencesPattern = "(\"references\"\\s*:\\s*\\[)(?<items>.*?)(\\])";
            return Regex.Replace(
                asmdefJson,
                referencesPattern,
                match =>
                {
                    string items = match.Groups["items"].Value.Trim();
                    string replacementItems = string.IsNullOrEmpty(items)
                        ? "\n        \"" + commonReference + "\"\n    "
                        : "\n        \"" + commonReference + "\",\n        " + items + "\n    ";
                    return match.Groups[1].Value + replacementItems + match.Groups[3].Value;
                },
                RegexOptions.Singleline,
                TimeSpan.FromSeconds(1)
            );
        }

        private static int RewriteManagedAssemblyForRuntime(string sourcePath, string outputPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    "The compiled mod assembly was not found. Let Unity finish compiling first.",
                    sourcePath
                );
            }

            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            using (ModuleDefMD module = ModuleDefMD.Load(sourceBytes))
            {
                int changes = 0;
                foreach (AssemblyRef assemblyRef in module.GetAssemblyRefs())
                {
                    string name = assemblyRef.Name;
                    if (name.StartsWith(SdkAssemblyPrefix, StringComparison.Ordinal))
                    {
                        assemblyRef.Name =
                            GameAssemblyPrefix + name.Substring(SdkAssemblyPrefix.Length);
                        changes++;
                    }
                }

                ModuleWriterOptions writerOptions = new ModuleWriterOptions(module);
                writerOptions.MetadataOptions.Flags =
                    MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
                module.Write(outputPath, writerOptions);
                return changes;
            }
        }

        private static void DeleteOutputFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string CreateDeploymentZip(
            string pluginsOutput,
            string projectOutput,
            string projectAssemblyName,
            string commonRuntimePath
        )
        {
            string zipPath = Path.Combine(pluginsOutput, projectAssemblyName + ".zip");
            DeleteOutputFile(zipPath);

            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                const string pluginEntryRoot = "BepInEx/plugins/";
                AddFileToZip(
                    archive,
                    commonRuntimePath,
                    pluginEntryRoot + Path.GetFileName(commonRuntimePath)
                );

                string commonRuntimePdb = Path.ChangeExtension(commonRuntimePath, ".pdb");
                if (File.Exists(commonRuntimePdb))
                {
                    AddFileToZip(
                        archive,
                        commonRuntimePdb,
                        pluginEntryRoot + Path.GetFileName(commonRuntimePdb)
                    );
                }

                string normalizedProjectRoot =
                    Path.GetFullPath(projectOutput)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                foreach (
                    string filePath in Directory
                        .GetFiles(projectOutput, "*", SearchOption.AllDirectories)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                )
                {
                    string normalizedFilePath = Path.GetFullPath(filePath);
                    if (
                        !normalizedFilePath.StartsWith(
                            normalizedProjectRoot,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        throw new InvalidOperationException(
                            "Cannot package a file outside the selected mod output: " + filePath
                        );
                    }

                    string relativePath = normalizedFilePath.Substring(
                        normalizedProjectRoot.Length
                    );
                    string entryPath =
                        pluginEntryRoot
                        + projectAssemblyName
                        + "/"
                        + relativePath.Replace('\\', '/');
                    AddFileToZip(archive, normalizedFilePath, entryPath);
                }
            }

            return zipPath;
        }

        private static void AddFileToZip(ZipArchive archive, string sourcePath, string entryPath)
        {
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("A deployment file was not found.", sourcePath);
            }

            ZipArchiveEntry entry = archive.CreateEntry(
                entryPath,
                System.IO.Compression.CompressionLevel.Optimal
            );
            using (Stream source = File.OpenRead(sourcePath))
            using (Stream destination = entry.Open())
            {
                source.CopyTo(destination);
            }
        }

        private static int ReplaceEqualLengthAscii(string path, string oldValue, string newValue)
        {
            if (oldValue.Length != newValue.Length)
            {
                throw new ArgumentException(
                    "Bundle remapping requires equal-length assembly names."
                );
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Built AssetBundle was not found.", path);
            }

            byte[] bytes = File.ReadAllBytes(path);
            byte[] oldBytes = System.Text.Encoding.ASCII.GetBytes(oldValue);
            byte[] newBytes = System.Text.Encoding.ASCII.GetBytes(newValue);
            int replacements = 0;

            for (int i = 0; i <= bytes.Length - oldBytes.Length; i++)
            {
                bool matches = true;
                for (int j = 0; j < oldBytes.Length; j++)
                {
                    if (bytes[i + j] != oldBytes[j])
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                {
                    continue;
                }

                Buffer.BlockCopy(newBytes, 0, bytes, i, newBytes.Length);
                replacements++;
                i += oldBytes.Length - 1;
            }

            if (replacements > 0)
            {
                File.WriteAllBytes(path, bytes);
            }
            return replacements;
        }

        private static string ToAssetPath(string absolutePath)
        {
            string normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        private static string AssetPathToAbsolute(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static string ResolveOutputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        private void Log(string message)
        {
            _log.Add(message);
            Debug.Log("[TarkovModBuild] " + message);
            _scroll = new Vector2(0f, float.MaxValue);
            Repaint();
        }
    }
}
