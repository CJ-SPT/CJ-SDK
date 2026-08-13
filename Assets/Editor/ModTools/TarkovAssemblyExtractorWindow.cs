using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using UnityEditor;
using UnityEngine;

namespace TarkovSdk.Editor
{
    public class TarkovAssemblyExtractorWindow : EditorWindow
    {
        private const string OldPrefix = "Assembly-CSharp";
        private const string NewPrefix = "Tarkov.Assembly";
        private const string DefaultOutputRelPath = "Assets/Plugins/Tarkov.Assemblies";
        private const string EftDataPathKey = "TarkovSdk.Extractor.EftDataPath";
        private const string SptPluginPathKey = "TarkovSdk.Extractor.SptPluginPath";
        private const string OutputPathKey = "TarkovSdk.Extractor.OutputPath";
        private const string SptOutputFolderName = "SPT";
        private const string UnpackZipFileName = "unpack_after_setup.zip";
        private const string GeneratedMonoScriptTypesName =
            "UnitySourceGeneratedAssemblyMonoScriptTypes_v1";

        private static readonly string[] RequiredManagedDlls = new string[]
        {
            // Do not add UnityEngine.*, Mono.*, Microsoft.CSharp, or framework System.*
            // assemblies here. Unity already provides them, and importing another copy breaks
            // package compilation. System.Runtime.CompilerServices.Unsafe is intentionally kept
            // because the game's Unity.Collections build requires that package assembly.
            "Accessibility.dll",
            "AmplifyMotion.dll",
            "AnimationSystem.Recording.dll",
            "AnimationSystem.Types.dll",
            "Assembly-CSharp-firstpass.dll",
            "Assembly-CSharp.dll",
            "bsg.componentace.compression.libs.zlib.dll",
            "bsg.console.core.dll",
            "bsg.microsoft.extensions.objectpool.dll",
            "bsg.system.buffers.dll",
            "BSG.Unity.Wires.dll",
            "Cinemachine.dll",
            "Coffee.SoftMaskForUGUI.dll",
            "com.nvidia.reflex.Runtime.dll",
            "Comfort.dll",
            "Comfort.Unity.dll",
            "CommonExtensions.dll",
            "DissonanceVoip.dll",
            "DOTween.dll",
            "DOTween.Modules.dll",
            "FbxBuildTestAssets.dll",
            "FilesChecker.dll",
            "ItemComponent.Types.dll",
            "ItemTemplate.Types.dll",
            "JBooth.MicroSplat.Core.dll",
            "kcp.dll",
            "LibraryLoaderUtility.dll",
            "Meta.XR.Audio.dll",
            "Newtonsoft.Json.dll",
            "Newtonsoft.Json.UnityConverters.dll",
            "NLog.dll",
            "Novell.Directory.Ldap.dll",
            "Sirenix.OdinInspector.Attributes.dll",
            "Sirenix.Serialization.Config.dll",
            "Sirenix.Serialization.dll",
            "Sirenix.Utilities.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
            "uLipSync.Runtime.dll",
            "Unity.AI.Navigation.dll",
            "Unity.Burst.dll",
            "Unity.Burst.Unsafe.dll",
            "Unity.Collections.dll",
            "Unity.Collections.LowLevel.ILSupport.dll",
            "Unity.Formats.Fbx.Runtime.dll",
            "Unity.Mathematics.dll",
            "Unity.MemoryProfiler.dll",
            "Unity.PlayableGraphVisualizer.dll",
            "Unity.Postprocessing.Runtime.dll",
            "Unity.ProBuilder.Csg.dll",
            "Unity.ProBuilder.dll",
            "Unity.ProBuilder.KdTree.dll",
            "Unity.ProBuilder.Poly2Tri.dll",
            "Unity.ProBuilder.Stl.dll",
            "Unity.Profiling.Core.dll",
            "Unity.Recorder.Base.dll",
            "Unity.Recorder.dll",
            "Unity.ScriptableBuildPipeline.dll",
            "websocket-sharp.dll",
            "where-allocations.dll",
        };

