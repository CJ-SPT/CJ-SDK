using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class VoiceTagBankCreator : EditorWindow
{
    private string voiceName = "";
    private string audioRootDirectory = "";

    [MenuItem("SDK/Game Tools/Custom Voice Creator")]
    public static void ShowWindow()
    {
        GetWindow<VoiceTagBankCreator>("Voice TagBank Creator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Create Voice and Tagbanks", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "To use this tool, prepare all your audioclips into a directory and prefix each audioclip name with the EXACT name of the tagbank it is going into. i.e. FriendlyFire_1, or OnEnemyGrenade_excited",
            MessageType.Info
        );

        voiceName = EditorGUILayout.TextField("Voice Name:", voiceName);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Voice Audioclips Directory:");
        audioRootDirectory = EditorGUILayout.TextField(audioRootDirectory);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            audioRootDirectory = EditorUtility.OpenFolderPanel(
                "Select Folder Containing Voice Audioclips",
                "",
                ""
            );
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "Select the directory containing your voice audioclips",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Create Voice Tagbanks"))
        {
            CreateVoiceTagbanks(voiceName, audioRootDirectory);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click to create tagbanks for the voice using the selected audio clips folder. This will automatically export the new voice Tagbanks to Assets/CustomVoices/(your voice name)",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Process Voice Audioclips"))
        {
            ProcessVoiceAudioclips(voiceName, audioRootDirectory);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click to process the selected voice's audio clips and associate them with the appropriate tagbanks.",
            MessageType.Info
        );
    }

    // Public static accessor
    public static void CreateVoiceTagbanks(string voiceName, string audioRootDirectory)
    {
        if (string.IsNullOrEmpty(voiceName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name for the new voice.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(audioRootDirectory))
        {
            EditorUtility.DisplayDialog("Error", "Please select the audio root directory.", "OK");
            return;
        }

        string[] audioFiles = Directory
            .GetFiles(audioRootDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3") || file.EndsWith(".ogg"))
            .ToArray();

        if (audioFiles.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No audio files found in the selected directory.",
                "OK"
            );
            return;
        }

        string voiceFolderPath = $"Assets/CustomVoices/{voiceName}";
        string parentDirectory = System.IO.Path.GetDirectoryName(voiceFolderPath);

        if (!AssetDatabase.IsValidFolder("Assets/CustomVoices"))
        {
            AssetDatabase.CreateFolder("Assets", "CustomVoices");
            AssetDatabase.Refresh();
        }

        if (!AssetDatabase.IsValidFolder(voiceFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/CustomVoices", voiceName);
            AssetDatabase.Refresh();
        }

        List<string> templateTagbankNames = new List<string>();
        string templatesFolder = "Assets/Examples/ExampleVoice/template_full/Tagbanks";
        string[] templatePaths = Directory.GetFiles(templatesFolder, "*.asset");

        foreach (string templatePath in templatePaths)
        {
            string templateName = Path.GetFileNameWithoutExtension(templatePath);
            string[] templateNameParts = templateName.Split('_');
            if (templateNameParts.Length > 0)
            {
                templateTagbankNames.Add(templateNameParts[0]);
            }
        }

        List<string> unprocessedAudioClips = new List<string>();

        foreach (string audioFile in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(audioFile);
            string[] nameParts = fileName.Split('_');
            if (nameParts.Length == 0)
                continue;

            string tagbankName = nameParts[0];

            if (templateTagbankNames.Contains(tagbankName))
            {
                string newTagBankPath = $"{voiceFolderPath}/{tagbankName}.asset";

                TagBank existingTagBank = AssetDatabase.LoadAssetAtPath<TagBank>(newTagBankPath);
                if (existingTagBank == null)
                {
                    string templatePath = templatePaths.FirstOrDefault(path =>
                        Path.GetFileNameWithoutExtension(path).StartsWith(tagbankName)
                    );

                    TagBank templateTagBank = AssetDatabase.LoadAssetAtPath<TagBank>(templatePath);
                    if (templateTagBank != null)
                    {
                        TagBank newTagBank = Object.Instantiate(templateTagBank);
                        AssetDatabase.CreateAsset(newTagBank, newTagBankPath);

                        newTagBank.SpreadGroups = new SpreadGroup[0];
                        newTagBank.Clips = new TaggedClip[0];

                        EditorUtility.SetDirty(newTagBank);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        Debug.LogError($"Template tag bank '{tagbankName}' not found.");
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    $"No matching tagbank template found for audio clip '{audioFile}'."
                );
                unprocessedAudioClips.Add(audioFile);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Tagbanks Created",
            $"Initial Tagbanks created for voice '{voiceName}'.\nUnmatched clips: {unprocessedAudioClips.Count}",
            "OK"
        );
    }

    // Public static accessor
    public static void ProcessVoiceAudioclips(string voiceName, string audioRootDirectory)
    {
        if (string.IsNullOrEmpty(voiceName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name for the new voice.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(audioRootDirectory))
        {
            EditorUtility.DisplayDialog("Error", "Please select the audio root directory.", "OK");
            return;
        }

        string[] audioFiles = Directory
            .GetFiles(audioRootDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".wav") || file.EndsWith(".mp3") || file.EndsWith(".ogg"))
            .ToArray();

        if (audioFiles.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No audio files found in the selected directory.",
                "OK"
            );
            return;
        }

        string voiceFolderPath = $"Assets/CustomVoices/{voiceName}";
        if (!AssetDatabase.IsValidFolder(voiceFolderPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/CustomVoices"))
            {
                AssetDatabase.CreateFolder("Assets", "CustomVoices");
            }

            AssetDatabase.CreateFolder("Assets/CustomVoices", voiceName);
            AssetDatabase.Refresh();
        }

        List<string> templateTagbankNames = new List<string>();
        string templatesFolder = "Assets/Examples/ExampleVoice/template_full/Tagbanks";
        string[] templatePaths = Directory.GetFiles(templatesFolder, "*.asset");

        foreach (string templatePath in templatePaths)
        {
            string templateName = Path.GetFileNameWithoutExtension(templatePath);
            string[] templateNameParts = templateName.Split('_');
            if (templateNameParts.Length > 0)
            {
                templateTagbankNames.Add(templateNameParts[0]);
            }
        }

        List<string> unprocessedAudioClips = new List<string>();

        foreach (string audioFile in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(audioFile);
            string[] nameParts = fileName.Split('_');
            if (nameParts.Length == 0)
                continue;

            string tagbankName = nameParts[0];
            string newTagBankPath = $"{voiceFolderPath}/{tagbankName}.asset";

            TagBank existingTagBank = AssetDatabase.LoadAssetAtPath<TagBank>(newTagBankPath);
            if (existingTagBank != null)
            {
                string relativePath = "Assets" + audioFile.Substring(Application.dataPath.Length);
                AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                if (audioClip != null)
                {
                    bool spreadGroupExists =
                        existingTagBank.SpreadGroups != null
                        && existingTagBank.SpreadGroups.Any(group =>
                            group.Clips != null
                            && group.Clips.Any(taggedClip => taggedClip.Clip == audioClip)
                        );

                    if (!spreadGroupExists)
                    {
                        SpreadGroup newSpreadGroup = new SpreadGroup
                        {
                            Clips = new[]
                            {
                                new TaggedClip
                                {
                                    Clip = audioClip,
                                    Length = audioClip.length,
                                    Mask = 0,
                                    Volume = 100,
                                    Falloff = 50,
                                    Exclude = false,
                                },
                            },
                        };

                        List<SpreadGroup> spreadGroups = new List<SpreadGroup>(
                            existingTagBank.SpreadGroups ?? new SpreadGroup[0]
                        );
                        spreadGroups.Add(newSpreadGroup);
                        existingTagBank.SpreadGroups = spreadGroups.ToArray();

                        EditorUtility.SetDirty(existingTagBank);
                        AssetDatabase.SaveAssets();
                    }

                    bool clipExists =
                        existingTagBank.Clips != null
                        && existingTagBank.Clips.Any(clip => clip.Clip == audioClip);

                    if (!clipExists)
                    {
                        List<TaggedClip> clips = new List<TaggedClip>(
                            existingTagBank.Clips ?? new TaggedClip[0]
                        );
                        clips.Add(
                            new TaggedClip
                            {
                                Clip = audioClip,
                                Length = audioClip.length,
                                Mask = 0,
                                Volume = 100,
                                Falloff = 50,
                                Exclude = false,
                            }
                        );

                        existingTagBank.Clips = clips.ToArray();
                        EditorUtility.SetDirty(existingTagBank);
                        AssetDatabase.SaveAssets();
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load audio clip '{audioFile}'.");
                }
            }
            else
            {
                Debug.LogWarning($"No existing tagbank found for audio clip '{audioFile}'.");
                unprocessedAudioClips.Add(audioFile);
            }
        }

        string voiceTemplatePath =
            "Assets/Examples/ExampleVoice/template_full/Tagbanks/Voice.asset";
        Voice voiceTemplate = AssetDatabase.LoadAssetAtPath<Voice>(voiceTemplatePath);
        if (voiceTemplate != null)
        {
            string newVoicePath = $"{voiceFolderPath}/Voice.asset";
            Voice newVoice = AssetDatabase.LoadAssetAtPath<Voice>(newVoicePath);
            if (newVoice == null)
            {
                newVoice = Object.Instantiate(voiceTemplate);
                AssetDatabase.CreateAsset(newVoice, newVoicePath);
                Debug.Log($"Voice asset created for voice '{voiceName}'.");
            }

            newVoice.Name = voiceName;
            newVoice.Banks = new TagBank[0];

            string[] outputTagBankPaths = Directory.GetFiles(voiceFolderPath, "*.asset");
            foreach (string tagBankPath in outputTagBankPaths)
            {
                TagBank tagBank = AssetDatabase.LoadAssetAtPath<TagBank>(tagBankPath);
                if (tagBank != null)
                {
                    List<TagBank> banks = new List<TagBank>(newVoice.Banks);
                    banks.Add(tagBank);
                    newVoice.Banks = banks.ToArray();
                }
            }

            EditorUtility.SetDirty(newVoice);
            AssetDatabase.SaveAssets();

            Debug.Log($"Tagbanks assigned to voice '{voiceName}'.");
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.Refresh();
            };
        }
        else
        {
            Debug.LogError("Voice template not found.");
        }

        EditorUtility.DisplayDialog(
            "Voice Processed",
            $"Processing complete for voice '{voiceName}'.\nUnprocessed clips: {unprocessedAudioClips.Count}",
            "OK"
        );
    }
}
