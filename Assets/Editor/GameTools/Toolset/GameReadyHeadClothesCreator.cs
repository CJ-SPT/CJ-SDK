using System.Collections.Generic;
using System.Reflection;
using Diz.Skinning;
using EFT.Visual;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

// Define the enum for Clothing/Head skin types
public enum ClothingHeadSkinType
{
    None,
    TorsoSkin,
    HeadSkin,
}

public class GameReadyClothingHeadCreatorEditor : EditorWindow
{
    private int numItemsToCreate = 1;
    private GameObject[] mainGameObjects;
    private List<GameObject[]> skinGameObjectsList;
    private int[] numSkinsPerItem;
    private Preset globalSkinPreset;
    private Vector2 scrollPosition;

    // Changed from bool[] to enum array
    private ClothingHeadSkinType[] skinTypes;

    // Mesh fields for TorsoSkin
    private Mesh[] baseMesh;
    private Mesh[] armorMesh;
    private Mesh[] vestMesh;

    // Mesh fields for HeadSkin
    private Mesh[] headBaseMesh;
    private Mesh[] headFaceCoverMesh;

    [MenuItem("SDK/Game Tools/Clothing and Head Creator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GameReadyClothingHeadCreatorEditor));
    }

    private void OnEnable()
    {
        InitializeArrays();
    }

    private void InitializeArrays()
    {
        mainGameObjects = new GameObject[numItemsToCreate];
        skinGameObjectsList = new List<GameObject[]>(numItemsToCreate);
        numSkinsPerItem = new int[numItemsToCreate];
        skinTypes = new ClothingHeadSkinType[numItemsToCreate];
        baseMesh = new Mesh[numItemsToCreate];
        armorMesh = new Mesh[numItemsToCreate];
        vestMesh = new Mesh[numItemsToCreate];
        headBaseMesh = new Mesh[numItemsToCreate];
        headFaceCoverMesh = new Mesh[numItemsToCreate];

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
            InitializeArrays();
        }

        GUILayout.Label("Select a Global Skin Preset:");
        globalSkinPreset =
            EditorGUILayout.ObjectField(
                "Global Skin Preset",
                globalSkinPreset,
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

            EditorGUILayout.Space(5);
            GUILayout.Label("Special Skin Type (Optional):");

            // Store previous type for cleanup
            ClothingHeadSkinType previousType = skinTypes[i];
            skinTypes[i] = (ClothingHeadSkinType)
                EditorGUILayout.EnumPopup("Skin Type", skinTypes[i]);

            // Clear mesh arrays when switching types
            if (previousType != skinTypes[i] && skinTypes[i] != ClothingHeadSkinType.None)
            {
                if (previousType != ClothingHeadSkinType.None)
                {
                    if (previousType == ClothingHeadSkinType.TorsoSkin)
                    {
                        baseMesh[i] = null;
                        armorMesh[i] = null;
                        vestMesh[i] = null;
                    }
                    else if (previousType == ClothingHeadSkinType.HeadSkin)
                    {
                        headBaseMesh[i] = null;
                        headFaceCoverMesh[i] = null;
                    }
                }
            }

            if (skinTypes[i] == ClothingHeadSkinType.TorsoSkin)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("TorsoSkin Meshes:");
                baseMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField("Base Mesh", baseMesh[i], typeof(Mesh), false);
                armorMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField("Armor Mesh", armorMesh[i], typeof(Mesh), false);
                vestMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField("Vest Mesh", vestMesh[i], typeof(Mesh), false);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            else if (skinTypes[i] == ClothingHeadSkinType.HeadSkin)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("HeadSkin Meshes:");
                headBaseMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField("Base Mesh", headBaseMesh[i], typeof(Mesh), false);
                headFaceCoverMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "FaceCover Mesh",
                        headFaceCoverMesh[i],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }

            if (GUILayout.Button("Select Skin GameObjects"))
            {
                SelectSkinGameObjects(i);
            }

            int numSkins = skinGameObjectsList[i].Length;
            GUILayout.Label("Number of Skins: " + numSkins);

            EditorGUILayout.EndVertical();
        }