        private string _eftDataPath = string.Empty;
        private string _sptPluginPath = string.Empty;
        private string _outputPath = DefaultOutputRelPath;
        private Vector2 _logScroll;
        private readonly List<string> _log = new List<string>();
        private bool _lastRunHadFailures;

        [MenuItem("Mod Tools/Assembly Import")]
        public static void ShowWindow()
        {
            TarkovAssemblyExtractorWindow win = GetWindow<TarkovAssemblyExtractorWindow>(
                "Tarkov Assembly Extractor"
            );
            win.minSize = new Vector2(620f, 460f);
            win.Show();
        }

        private void OnEnable()
        {
            _eftDataPath = EditorPrefs.GetString(EftDataPathKey, string.Empty);
            _sptPluginPath = EditorPrefs.GetString(SptPluginPathKey, string.Empty);
            _outputPath = EditorPrefs.GetString(OutputPathKey, DefaultOutputRelPath);

            if (string.IsNullOrEmpty(_sptPluginPath))
            {
                SetDetectedSptPluginPath();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("EscapeFromTarkov_Data folder", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                string newVal = EditorGUILayout.TextField(_eftDataPath);
                if (newVal != _eftDataPath)
                {
                    _eftDataPath = newVal;
                    EditorPrefs.SetString(EftDataPathKey, _eftDataPath);
                }
                if (GUILayout.Button("Browse", GUILayout.Width(90f)))
                {
                    string start = Directory.Exists(_eftDataPath) ? _eftDataPath : string.Empty;
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select EscapeFromTarkov_Data folder",
                        start,
                        string.Empty
                    );
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _eftDataPath = picked.Replace('/', Path.DirectorySeparatorChar);
                        EditorPrefs.SetString(EftDataPathKey, _eftDataPath);
                        SetDetectedSptPluginPath();
                        GUI.FocusControl(null);
                    }
                }
            }

