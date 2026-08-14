using System.Reflection;
using EFT.Visual;
using UnityEditor;
using UnityEngine;

public class GameReadyDressObjectCreatorEditor : EditorWindow
{
    private int numItemsToCreate = 1;

    private GameObject[] meshGameObjects;
    private bool[] useRainCondensator;
    private bool[] useCustomItem;
    private Mesh[] baseMeshes;
    private Mesh[] customMeshes;
    private EClippingCustoms[] itemTypes;
    private EDecalTextureType[] decalTextureTypes;

    private Vector2 scrollPosition;

    [MenuItem("SDK/Game Tools/Dress Object Creator")]
    public static void ShowWindow()
    {
        GetWindow<GameReadyDressObjectCreatorEditor>("Dress Object Creator");
    }

    private void OnEnable()
    {
        meshGameObjects = new GameObject[numItemsToCreate];
        useRainCondensator = new bool[numItemsToCreate];
        useCustomItem = new bool[numItemsToCreate];
        baseMeshes = new Mesh[numItemsToCreate];
        customMeshes = new Mesh[numItemsToCreate];
        itemTypes = new EClippingCustoms[numItemsToCreate];
        decalTextureTypes = new EDecalTextureType[numItemsToCreate];
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Enter the number of dress items you want to create:");
        int newNum = EditorGUILayout.IntField("Number of Items", numItemsToCreate);
        if (newNum < 1)
            newNum = 1;

        if (newNum != numItemsToCreate)
        {
            numItemsToCreate = newNum;

            System.Array.Resize(ref meshGameObjects, numItemsToCreate);
            System.Array.Resize(ref useRainCondensator, numItemsToCreate);
            System.Array.Resize(ref useCustomItem, numItemsToCreate);
            System.Array.Resize(ref baseMeshes, numItemsToCreate);
            System.Array.Resize(ref customMeshes, numItemsToCreate);
            System.Array.Resize(ref itemTypes, numItemsToCreate);
            System.Array.Resize(ref decalTextureTypes, numItemsToCreate);
        }

        if (meshGameObjects == null || meshGameObjects.Length != numItemsToCreate)
        {
            meshGameObjects ??= new GameObject[numItemsToCreate];
            System.Array.Resize(ref meshGameObjects, numItemsToCreate);
        }
        if (useRainCondensator == null || useRainCondensator.Length != numItemsToCreate)
        {
            useRainCondensator ??= new bool[numItemsToCreate];
            System.Array.Resize(ref useRainCondensator, numItemsToCreate);
        }
        if (useCustomItem == null || useCustomItem.Length != numItemsToCreate)
        {
            useCustomItem ??= new bool[numItemsToCreate];
            System.Array.Resize(ref useCustomItem, numItemsToCreate);
        }
        if (baseMeshes == null || baseMeshes.Length != numItemsToCreate)
        {
            baseMeshes ??= new Mesh[numItemsToCreate];
            System.Array.Resize(ref baseMeshes, numItemsToCreate);
        }
        if (customMeshes == null || customMeshes.Length != numItemsToCreate)
        {
            customMeshes ??= new Mesh[numItemsToCreate];
            System.Array.Resize(ref customMeshes, numItemsToCreate);
        }
        if (itemTypes == null || itemTypes.Length != numItemsToCreate)
        {
            itemTypes ??= new EClippingCustoms[numItemsToCreate];
            System.Array.Resize(ref itemTypes, numItemsToCreate);
        }
        if (decalTextureTypes == null || decalTextureTypes.Length != numItemsToCreate)
        {
            decalTextureTypes ??= new EDecalTextureType[numItemsToCreate];
            System.Array.Resize(ref decalTextureTypes, numItemsToCreate);
        }

        for (int i = 0; i < numItemsToCreate; i++)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Item " + (i + 1), EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            meshGameObjects[i] = (GameObject)
                EditorGUILayout.ObjectField(
                    "Mesh GameObject",
                    meshGameObjects[i],
                    typeof(GameObject),
                    true
                );

            EditorGUILayout.Space();
            decalTextureTypes[i] = (EDecalTextureType)
                EditorGUILayout.EnumPopup("Decal Texture Type", decalTextureTypes[i]);

            EditorGUILayout.Space();
            useRainCondensator[i] = EditorGUILayout.Toggle(
                "Rain Condensator",
                useRainCondensator[i]
            );
            useCustomItem[i] = EditorGUILayout.Toggle("Custom Item", useCustomItem[i]);

            if (useCustomItem[i])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Custom Item Settings", EditorStyles.miniBoldLabel);

                baseMeshes[i] = (Mesh)
                    EditorGUILayout.ObjectField("Base Mesh", baseMeshes[i], typeof(Mesh), false);

                customMeshes[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Custom Mesh",
                        customMeshes[i],
                        typeof(Mesh),
                        false
                    );

                itemTypes[i] = (EClippingCustoms)
                    EditorGUILayout.EnumFlagsField("Item Type", itemTypes[i]);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(!AllDressObjectsSet());
        if (GUILayout.Button("Create Game Ready Dress Object(s)"))
        {
            CreateGameReadyDressObjects(
                numItemsToCreate,
                meshGameObjects,
                useRainCondensator,
                useCustomItem,
                baseMeshes,
                customMeshes,
                itemTypes,
                decalTextureTypes
            );
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndScrollView();
    }

    private bool AllDressObjectsSet()
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            if (meshGameObjects[i] == null)
                return false;

            if (useCustomItem[i])
            {
                if (baseMeshes[i] == null || customMeshes[i] == null)
                    return false;
            }
        }

