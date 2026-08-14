using System.Collections.Generic;
using System.Reflection;
using Diz.Skinning;
using EFT.Visual;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

public class GameReadyCharacterArmsCreatorEditor : EditorWindow
{
    private int numItemsToCreate = 1;
    private GameObject[] mainGameObjects;
    private List<GameObject[]> skinGameObjectsList;
    private int[] numSkinsPerItem;
    private Preset defaultSkinPreset;
    private Vector2 scrollPosition;

    [MenuItem("SDK/Game Tools/First Person Character Arms Creator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GameReadyCharacterArmsCreatorEditor));
    }

    private void OnEnable()
    {
        mainGameObjects = new GameObject[numItemsToCreate];
        skinGameObjectsList = new List<GameObject[]>(numItemsToCreate);
        numSkinsPerItem = new int[numItemsToCreate];

        for (int i = 0; i < numItemsToCreate; i++)
        {
            skinGameObjectsList.Add(new GameObject[0]);
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Enter the number of items you want to create:");
        numItemsToCreate = EditorGUILayout.IntField("Number of Items", numItemsToCreate);

        if (mainGameObjects.Length != numItemsToCreate)
        {
            mainGameObjects = new GameObject[numItemsToCreate];
            skinGameObjectsList.Clear();
            numSkinsPerItem = new int[numItemsToCreate];

            for (int i = 0; i < numItemsToCreate; i++)
            {
                skinGameObjectsList.Add(new GameObject[0]);
            }
        }

        GUILayout.Label("Select a Global Skin Preset:");
        defaultSkinPreset =
            EditorGUILayout.ObjectField(
                "Global Skin Preset",
                defaultSkinPreset,
                typeof(Preset),
                false
            ) as Preset;

        for (int i = 0; i < numItemsToCreate; i++)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Item " + (i + 1));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            mainGameObjects[i] = (GameObject)
                EditorGUILayout.ObjectField(
                    "Gameobject_MAIN",
                    mainGameObjects[i],
                    typeof(GameObject),
                    true
                );

            if (GUILayout.Button("Select Skin GameObjects"))
            {
                SelectSkinGameObjects(i);
            }

            int numSkins = skinGameObjectsList[i].Length;
            GUILayout.Label("Number of Skins: " + numSkins);

            EditorGUILayout.EndVertical();
        }

        EditorGUI.BeginDisabledGroup(!AllGameObjectsSet());
        if (GUILayout.Button("Create Game Ready Character Arms"))
        {
            CreateGameReadyCharacterArms(
                numItemsToCreate,
                mainGameObjects,
                skinGameObjectsList,
                defaultSkinPreset
            );
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();
    }

    private bool AllGameObjectsSet()
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            if (
                mainGameObjects[i] == null
                || skinGameObjectsList[i] == null
                || skinGameObjectsList[i].Length == 0
            )
            {
                return false;
            }

            for (int j = 0; j < skinGameObjectsList[i].Length; j++)
            {
                if (skinGameObjectsList[i][j] == null)
                    return false;
            }
        }

        return true;
    }

    private void SelectSkinGameObjects(int index)
    {
        GameObject[] selectedGameObjects = Selection.gameObjects;
        if (selectedGameObjects == null || selectedGameObjects.Length == 0)
        {
            Debug.LogError("No GameObjects selected.");
            return;
        }

        List<GameObject> skinGameObjects = new List<GameObject>();
        foreach (GameObject gameObject in selectedGameObjects)
        {
            if (gameObject != mainGameObjects[index])
            {
                skinGameObjects.Add(gameObject);
            }
        }

        skinGameObjectsList[index] = skinGameObjects.ToArray();
        numSkinsPerItem[index] = skinGameObjects.Count;
    }

    // Public static accessor
    public static void CreateGameReadyCharacterArms(
        int numItemsToCreate,
        GameObject[] mainGameObjects,
        List<GameObject[]> skinGameObjectsList,
        Preset defaultSkinPreset
    )
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            GameObject mainGameObject = mainGameObjects[i];
            GameObject[] skinGameObjects = skinGameObjectsList[i];

            if (
                PrefabUtility.GetPrefabInstanceStatus(mainGameObject)
                == PrefabInstanceStatus.Connected
            )
            {
                PrefabUtility.UnpackPrefabInstance(
                    mainGameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction
                );
            }

            LoddedSkin loddedSkinComponent = mainGameObject.AddComponent<LoddedSkin>();
            List<Skin> lodsList = new List<Skin>();

            for (int j = 0; j < skinGameObjects.Length; j++)
            {
                GameObject skinGameObject = skinGameObjects[j];

                SkinnedMeshRenderer skinnedMeshRenderer =
                    skinGameObject.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer == null)
                {
                    Debug.LogError(
                        "SkinnedMeshRenderer not found on the Skin GameObject: "
                            + skinGameObject.name
                    );
                    continue;
                }

                Skin skinComponent = skinGameObject.AddComponent<Skin>();
                if (defaultSkinPreset != null)
                {
                    defaultSkinPreset.ApplyTo(skinComponent);
                }
                else
                {
                    Debug.LogError("Global Skin Preset not set.");
                }

                FieldInfo rendererField = typeof(Skin).GetField(
                    "_skinnedMeshRenderer",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                rendererField.SetValue(skinComponent, skinnedMeshRenderer);

                lodsList.Add(skinGameObject.GetComponent<Skin>());

                HotObject hotObjectComponent = skinGameObject.AddComponent<HotObject>();
                skinGameObject.AddComponent<RainCondensator>();
                hotObjectComponent.Temperature = new Vector3(0.7f, 1f, 4f);

                skinGameObject.transform.SetParent(mainGameObject.transform);
            }

            FieldInfo lodsField = typeof(LoddedSkin).GetField(
                "_lods",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            lodsField.SetValue(loddedSkinComponent, lodsList.ToArray());
        }

        EditorUtility.DisplayDialog(
            "GameReady Character Arms Created",
            "The GameReady Character Arms have been created successfully!",
            "OK"
        );
    }
}