            string managedHint = string.IsNullOrEmpty(_eftDataPath)
                ? "(no folder selected)"
                : (
                    Directory.Exists(Path.Combine(_eftDataPath, "Managed"))
                        ? "✓ Managed subfolder found"
                        : "✗ no Managed subfolder here"
                );
            EditorGUILayout.LabelField(managedHint, EditorStyles.miniLabel);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("SPT plugin folder (optional)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                string newVal = EditorGUILayout.TextField(_sptPluginPath);
                if (newVal != _sptPluginPath)
                {
                    _sptPluginPath = newVal;
                    EditorPrefs.SetString(SptPluginPathKey, _sptPluginPath);
                }
                if (GUILayout.Button("Browse", GUILayout.Width(90f)))
                {
                    string start = Directory.Exists(_sptPluginPath)
                        ? _sptPluginPath
                        : GetDetectedSptPluginPath();
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select BepInEx/plugins/spt folder",
                        Directory.Exists(start) ? start : string.Empty,
                        string.Empty
                    );
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _sptPluginPath = picked.Replace('/', Path.DirectorySeparatorChar);
                        EditorPrefs.SetString(SptPluginPathKey, _sptPluginPath);
                        GUI.FocusControl(null);
                    }
                }
            }

            string[] sptAssemblies = FindSptAssemblyPaths(_sptPluginPath);
            string sptHint;
            if (string.IsNullOrWhiteSpace(_sptPluginPath))
            {
                sptHint = "(not selected; SPT assemblies will be skipped)";
            }
            else if (!Directory.Exists(_sptPluginPath))
            {
                sptHint = "✗ folder not found";
            }
            else
            {
                sptHint =
                    sptAssemblies.Length > 0
                        ? "✓ " + sptAssemblies.Length + " spt-*.dll assemblies found"
                        : "✗ no spt-*.dll assemblies found";
            }
            EditorGUILayout.LabelField(sptHint, EditorStyles.miniLabel);

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField(
                "Output folder (project-relative or absolute)",
                EditorStyles.boldLabel
            );
            string newOut = EditorGUILayout.TextField(_outputPath);
            if (newOut != _outputPath)
            {
                _outputPath = newOut;
                EditorPrefs.SetString(OutputPathKey, _outputPath);
            }
            EditorGUILayout.LabelField(
                "→ " + ResolveOutputAbsolute(_outputPath),
                EditorStyles.miniLabel
            );

            EditorGUILayout.Space(10f);
            using (new EditorGUI.DisabledScope(!CanExtract()))
            {
                if (
                    GUILayout.Button(
                        "Extract & Rewrite ("
                            + RequiredManagedDlls.Length
                            + " game + "
                            + sptAssemblies.Length
                            + " SPT DLLs)",
                        GUILayout.Height(32f)
                    )
                )
                {
                    Run();
                }
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (_log.Count > 0 && GUILayout.Button("Clear", GUILayout.Width(60f)))
                {
                    _log.Clear();
                    _lastRunHadFailures = false;
                }
            }

            using (
                EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(
                    _logScroll,
                    GUILayout.ExpandHeight(true)
                )
            )
            {
                _logScroll = scroll.scrollPosition;
                foreach (string line in _log)
                {
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private bool CanExtract()
        {
            return !string.IsNullOrEmpty(_eftDataPath)
                && Directory.Exists(_eftDataPath)
                && Directory.Exists(Path.Combine(_eftDataPath, "Managed"))
                && !string.IsNullOrEmpty(_outputPath);
        }

        private void Run()
        {
            _log.Clear();
            _lastRunHadFailures = false;

            string managedDir = Path.Combine(_eftDataPath, "Managed");
            string absOutput = ResolveOutputAbsolute(_outputPath);
            string absSptOutput = Path.Combine(absOutput, SptOutputFolderName);
            string[] sptAssemblyPaths = FindSptAssemblyPaths(_sptPluginPath);

            Log("Source: " + managedDir);
            if (sptAssemblyPaths.Length > 0)
            {
                Log("SPT source: " + _sptPluginPath);
            }
            Log("Output: " + absOutput);
            Log(string.Empty);

            try
            {
                Directory.CreateDirectory(absOutput);
                if (sptAssemblyPaths.Length > 0)
                {
                    Directory.CreateDirectory(absSptOutput);
                }
            }
            catch (Exception ex)
            {
                Log("Cannot create output folder: " + ex.Message);
                _lastRunHadFailures = true;
                return;
            }

            int rewritten = 0,
                copied = 0,
                missing = 0,
                failed = 0;
            int unpackNew = 0,
                unpackOverwritten = 0,
                unpackFailed = 0;
            int explicitlyReferenced = 0;
            int updatedAsmdefs = 0;
            bool unpackAttempted = false;
            List<string> missingFiles = new List<string>();
            List<string> failedFiles = new List<string>();

            bool inAssets = absOutput
                .Replace('\\', '/')
                .StartsWith(
                    Application.dataPath.Replace('\\', '/'),
                    StringComparison.OrdinalIgnoreCase
                );

            try
            {
                if (inAssets)
                {
                    AssetDatabase.StartAssetEditing();
                }

                for (int i = 0; i < RequiredManagedDlls.Length; i++)
                {
                    string src = RequiredManagedDlls[i];
                    string srcPath = Path.Combine(managedDir, src);
                    float pct = (float)i / RequiredManagedDlls.Length;

                    if (
                        EditorUtility.DisplayCancelableProgressBar(
                            "Extracting Tarkov assemblies",
                            src + "  (" + (i + 1) + "/" + RequiredManagedDlls.Length + ")",
                            pct
                        )
                    )
                    {
                        Log("Cancelled by user.");
                        return;
                    }

                    if (!File.Exists(srcPath))
                    {
                        Log("MISSING: " + src);
                        missingFiles.Add(src);
                        missing++;
                        continue;
                    }

                    try
                    {
                        ProcessResult r = ProcessOne(
                            srcPath,
                            absOutput,
                            out string outFileName,
                            out int refChanges
                        );
                        switch (r)
                        {
                            case ProcessResult.Rewritten:
                                Log(
                                    "REWRITE: "
                                        + src
                                        + "  → "
                                        + outFileName
                                        + "  (refs updated: "
                                        + refChanges
                                        + ")"
                                );
                                rewritten++;
                                break;
                            case ProcessResult.CopiedAsIs:
                                Log("COPY:    " + src);
                                copied++;
                                break;
                            case ProcessResult.CopiedNonManaged:
                                Log("COPY (native): " + src);
                                copied++;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("FAILED:  " + src + "  -  " + ex.Message);
                        failedFiles.Add(src);
                        failed++;
                    }
                }

                for (int i = 0; i < sptAssemblyPaths.Length; i++)
                {
                    string srcPath = sptAssemblyPaths[i];
                    string src = Path.GetFileName(srcPath);
                    float pct =
                        (float)(RequiredManagedDlls.Length + i)
                        / (RequiredManagedDlls.Length + sptAssemblyPaths.Length);

                    if (
                        EditorUtility.DisplayCancelableProgressBar(
                            "Extracting SPT assemblies",
                            src + "  (" + (i + 1) + "/" + sptAssemblyPaths.Length + ")",
                            pct
                        )
                    )
                    {
                        Log("Cancelled by user.");
                        return;
                    }

                    try
                    {
                        ProcessResult r = ProcessOne(
                            srcPath,
                            absSptOutput,
                            out string outFileName,
                            out int refChanges
                        );
                        if (r == ProcessResult.Rewritten)
                        {
                            Log(
                                "SPT REWRITE: "
                                    + src
                                    + "  → "
                                    + outFileName
                                    + "  (refs updated: "
                                    + refChanges
                                    + ")"
                            );
                            rewritten++;
                        }
                        else
                        {
                            Log("SPT COPY:    " + src);
                            copied++;
                        }

                        CopyDocumentationFile(srcPath, absSptOutput, outFileName);
                    }
                    catch (Exception ex)
                    {
                        Log("SPT FAILED:  " + src + "  -  " + ex.Message);
                        failedFiles.Add(src);
                        failed++;
                    }
                }

                EditorUtility.DisplayProgressBar(
                    "Extracting Tarkov assemblies",
                    "Unpacking " + UnpackZipFileName,
                    1f
                );
                unpackAttempted = TryUnpackAfterSetupZip(
                    out unpackNew,
                    out unpackOverwritten,
                    out unpackFailed
                );
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (inAssets)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    explicitlyReferenced = ConfigureExplicitPluginReferences(absOutput);
                    updatedAsmdefs = SynchronizeAsmdefPrecompiledReferences(absOutput);
                    if (explicitlyReferenced > 0 || updatedAsmdefs > 0)
                    {
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    }
                }
            }

            Log(string.Empty);
            Log(
                "Done. Rewritten: "
                    + rewritten
                    + ", copied: "
                    + copied
                    + ", missing: "
                    + missing
                    + ", failed: "
                    + failed
            );
            if (explicitlyReferenced > 0)
            {
                Log(
                    "Isolated "
                        + explicitlyReferenced
                        + " managed plug-ins; reference them from an asmdef via Override References."
                );
            }
            if (updatedAsmdefs > 0)
            {
                Log(
                    "Updated "
                        + updatedAsmdefs
                        + " SDK/mod asmdef file(s) with generated assembly references."
                );
            }
            if (unpackAttempted)
            {
                Log(
                    "Unpack: "
                        + unpackNew
                        + " new, "
                        + unpackOverwritten
                        + " overwritten"
                        + (unpackFailed > 0 ? ", " + unpackFailed + " failed" : "")
                        + "."
                );
            }

            _lastRunHadFailures = failed > 0 || missing > 0 || unpackFailed > 0;

            if (missingFiles.Count > 0)
            {
                Log("Missing from Managed: " + string.Join(", ", missingFiles));
            }
            if (failedFiles.Count > 0)
            {
                Log("Failed: " + string.Join(", ", failedFiles));
            }
        }

        private enum ProcessResult
        {
            Rewritten,
            CopiedAsIs,
            CopiedNonManaged,
        }

        private static ProcessResult ProcessOne(
            string srcPath,
            string outDir,
            out string outFileName,
            out int refChanges
        )
        {
            byte[] bytes = File.ReadAllBytes(srcPath);
            string srcName = Path.GetFileName(srcPath);
            outFileName = srcName;
            refChanges = 0;

            ModuleDefMD module;
            try
            {
                module = ModuleDefMD.Load(bytes);
            }
            catch (BadImageFormatException)
            {
                File.WriteAllBytes(Path.Combine(outDir, srcName), bytes);
                return ProcessResult.CopiedNonManaged;
            }

            try
            {
                bool changed = false;

                // Unity uses this generated registry to discover MonoBehaviours in precompiled
                // assemblies. Tarkov's obfuscator renames its required Get method, which makes
                // the Editor's MonoScriptInfoScraper crash. Restore that method name so the
                // registry remains usable by Add Component and prefab serialization.
                for (int i = module.Types.Count - 1; i >= 0; i--)
                {
                    if (
                        string.Equals(
                            module.Types[i].Name.String,
                            GeneratedMonoScriptTypesName,
                            StringComparison.Ordinal
                        )
                    )
                    {
                        TypeDef generatedRegistry = module.Types[i];
                        foreach (MethodDef method in generatedRegistry.Methods)
                        {
                            if (
                                method.IsStatic
                                && method.Parameters.Count == 0
                                && method.ReturnType.FullName.EndsWith(
                                    "/MonoScriptData",
                                    StringComparison.Ordinal
                                )
                                && method.Name.String != "Get"
                            )
                            {
                                method.Name = "Get";
                                changed = true;
                                break;
                            }
                        }
                    }
                }

                AssemblyDef assembly = module.Assembly;
                if (assembly != null)
                {
                    string name = assembly.Name;
                    if (name.Contains(OldPrefix))
                    {
                        string newName = name.Replace(OldPrefix, NewPrefix);
                        assembly.Name = newName;
                        module.Name = newName + ".dll";
                        outFileName = newName + ".dll";
                        changed = true;
                    }
                }

                foreach (AssemblyRef aref in module.GetAssemblyRefs())
                {
                    string refName = aref.Name;
                    if (refName.Contains(OldPrefix))
                    {
                        aref.Name = refName.Replace(OldPrefix, NewPrefix);
                        refChanges++;
                        changed = true;
                    }
                }

                // System.Type values inside custom attributes are serialized as assembly-qualified
                // names in the attribute blob. dnlib parses them lazily, so renaming the normal
                // AssemblyRef table above does not update those embedded scopes automatically.
                // A stale RequireComponent(typeof(...)) otherwise produces TypeLoadException during
                // Unity's ResolveRequiredComponents pass.
                int customAttributeChanges = RewriteCustomAttributeTypeReferences(module);
                if (customAttributeChanges > 0)
                {
                    refChanges += customAttributeChanges;
                    changed = true;
                }

                string outPath = Path.Combine(outDir, outFileName);
                if (changed)
                {
                    ModuleWriterOptions opts = new ModuleWriterOptions(module);
                    opts.MetadataOptions.Flags =
                        MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
                    module.Write(outPath, opts);
                    return ProcessResult.Rewritten;
                }

                File.WriteAllBytes(outPath, bytes);
                return ProcessResult.CopiedAsIs;
            }
            finally
            {
                module.Dispose();
            }
        }

        private static int RewriteCustomAttributeTypeReferences(ModuleDefMD module)
        {
            AssemblyRef firstPassReference = null;
            foreach (AssemblyRef assemblyRef in module.GetAssemblyRefs())
            {
                if (
                    string.Equals(
                        assemblyRef.Name.String,
                        NewPrefix + "-firstpass",
                        StringComparison.Ordinal
                    )
                )
                {
                    firstPassReference = assemblyRef;
                    break;
                }
            }

            if (firstPassReference == null)
            {
                return 0;
            }

            int changes = 0;
            changes += RewriteCustomAttributes(module, firstPassReference);
            if (module.Assembly != null)
            {
                changes += RewriteCustomAttributes(module.Assembly, firstPassReference);
            }

            foreach (TypeDef type in module.GetTypes())
            {
                changes += RewriteCustomAttributes(type, firstPassReference);
                foreach (GenericParam genericParameter in type.GenericParameters)
                {
                    changes += RewriteCustomAttributes(genericParameter, firstPassReference);
                }

                foreach (MethodDef method in type.Methods)
                {
                    changes += RewriteCustomAttributes(method, firstPassReference);
                    foreach (ParamDef parameter in method.ParamDefs)
                    {
                        changes += RewriteCustomAttributes(parameter, firstPassReference);
                    }
                    foreach (GenericParam genericParameter in method.GenericParameters)
                    {
                        changes += RewriteCustomAttributes(genericParameter, firstPassReference);
                    }
                }
                foreach (FieldDef field in type.Fields)
                {
                    changes += RewriteCustomAttributes(field, firstPassReference);
                }
                foreach (PropertyDef property in type.Properties)
                {
                    changes += RewriteCustomAttributes(property, firstPassReference);
                }
                foreach (EventDef eventDefinition in type.Events)
                {
                    changes += RewriteCustomAttributes(eventDefinition, firstPassReference);
                }
            }

            return changes;
        }

        private static int RewriteCustomAttributes(
            IHasCustomAttribute provider,
            AssemblyRef firstPassReference
        )
        {
            int changes = 0;
            foreach (CustomAttribute attribute in provider.CustomAttributes)
            {
                foreach (CAArgument argument in attribute.ConstructorArguments)
                {
                    changes += RewriteCustomAttributeArgument(argument, firstPassReference);
                }
                foreach (CANamedArgument namedArgument in attribute.NamedArguments)
                {
                    changes += RewriteCustomAttributeArgument(
                        namedArgument.Argument,
                        firstPassReference
                    );
                }
            }
            return changes;
        }

        private static int RewriteCustomAttributeArgument(
            CAArgument argument,
            AssemblyRef firstPassReference
        )
        {
            int changes = RewriteCustomAttributeTypeSig(argument.Type, firstPassReference);

            TypeSig typeValue = argument.Value as TypeSig;
            if (typeValue != null)
            {
                changes += RewriteCustomAttributeTypeSig(typeValue, firstPassReference);
            }

            IList<CAArgument> arrayValue = argument.Value as IList<CAArgument>;
            if (arrayValue != null)
            {
                foreach (CAArgument item in arrayValue)
                {
                    changes += RewriteCustomAttributeArgument(item, firstPassReference);
                }
            }

            return changes;
        }

        private static int RewriteCustomAttributeTypeSig(
            TypeSig typeSignature,
            AssemblyRef firstPassReference
        )
        {
            int changes = 0;
            for (TypeSig current = typeSignature; current != null; current = current.Next)
            {
                TypeRef typeReference = current.ScopeType as TypeRef;
                AssemblyRef currentAssembly =
                    typeReference != null ? typeReference.ResolutionScope as AssemblyRef : null;
                if (
                    currentAssembly != null
                    && string.Equals(
                        currentAssembly.Name.String,
                        OldPrefix + "-firstpass",
                        StringComparison.Ordinal
                    )
                )
                {
                    typeReference.ResolutionScope = firstPassReference;
                    changes++;
                }

                GenericInstSig genericInstance = current as GenericInstSig;
                if (genericInstance != null)
                {
                    foreach (TypeSig genericArgument in genericInstance.GenericArguments)
                    {
                        changes += RewriteCustomAttributeTypeSig(
                            genericArgument,
                            firstPassReference
                        );
                    }
                }
            }
            return changes;
        }

        private string GetDetectedSptPluginPath()
        {
            if (string.IsNullOrWhiteSpace(_eftDataPath))
            {
                return string.Empty;
            }

            DirectoryInfo dataDirectory = new DirectoryInfo(_eftDataPath);
            if (dataDirectory.Parent == null)
            {
                return string.Empty;
            }

            return Path.Combine(dataDirectory.Parent.FullName, "BepInEx", "plugins", "spt");
        }

        private void SetDetectedSptPluginPath()
        {
            string detected = GetDetectedSptPluginPath();
            if (!Directory.Exists(detected))
            {
                return;
            }

            _sptPluginPath = detected;
            EditorPrefs.SetString(SptPluginPathKey, _sptPluginPath);
        }

        private static string[] FindSptAssemblyPaths(string sourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            {
                return new string[0];
            }

            string[] paths = Directory.GetFiles(
                sourceDirectory,
                "spt-*.dll",
                SearchOption.AllDirectories
            );
            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        private static void CopyDocumentationFile(
            string sourceAssemblyPath,
            string outputDirectory,
            string outputAssemblyName
        )
        {
            string sourceXml = Path.ChangeExtension(sourceAssemblyPath, ".xml");
            if (!File.Exists(sourceXml))
            {
                return;
            }

            string outputXmlName = Path.ChangeExtension(outputAssemblyName, ".xml");
            File.Copy(sourceXml, Path.Combine(outputDirectory, outputXmlName), true);
        }

        private static int SynchronizeAsmdefPrecompiledReferences(string sptOutputDirectory)
        {
            if (!Directory.Exists(sptOutputDirectory))
            {
                return 0;
            }

            HashSet<string> sptDllNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (
                string dllPath in Directory.GetFiles(
                    sptOutputDirectory,
                    "spt-*.dll",
                    SearchOption.AllDirectories
                )
            )
            {
                sptDllNames.Add(Path.GetFileName(dllPath));
            }
            if (sptDllNames.Count == 0)
            {
                return 0;
            }

            List<string> asmdefPaths = new List<string>();
            string commonAsmdef = Path.Combine(
                Application.dataPath,
                "CJ.ModSdk",
                "CJ.ModSdk.asmdef"
            );
            if (File.Exists(commonAsmdef))
            {
                asmdefPaths.Add(commonAsmdef);
            }

            string modsDirectory = Path.Combine(Application.dataPath, "Mods");
            if (Directory.Exists(modsDirectory))
            {
                asmdefPaths.AddRange(
                    Directory.GetFiles(modsDirectory, "*.asmdef", SearchOption.AllDirectories)
                );
            }

            List<string> sortedDllNames = new List<string>(sptDllNames);
            sortedDllNames.Sort(StringComparer.OrdinalIgnoreCase);
            int updated = 0;
            foreach (string asmdefPath in asmdefPaths)
            {
                string json = File.ReadAllText(asmdefPath);
                if (
                    !Regex.IsMatch(
                        json,
                        "\\\"overrideReferences\\\"\\s*:\\s*true",
                        RegexOptions.IgnoreCase,
                        TimeSpan.FromSeconds(1)
                    )
                )
                {
                    continue;
                }

                Match match = Regex.Match(
                    json,
                    "(\\\"precompiledReferences\\\"\\s*:\\s*\\[)(?<items>.*?)(\\])",
                    RegexOptions.Singleline,
                    TimeSpan.FromSeconds(1)
                );
                if (!match.Success)
                {
                    continue;
                }

                string existingItems = match.Groups["items"].Value;
                List<string> missing = new List<string>();
                foreach (string dllName in sortedDllNames)
                {
                    if (
                        !Regex.IsMatch(
                            existingItems,
                            "\\\"" + Regex.Escape(dllName) + "\\\"",
                            RegexOptions.IgnoreCase,
                            TimeSpan.FromSeconds(1)
                        )
                    )
                    {
                        missing.Add("\"" + dllName + "\"");
                    }
                }
                if (missing.Count == 0)
                {
                    continue;
                }

                string existingTrimmed = existingItems.Trim();
                string replacementItems = string.IsNullOrEmpty(existingTrimmed)
                    ? "\n        " + string.Join(",\n        ", missing.ToArray()) + "\n    "
                    : "\n        "
                        + existingTrimmed
                        + ",\n        "
                        + string.Join(",\n        ", missing.ToArray())
                        + "\n    ";
                Group itemsGroup = match.Groups["items"];
                string updatedJson =
                    json.Substring(0, itemsGroup.Index)
                    + replacementItems
                    + json.Substring(itemsGroup.Index + itemsGroup.Length);
                File.WriteAllText(asmdefPath, updatedJson);
                updated++;
            }

            return updated;
        }

        private static int ConfigureExplicitPluginReferences(string absOutput)
        {
            // Unity 2022.3 keeps this importer flag internal even though it is serialized in meta files.
            string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            int changed = 0;
            PropertyInfo explicitlyReferencedProperty = typeof(PluginImporter).GetProperty(
                "IsExplicitlyReferenced",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (explicitlyReferencedProperty == null)
            {
                throw new MissingMemberException(
                    typeof(PluginImporter).FullName,
                    "IsExplicitlyReferenced"
                );
            }

            foreach (
                string dllPath in Directory.GetFiles(
                    absOutput,
                    "*.dll",
                    SearchOption.AllDirectories
                )
            )
            {
                string normalizedDllPath = Path.GetFullPath(dllPath).Replace('\\', '/');
                if (
                    !normalizedDllPath.StartsWith(
                        dataPath + "/",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                string assetPath = "Assets" + normalizedDllPath.Substring(dataPath.Length);
                PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer == null || (bool)explicitlyReferencedProperty.GetValue(importer, null))
                {
                    continue;
                }

                explicitlyReferencedProperty.SetValue(importer, true, null);
                if (AssetDatabase.WriteImportSettingsIfDirty(assetPath))
                {
                    changed++;
                }
            }

            return changed;
        }

        private bool TryUnpackAfterSetupZip(out int newFiles, out int overwritten, out int failed)
        {
            newFiles = 0;
            overwritten = 0;
            failed = 0;

            string zipPath = ResolveScriptSiblingPath(UnpackZipFileName);
            if (!File.Exists(zipPath))
            {
                Log(string.Empty);
                Log("No " + UnpackZipFileName + " next to script; skipping unpack.");
                Log("Looked at: " + zipPath);
                return false;
            }

            Log(string.Empty);
            Log("Unpacking " + zipPath + " into Assets/...");

            string destRoot = Application.dataPath;
            string normalizedRoot =
                Path.GetFullPath(destRoot).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string outPath = Path.GetFullPath(Path.Combine(destRoot, rel));

                        if (!outPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            Log("UNPACK SKIP (outside Assets): " + entry.FullName);
                            failed++;
                            continue;
                        }

                        try
                        {
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(outPath);
                                continue;
                            }

                            string parentDir = Path.GetDirectoryName(outPath);
                            if (!string.IsNullOrEmpty(parentDir))
                            {
                                Directory.CreateDirectory(parentDir);
                            }

                            bool existed = File.Exists(outPath);
                            entry.ExtractToFile(outPath, overwrite: true);
                            if (existed)
                            {
                                overwritten++;
                                Log("UNPACK OVERWRITE: " + entry.FullName);
                            }
                            else
                            {
                                newFiles++;
                                Log("UNPACK NEW:       " + entry.FullName);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("UNPACK FAILED: " + entry.FullName + "  -  " + ex.Message);
                            failed++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Unpack failed to open " + UnpackZipFileName + ": " + ex.Message);
                failed++;
            }

            return true;
        }

        private string ResolveScriptSiblingPath(string fileName)
        {
            MonoScript ms = MonoScript.FromScriptableObject(this);
            string scriptAssetPath = ms != null ? AssetDatabase.GetAssetPath(ms) : null;

            if (string.IsNullOrEmpty(scriptAssetPath))
            {
                return Path.GetFullPath(
                    Path.Combine(Application.dataPath, "Editor", "SDK2", fileName)
                );
            }

            string scriptDirRelative = Path.GetDirectoryName(scriptAssetPath);
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, scriptDirRelative, fileName));
        }

        private static string ResolveOutputAbsolute(string maybeRel)
        {
            if (string.IsNullOrEmpty(maybeRel))
            {
                return string.Empty;
            }
            if (Path.IsPathRooted(maybeRel))
            {
                return Path.GetFullPath(maybeRel);
            }
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, maybeRel));
        }

        private void Log(string line)
        {
            _log.Add(line);
            if (!string.IsNullOrEmpty(line))
            {
                Debug.Log("[TarkovAssemblyExtractor] " + line);
            }
            _logScroll = new Vector2(0f, float.MaxValue);
            Repaint();
        }
    }
}
