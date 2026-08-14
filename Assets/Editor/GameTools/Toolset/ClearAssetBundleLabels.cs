using UnityEditor;
using UnityEngine;

public class ClearAssetBundleLabels : EditorWindow
{
    private string folderPath = "Assets";

    [MenuItem("SDK/Game Tools/Clear Asset Bundle Names")]
    public static void ShowWindow()
    {
        GetWindow<ClearAssetBundleLabels>("Clear Asset Bundle Names");
    }

    private void OnGUI()
    {
        GUILayout.Label("Clear Asset Bundle Names Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Selected Folder:", folderPath);

        if (GUILayout.Button("Select Folder"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel(
                "Select Folder",
                Application.dataPath,
                ""
            );
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Folder",
                        "Please select a folder inside the Assets directory.",
                        "OK"
                    );
                }
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear Asset Bundle Names"))
        {
            if (
                EditorUtility.DisplayDialog(
                    "Confirm",
                    "This will clear all Asset Bundle names in the selected folder. Proceed?",
                    "Yes",
                    "No"
                )
            )
            {
                ClearAssetBundleNamesInFolder(folderPath);
            }
        }
    }

    // Public static accessor
    public static void ClearAssetBundleNamesInFolder(string path)
    {
        string[] assetPaths = AssetDatabase.FindAssets("", new[] { path });

        int clearedCount = 0;

        foreach (string assetGUID in assetPaths)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
            {
                importer.assetBundleName = string.Empty;
                clearedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Done",
            $"Cleared Asset Bundle names for {clearedCount} assets in {path}.",
            "OK"
        );
    }
}
