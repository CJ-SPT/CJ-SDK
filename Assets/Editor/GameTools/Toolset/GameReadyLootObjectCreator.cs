using EFT.Visual;
using UnityEditor;
using UnityEngine;

public class GameReadyLootItemEditor : EditorWindow
{
    [MenuItem("SDK/Game Tools/Create GameReady Loot Object(s)")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GameReadyLootItemEditor));
    }

    private void OnGUI()
    {
        GUILayout.Label(
            "Click the button to turn selected GameObjects into GameReady loot objects:"
        );

        EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0);
        if (GUILayout.Button("Create GameReady Loot Object(s)"))
        {
            CreateGameReadyLootObjects();
        }
        EditorGUI.EndDisabledGroup();
    }

    // Public static so GUI toolbox can call it directly
    public static void CreateGameReadyLootObjects()
    {
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            if (selectedObject == null)
                continue;

            GameObject mainGameObject = new GameObject(selectedObject.name);
            mainGameObject.transform.position = Vector3.zero;

            selectedObject.transform.SetParent(mainGameObject.transform, true);

            MeshCollider meshCollider = selectedObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = selectedObject.AddComponent<MeshCollider>();
            }
            meshCollider.convex = true;

            if (mainGameObject.GetComponent<PreviewPivot>() == null)
            {
                mainGameObject.AddComponent<PreviewPivot>();
            }
        }

        EditorUtility.DisplayDialog(
            "GameReady Loot Objects Created",
            "The GameReady loot objects have been created successfully for the selected GameObjects!",
            "OK"
        );
    }
}