        EditorGUI.BeginDisabledGroup(!AllGameObjectsSet());
        if (GUILayout.Button("Create Game Ready Clothes/Head"))
        {
            CreateGameReadyCharacterClothesHead(
                numItemsToCreate,
                mainGameObjects,
                skinGameObjectsList,
                globalSkinPreset,
                skinTypes,
                baseMesh,
                armorMesh,
                vestMesh,
                headBaseMesh,
                headFaceCoverMesh
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

            // Check mesh requirements based on selected skin type
            if (skinTypes[i] == ClothingHeadSkinType.TorsoSkin)
            {
                if (baseMesh[i] == null || armorMesh[i] == null || vestMesh[i] == null)
                {
                    return false;
                }
            }
            else if (skinTypes[i] == ClothingHeadSkinType.HeadSkin)
            {
                if (headBaseMesh[i] == null || headFaceCoverMesh[i] == null)
                {
                    return false;
                }
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

    // Updated static method with HeadSkin support
    public static void CreateGameReadyCharacterClothesHead(
        int numItemsToCreate,
        GameObject[] mainGameObjects,
        List<GameObject[]> skinGameObjectsList,
        Preset globalSkinPreset,
        ClothingHeadSkinType[] skinTypes = null,
        Mesh[] baseMesh = null,
        Mesh[] armorMesh = null,
        Mesh[] vestMesh = null,
        Mesh[] headBaseMesh = null,
        Mesh[] headFaceCoverMesh = null
    )
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            GameObject mainGameObject = mainGameObjects[i];
            GameObject[] skinGameObjects = skinGameObjectsList[i];

            if (mainGameObject == null)
            {
                Debug.LogError("Main GameObject is null for item " + (i + 1));
                continue;
            }

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

            // Handle TorsoSkin or HeadSkin based on selection
            if (
                skinTypes != null
                && i < skinTypes.Length
                && skinTypes[i] != ClothingHeadSkinType.None
            )
            {
                if (skinTypes[i] == ClothingHeadSkinType.TorsoSkin)
                {
                    TorsoSkin torsoSkinComponent = mainGameObject.AddComponent<TorsoSkin>();

                    if (baseMesh != null && i < baseMesh.Length && baseMesh[i] != null)
                    {
                        FieldInfo baseField = typeof(TorsoSkin).GetField(
                            "_base",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                        baseField.SetValue(torsoSkinComponent, baseMesh[i]);
                    }

                    if (armorMesh != null && i < armorMesh.Length && armorMesh[i] != null)
                    {
                        FieldInfo armorField = typeof(TorsoSkin).GetField(
                            "_armor",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                        armorField.SetValue(torsoSkinComponent, armorMesh[i]);
                    }

                    if (vestMesh != null && i < vestMesh.Length && vestMesh[i] != null)
                    {
                        FieldInfo vestField = typeof(TorsoSkin).GetField(
                            "_vest",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                        vestField.SetValue(torsoSkinComponent, vestMesh[i]);
                    }
                }
                else if (skinTypes[i] == ClothingHeadSkinType.HeadSkin)
                {
                    HeadSkin headSkinComponent = mainGameObject.AddComponent<HeadSkin>();

                    // Create and assign HeadSkinData
                    HeadSkinData headData = new HeadSkinData();
                    if (headBaseMesh != null && i < headBaseMesh.Length && headBaseMesh[i] != null)
                    {
                        headData.Base = headBaseMesh[i];
                    }

                    if (
                        headFaceCoverMesh != null
                        && i < headFaceCoverMesh.Length
                        && headFaceCoverMesh[i] != null
                    )
                    {
                        headData.FaceCover = headFaceCoverMesh[i];
                    }

                    // Set the Data field for HeadSkin (inherited from CustomSkin<T>)
                    FieldInfo dataField = typeof(HeadSkin).GetField(
                        "Data",
                        BindingFlags.Public | BindingFlags.Instance
                    );
                    if (dataField != null)
                    {
                        dataField.SetValue(headSkinComponent, headData);
                    }
                }
            }

            // Now add the LoddedSkin component
            LoddedSkin loddedSkinComponent = mainGameObject.AddComponent<LoddedSkin>();
            List<Skin> lodsList = new List<Skin>();

            for (int j = 0; j < skinGameObjects.Length; j++)
            {
                GameObject skinGameObject = skinGameObjects[j];

                if (skinGameObject == null)
                {
                    Debug.LogWarning("Skipping null skin GameObject for item " + (i + 1));
                    continue;
                }

                Skin skinComponent = skinGameObject.AddComponent<Skin>();
                if (globalSkinPreset != null)
                {
                    globalSkinPreset.ApplyTo(skinComponent);
                }
                else
                {
                    Debug.LogError("Global Skin Preset not set.");
                }

                SkinnedMeshRenderer skinnedMeshRenderer =
                    skinGameObject.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    FieldInfo rendererField = typeof(Skin).GetField(
                        "_skinnedMeshRenderer",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    rendererField.SetValue(skinComponent, skinnedMeshRenderer);
                }
                else
                {
                    Debug.LogError("SkinnedMeshRenderer not found on the Skin GameObject.");
                }

                lodsList.Add(skinGameObject.GetComponent<Skin>());
                skinGameObject.transform.SetParent(mainGameObject.transform);
            }

            FieldInfo lodsField = typeof(LoddedSkin).GetField(
                "_lods",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            lodsField.SetValue(loddedSkinComponent, lodsList.ToArray());

            // If TorsoSkin was added, set its _skin field to the first skin
            if (
                skinTypes != null
                && i < skinTypes.Length
                && skinTypes[i] == ClothingHeadSkinType.TorsoSkin
            )
            {
                TorsoSkin torsoSkinComponent = mainGameObject.GetComponent<TorsoSkin>();
                if (torsoSkinComponent != null && lodsList.Count > 0)
                {
                    FieldInfo skinField = typeof(TorsoSkin).GetField(
                        "_skin",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    skinField.SetValue(torsoSkinComponent, lodsList[0]);
                }
            }
            // If HeadSkin was added, set its Skin field to the first skin
            else if (
                skinTypes != null
                && i < skinTypes.Length
                && skinTypes[i] == ClothingHeadSkinType.HeadSkin
            )
            {
                HeadSkin headSkinComponent = mainGameObject.GetComponent<HeadSkin>();
                if (headSkinComponent != null && lodsList.Count > 0)
                {
                    FieldInfo skinField = typeof(HeadSkin).GetField(
                        "Skin",
                        BindingFlags.Public | BindingFlags.Instance
                    );
                    skinField.SetValue(headSkinComponent, lodsList[0]);
                }
            }
        }

        EditorUtility.DisplayDialog(
            "GameReady Clothing/Head Created",
            "The GameReady Clothes/Head have been created successfully!",
            "OK"
        );
    }
}
