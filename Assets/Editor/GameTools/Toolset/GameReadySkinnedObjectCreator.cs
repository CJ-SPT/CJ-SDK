using System.Collections.Generic;
using System.Reflection;
using Diz.Skinning;
using EFT.Visual;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

public class GameReadySkinnedObjectCreatorEditor : EditorWindow
{
    private int numItemsToCreate = 1;
    private GameObject[] mainGameObjects;
    private List<GameObject[]> skinGameObjects;
    private List<GameObject[]> lootGameObjects;
    private Preset[] skinPresets;
    private int selectedSkinCount;
    private int selectedLootCount;

    private CustomGUIToolboxEditor.SkinnedObjectType[] skinTypes;

    private Mesh[][] backpackMeshes;

    private Mesh[][] vestMeshes;

    [MenuItem("SDK/Game Tools/Skinned Object Creator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GameReadySkinnedObjectCreatorEditor));
    }

    private void OnEnable()
    {
        InitializeArrays();
    }

    private void InitializeArrays()
    {
        mainGameObjects = new GameObject[numItemsToCreate];
        skinGameObjects = new List<GameObject[]>();
        lootGameObjects = new List<GameObject[]>();
        skinPresets = new Preset[numItemsToCreate];
        skinTypes = new CustomGUIToolboxEditor.SkinnedObjectType[numItemsToCreate];
        backpackMeshes = new Mesh[numItemsToCreate][];
        vestMeshes = new Mesh[numItemsToCreate][];

        for (int i = 0; i < numItemsToCreate; i++)
        {
            skinGameObjects.Add(new GameObject[0]);
            lootGameObjects.Add(new GameObject[0]);
            backpackMeshes[i] = new Mesh[4];
            vestMeshes[i] = new Mesh[2];
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter the number of items you want to create:");
        numItemsToCreate = EditorGUILayout.IntField("Number of Items", numItemsToCreate);

        if (mainGameObjects == null || mainGameObjects.Length != numItemsToCreate)
        {
            InitializeArrays();
        }

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

            skinPresets[i] = (Preset)
                EditorGUILayout.ObjectField("Skin Preset", skinPresets[i], typeof(Preset), false);

            EditorGUILayout.Space(5);
            GUILayout.Label("Special Skin Type (Optional):");

            CustomGUIToolboxEditor.SkinnedObjectType previousType = skinTypes[i];
            skinTypes[i] = (CustomGUIToolboxEditor.SkinnedObjectType)
                EditorGUILayout.EnumPopup("Skin Type", skinTypes[i]);

            if (
                previousType == CustomGUIToolboxEditor.SkinnedObjectType.None
                && skinTypes[i] != CustomGUIToolboxEditor.SkinnedObjectType.None
            ) { }
            else if (
                previousType != skinTypes[i]
                && skinTypes[i] != CustomGUIToolboxEditor.SkinnedObjectType.None
            )
            {
                if (previousType != CustomGUIToolboxEditor.SkinnedObjectType.None)
                {
                    if (previousType == CustomGUIToolboxEditor.SkinnedObjectType.Backpack)
                    {
                        backpackMeshes[i] = new Mesh[4];
                    }
                    else if (previousType == CustomGUIToolboxEditor.SkinnedObjectType.Vest)
                    {
                        vestMeshes[i] = new Mesh[2];
                    }
                }
            }

            if (skinTypes[i] == CustomGUIToolboxEditor.SkinnedObjectType.Backpack)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("Backpack Skin Meshes:");
                backpackMeshes[i][0] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Body Mesh",
                        backpackMeshes[i][0],
                        typeof(Mesh),
                        false
                    );
                backpackMeshes[i][1] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Armor Mesh",
                        backpackMeshes[i][1],
                        typeof(Mesh),
                        false
                    );
                backpackMeshes[i][2] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Vest Mesh",
                        backpackMeshes[i][2],
                        typeof(Mesh),
                        false
                    );
                backpackMeshes[i][3] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "ArmorVest Mesh",
                        backpackMeshes[i][3],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
            }
            else if (skinTypes[i] == CustomGUIToolboxEditor.SkinnedObjectType.Vest)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("Vest Skin Meshes:");
                vestMeshes[i][0] = (Mesh)
                    EditorGUILayout.ObjectField("Body Mesh", vestMeshes[i][0], typeof(Mesh), false);
                vestMeshes[i][1] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Armor Mesh",
                        vestMeshes[i][1],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Selected Loot Gameobject(s)"))
        {
            SelectLootGameObjects();
        }

        if (selectedLootCount > 0)
        {
            EditorGUILayout.LabelField("Selected Loot Objects: " + selectedLootCount);
        }

        if (GUILayout.Button("Apply Selected Skin Gameobject(s)"))
        {
            SelectSkinGameObjects();
        }

        if (selectedSkinCount > 0)
        {
            EditorGUILayout.LabelField("Selected Skin Objects: " + selectedSkinCount);
        }

        EditorGUI.BeginDisabledGroup(!AllGameObjectsSet());
        if (GUILayout.Button("Create Gameready Skinned Object(s)"))
        {
            CreateGamereadyObjects(
                numItemsToCreate,
                mainGameObjects,
                skinGameObjects,
                lootGameObjects,
                skinPresets,
                skinTypes,
                backpackMeshes,
                vestMeshes
            );
        }
        EditorGUI.EndDisabledGroup();
    }

    private bool AllGameObjectsSet()
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            if (
                mainGameObjects[i] == null
                || skinPresets[i] == null
                || skinGameObjects[i].Length == 0
                || lootGameObjects[i].Length == 0
            )
            {
                return false;
            }

            if (skinTypes[i] == CustomGUIToolboxEditor.SkinnedObjectType.Backpack)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (backpackMeshes[i][j] == null)
                    {
                        Debug.LogError($"BackpackSkin requires all 4 meshes for item {i + 1}");
                        return false;
                    }
                }
            }
            else if (skinTypes[i] == CustomGUIToolboxEditor.SkinnedObjectType.Vest)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (vestMeshes[i][j] == null)
                    {
                        Debug.LogError($"VestSkin requires both meshes for item {i + 1}");
                        return false;
                    }
                }
            }
        }
        return true;
    }

    private void SelectSkinGameObjects()
    {
        List<GameObject> selectedObjects = new List<GameObject>(Selection.gameObjects);
        if (selectedObjects.Count > 0)
        {
            for (int i = 0; i < numItemsToCreate; i++)
            {
                skinGameObjects[i] = selectedObjects.ToArray();
            }

            selectedSkinCount = selectedObjects.Count;
        }
    }

    private void SelectLootGameObjects()
    {
        List<GameObject> selectedObjects = new List<GameObject>(Selection.gameObjects);
        if (selectedObjects.Count > 0)
        {
            for (int i = 0; i < numItemsToCreate; i++)
            {
                lootGameObjects[i] = selectedObjects.ToArray();
            }

            selectedLootCount = selectedObjects.Count;
        }
    }

    // Public static accessor
    public static void CreateGamereadyObjects(
        int numItemsToCreate,
        GameObject[] mainGameObjects,
        List<GameObject[]> skinGameObjects,
        List<GameObject[]> lootGameObjects,
        Preset[] skinPresets,
        CustomGUIToolboxEditor.SkinnedObjectType[] skinTypes = null,
        Mesh[][] backpackMeshes = null,
        Mesh[][] vestMeshes = null
    )
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            GameObject mainGameObject = mainGameObjects[i];
            GameObject[] skinObjects = skinGameObjects[i];
            GameObject[] lootObjects = lootGameObjects[i];
            Preset skinPreset = skinPresets[i];

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

            mainGameObject.AddComponent<PreviewPivot>();
            DressItem dressItemComponent = mainGameObject.AddComponent<DressItem>();

            List<AbstractSkin> allSkins = new List<AbstractSkin>();

            if (skinPreset != null)
            {
                for (int j = 0; j < skinObjects.Length; j++)
                {
                    GameObject skinObject = skinObjects[j];

                    bool isFirstObject = (j == 0);
                    bool hasSpecialSkin = (
                        skinTypes != null
                        && i < skinTypes.Length
                        && skinTypes[i] != CustomGUIToolboxEditor.SkinnedObjectType.None
                    );

                    if (isFirstObject && hasSpecialSkin)
                    {
                        if (
                            skinTypes[i] == CustomGUIToolboxEditor.SkinnedObjectType.Backpack
                            && backpackMeshes != null
                            && i < backpackMeshes.Length
                        )
                        {
                            Skin skinComponent = skinObject.AddComponent<Skin>();
                            skinPreset.ApplyTo(skinComponent);

                            BackpackSkin backpackSkinComponent =
                                skinObject.AddComponent<BackpackSkin>();

                            FieldInfo skinField = typeof(BackpackSkin).GetField(
                                "Skin",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                            if (skinField != null)
                            {
                                skinField.SetValue(backpackSkinComponent, skinComponent);
                            }

                            BackpackSkinData backpackData = new BackpackSkinData();
                            if (backpackMeshes[i] != null && backpackMeshes[i].Length >= 4)
                            {
                                backpackData.Body = backpackMeshes[i][0];
                                backpackData.Armor = backpackMeshes[i][1];
                                backpackData.Vest = backpackMeshes[i][2];
                                backpackData.ArmorVest = backpackMeshes[i][3];
                            }

                            FieldInfo dataField = typeof(BackpackSkin).GetField(
                                "Data",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                            if (dataField != null)
                            {
                                dataField.SetValue(backpackSkinComponent, backpackData);
                            }

                            allSkins.Add(backpackSkinComponent);
                        }
                        else if (
                            skinTypes[i] == CustomGUIToolboxEditor.SkinnedObjectType.Vest
                            && vestMeshes != null
                            && i < vestMeshes.Length
                        )
                        {
                            Skin skinComponent = skinObject.AddComponent<Skin>();
                            skinPreset.ApplyTo(skinComponent);

                            VestSkin vestSkinComponent = skinObject.AddComponent<VestSkin>();

                            FieldInfo skinField = typeof(VestSkin).GetField(
                                "Skin",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                            if (skinField != null)
                            {
                                skinField.SetValue(vestSkinComponent, skinComponent);
                            }

                            VestSkinData vestData = new VestSkinData();
                            if (vestMeshes[i] != null && vestMeshes[i].Length >= 2)
                            {
                                vestData.Body = vestMeshes[i][0];
                                vestData.Armor = vestMeshes[i][1];
                            }

                            FieldInfo dataField = typeof(VestSkin).GetField(
                                "Data",
                                BindingFlags.Public | BindingFlags.Instance
                            );
                            if (dataField != null)
                            {
                                dataField.SetValue(vestSkinComponent, vestData);
                            }

                            allSkins.Add(vestSkinComponent);
                        }
                    }
                    else
                    {
                        Skin skinComponent = skinObject.AddComponent<Skin>();
                        skinPreset.ApplyTo(skinComponent);

                        allSkins.Add(skinComponent);
                    }

                    SkinnedMeshRenderer skinnedMeshRenderer =
                        skinObject.GetComponent<SkinnedMeshRenderer>();
                    if (skinnedMeshRenderer != null)
                    {
                        if (
                            skinObject.GetComponent<Skin>() != null
                            && !(
                                skinObject.GetComponent<BackpackSkin>() != null
                                || skinObject.GetComponent<VestSkin>() != null
                            )
                        )
                        {
                            FieldInfo rendererField = typeof(Skin).GetField(
                                "_skinnedMeshRenderer",
                                BindingFlags.NonPublic | BindingFlags.Instance
                            );
                            rendererField.SetValue(
                                skinObject.GetComponent<Skin>(),
                                skinnedMeshRenderer
                            );
                        }
                    }
                    else
                    {
                        Debug.LogError("SkinnedMeshRenderer not found on the Skin GameObject.");
                    }
                }
            }

            GameObject loot = new GameObject("Loot");
            GameObject skin = new GameObject("Skin");

            loot.transform.SetParent(mainGameObject.transform);
            skin.transform.SetParent(mainGameObject.transform);

            SkinDress skinDressComponent = skin.AddComponent<SkinDress>();

            FieldInfo lodsField = typeof(SkinDress).GetField(
                "_lods",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            lodsField.SetValue(skinDressComponent, allSkins.ToArray());

            FieldInfo renderersField = typeof(SkinDress).GetField(
                "Renderers",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            List<Renderer> renderersList = new List<Renderer>();
            for (int j = 0; j < skinObjects.Length; j++)
            {
                renderersList.Add(skinObjects[j].GetComponent<Renderer>());
            }
            renderersField.SetValue(skinDressComponent, renderersList.ToArray());

            for (int j = 0; j < skinObjects.Length; j++)
            {
                skinObjects[j].transform.SetParent(skin.transform);
            }

            for (int j = 0; j < lootObjects.Length; j++)
            {
                lootObjects[j].transform.SetParent(loot.transform);
                MeshCollider meshCollider = lootObjects[j].AddComponent<MeshCollider>();
                meshCollider.convex = true;
            }

            dressItemComponent.LootPrefab = loot;
            dressItemComponent.DressPrefab = skin;
        }

        EditorUtility.DisplayDialog(
            "GameReady Skinned Objects Created",
            "The GameReady Skinned objects have been created successfully!",
            "OK"
        );
    }
}