        return true;
    }

    // Public static accessor
    public static void CreateGameReadyDressObjects(
        int numItemsToCreate,
        GameObject[] meshGameObjects,
        bool[] useRainCondensator,
        bool[] useCustomItem,
        Mesh[] baseMeshes,
        Mesh[] customMeshes,
        EClippingCustoms[] itemTypes,
        EDecalTextureType[] decalTextureTypes
    )
    {
        for (int i = 0; i < numItemsToCreate; i++)
        {
            GameObject selectedObject = meshGameObjects[i];
            if (selectedObject == null)
            {
                Debug.LogWarning($"Skipping null mesh GameObject for item {i + 1}");
                continue;
            }

            GameObject emptyGameObject = new GameObject(selectedObject.name);
            emptyGameObject.transform.position = Vector3.zero;

            selectedObject.transform.SetParent(emptyGameObject.transform, true);

            Dress dressScript = emptyGameObject.AddComponent<Dress>();

            dressScript.DecalTextureType = decalTextureTypes[i];

            emptyGameObject.AddComponent<PreviewPivot>();

            BoxCollider boxCollider = selectedObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = selectedObject.AddComponent<BoxCollider>();
            }

            boxCollider.enabled = false;

            Renderer renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                FieldInfo renderersField = typeof(Dress).GetField(
                    "Renderers",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );

                if (renderersField != null)
                {
                    renderersField.SetValue(dressScript, new Renderer[] { renderer });
                }
            }

            if (useRainCondensator[i])
            {
                emptyGameObject.AddComponent<RainCondensator>();
            }

            if (useCustomItem[i])
            {
                CustomItem customItemScript = emptyGameObject.AddComponent<CustomItem>();

                MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError(
                        $"MeshFilter not found on {selectedObject.name}. CustomItem requires a MeshFilter."
                    );
                }

                FieldInfo meshFilterField = typeof(CustomItem).GetField(
                    "_meshFilter",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                if (meshFilterField != null)
                {
                    meshFilterField.SetValue(customItemScript, meshFilter);
                }

                CustomItemData customItemData = new CustomItemData
                {
                    Base = baseMeshes[i],
                    Custom = customMeshes[i],
                };

                FieldInfo dataField = typeof(CustomItem).GetField(
                    "_data",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                if (dataField != null)
                {
                    dataField.SetValue(customItemScript, customItemData);
                }

                FieldInfo itemTypeField = typeof(CustomItem).GetField(
                    "_itemType",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
                if (itemTypeField != null)
                {
                    itemTypeField.SetValue(customItemScript, itemTypes[i]);
                }
            }
        }

        EditorUtility.DisplayDialog(
            "GameReady Dress Object(s) Created",
            "The GameReady dress object(s) have been created successfully!",
            "OK"
        );
    }
}
