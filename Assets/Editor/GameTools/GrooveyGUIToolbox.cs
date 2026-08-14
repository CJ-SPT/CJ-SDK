using System.Collections.Generic;
using EFT.UI.DragAndDrop;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

public class CustomGUIToolboxEditor : EditorWindow
{
    private enum ViewState
    {
        MainMenu,
        CreationTools,
        WeaponTools,
        UtilityTools,
        LootItemTool,
        SkinnedObjectTool,
        DressObjectTool,
        ClothingHeadTool,
        CharacterArmsTool,
        RigLayoutTool,
        VoiceCreatorTool,
        TransformLinksTool,
        MaterialTransferTool,
        ComponentTransferTool,
        ClearAssetBundlesTool,
    }

    private ViewState currentView = ViewState.MainMenu;
    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle buttonStyle;
    private GUIStyle categoryButtonStyle;
    private GUIStyle descriptionStyle;
    private GUIStyle backButtonStyle;
    private bool stylesInitialized = false;

    // Default preset for all skinned items
    private Preset defaultSkinPreset;

    // Tool-specific state variables

    // Skinned Object Creator
    private int skinnedNumItems = 1;
    private GameObject[] skinnedMainObjects;
    private List<GameObject[]> skinnedSkinObjects;
    private List<GameObject[]> skinnedLootObjects;
    private Preset[] skinnedPresets;
    private int skinnedSelectedSkinCount;
    private int skinnedSelectedLootCount;
    private SkinnedObjectType[] skinnedSkinTypes;
    private Mesh[][] skinnedBackpackMeshes;
    private Mesh[][] skinnedVestMeshes;

    public enum SkinnedObjectType
    {
        None,
        Backpack,
        Vest,
    }

    // Dress Item Creator
    private int dressNumItems = 1;
    private GameObject[] dressMeshObjects;
    private bool[] dressUseRainCondensator;
    private bool[] dressUseCustomItem;
    private Mesh[] dressBaseMeshes;
    private Mesh[] dressCustomMeshes;
    private EClippingCustoms[] dressItemTypes;
    private EDecalTextureType[] dressDecalTextureTypes; // <-- Add this

    // Clothing/Head Creator
    private int clothingNumItems = 1;
    private GameObject[] clothingMainObjects;
    private List<GameObject[]> clothingSkinObjects;
    private ClothingHeadSkinType[] clothingSkinTypes;
    private Mesh[] clothingBaseMesh;
    private Mesh[] clothingArmorMesh;
    private Mesh[] clothingVestMesh;
    private Mesh[] clothingHeadBaseMesh;
    private Mesh[] clothingHeadFaceCoverMesh;

    // Character Arms Creator
    private int armsNumItems = 1;
    private GameObject[] armsMainObjects;
    private List<GameObject[]> armsSkinObjects;
    private Preset armsGlobalPreset;

    // Rig Layout Editor
    private GameObject rigGameObject;
    private List<GridView> rigGridViews = new List<GridView>();
    private List<int> rigCellWidths = new List<int>();
    private List<int> rigCellHeights = new List<int>();

    // Voice Creator
    private string voiceName = "";
    private string audioRootDirectory = "";

    // Transform Links
    private GameObject transformLinksMainObject;
    private bool transformLinksShowErrors = false;

    // Material Transfer
    private GameObject materialSourceObject;
    private GameObject materialTargetObject;
    private bool materialTransferShader = true;
    private bool materialTransferProperties = true;
    private bool materialCreateNew = false;

    // Component Transfer
    private GameObject componentSourceObject;
    private GameObject componentTargetObject;
    private bool componentIncludeInactive = true;
    private bool componentPreserveExisting = true;

    // Clear Asset Bundles Tool
    private string clearBundlesFolderPath = "Assets";

    [MenuItem("SDK/Game Tools/Automation Toolset")]
    public static void ShowWindow()
    {
        var window = GetWindow<CustomGUIToolboxEditor>("GUI Toolbox");
        window.minSize = new Vector2(450, 550);
    }

    private void OnEnable()
    {
        currentView = ViewState.MainMenu;
        InitializeToolStates();
    }

    private void InitializeToolStates()
    {
        skinnedMainObjects = new GameObject[1];
        skinnedSkinObjects = new List<GameObject[]> { new GameObject[0] };
        skinnedLootObjects = new List<GameObject[]> { new GameObject[0] };
        skinnedPresets = new Preset[1];
        skinnedSkinTypes = new SkinnedObjectType[1];
        skinnedBackpackMeshes = new Mesh[1][];
        skinnedVestMeshes = new Mesh[1][];

        for (int i = 0; i < skinnedBackpackMeshes.Length; i++)
        {
            skinnedBackpackMeshes[i] = new Mesh[4];
            skinnedVestMeshes[i] = new Mesh[2];
        }

        clothingMainObjects = new GameObject[1];
        clothingSkinObjects = new List<GameObject[]> { new GameObject[0] };
        clothingSkinTypes = new ClothingHeadSkinType[1];
        clothingBaseMesh = new Mesh[1];
        clothingArmorMesh = new Mesh[1];
        clothingVestMesh = new Mesh[1];
        clothingHeadBaseMesh = new Mesh[1];
        clothingHeadFaceCoverMesh = new Mesh[1];

        defaultSkinPreset = AssetDatabase.LoadAssetAtPath<Preset>(
            "Assets/ScriptPresets/ExampleSkeletonSkin.preset"
        );
        if (defaultSkinPreset == null)
        {
            Debug.LogWarning(
                "ExampleSkeletonSkin preset not found at Assets/ScriptPresets/ExampleSkeletonSkin.preset"
            );
        }

        armsMainObjects = new GameObject[1];
        armsSkinObjects = new List<GameObject[]> { new GameObject[0] };

        armsGlobalPreset = AssetDatabase.LoadAssetAtPath<Preset>(
            "Assets/ScriptPresets/ExampleSkeletonHands.preset"
        );
        if (armsGlobalPreset == null)
        {
            Debug.LogWarning(
                "ExampleSkeletonHands preset not found at Assets/ScriptPresets/ExampleSkeletonHands.preset"
            );
        }

        clearBundlesFolderPath = "Assets";

        dressMeshObjects = new GameObject[1];
        dressUseRainCondensator = new bool[1];
        dressUseCustomItem = new bool[1];
        dressBaseMeshes = new Mesh[1];
        dressCustomMeshes = new Mesh[1];
        dressItemTypes = new EClippingCustoms[1];
        dressDecalTextureTypes = new EDecalTextureType[1];
    }

    private void InitializeStyles()
    {
        if (stylesInitialized)
            return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(10, 10, 10, 10),
        };

        sectionStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(15, 15, 10, 10),
            margin = new RectOffset(10, 10, 5, 5),
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 32,
            alignment = TextAnchor.MiddleCenter,
        };

        categoryButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            fixedHeight = 50,
            alignment = TextAnchor.MiddleCenter,
        };

        descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 11,
            wordWrap = true,
            margin = new RectOffset(0, 0, 5, 10),
        };

        backButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 28,
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitializeStyles();
        DrawHeader();

        if (currentView != ViewState.MainMenu)
        {
            DrawNavigationBar();
        }

        GUILayout.Space(5);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (currentView)
        {
            case ViewState.MainMenu:
                DrawMainMenu();
                break;
            case ViewState.CreationTools:
                DrawCreationToolsCategory();
                break;
            case ViewState.WeaponTools:
                DrawWeaponToolsCategory();
                break;
            case ViewState.UtilityTools:
                DrawUtilityToolsCategory();
                break;
            case ViewState.LootItemTool:
                DrawLootItemTool();
                break;
            case ViewState.SkinnedObjectTool:
                DrawSkinnedObjectTool();
                break;
            case ViewState.DressObjectTool:
                DrawDressObjectTool();
                break;
            case ViewState.ClothingHeadTool:
                DrawClothingHeadTool();
                break;
            case ViewState.CharacterArmsTool:
                DrawCharacterArmsTool();
                break;
            case ViewState.RigLayoutTool:
                DrawRigLayoutTool();
                break;
            case ViewState.VoiceCreatorTool:
                DrawVoiceCreatorTool();
                break;
            case ViewState.TransformLinksTool:
                DrawTransformLinksTool();
                break;
            case ViewState.MaterialTransferTool:
                DrawMaterialTransferTool();
                break;
            case ViewState.ComponentTransferTool:
                DrawComponentTransferTool();
                break;
            case ViewState.ClearAssetBundlesTool:
                DrawClearAssetBundlesTool();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🛠️ Groovey's GUI Toolbox", headerStyle);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label(GetCurrentSubtitle(), EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private string GetCurrentSubtitle()
    {
        switch (currentView)
        {
            case ViewState.MainMenu:
                return "Automate the creation of (almost) every Tarkov Item";
            case ViewState.CreationTools:
                return "Creation Tools";
            case ViewState.WeaponTools:
                return "Weapon Tools";
            case ViewState.UtilityTools:
                return "Utility Tools";
            default:
                return "Tool Active";
        }
    }

    private void DrawNavigationBar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🏠 Home", backButtonStyle, GUILayout.Width(80)))
        {
            currentView = ViewState.MainMenu;
            scrollPosition = Vector2.zero;
        }

        if (IsToolView(currentView))
        {
            if (GUILayout.Button("← Back", backButtonStyle, GUILayout.Width(80)))
            {
                currentView = GetParentCategory(currentView);
                scrollPosition = Vector2.zero;
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private bool IsToolView(ViewState view)
    {
        return view >= ViewState.LootItemTool;
    }

    private ViewState GetParentCategory(ViewState toolView)
    {
        switch (toolView)
        {
            case ViewState.LootItemTool:
            case ViewState.SkinnedObjectTool:
            case ViewState.DressObjectTool:
            case ViewState.ClothingHeadTool:
            case ViewState.CharacterArmsTool:
            case ViewState.RigLayoutTool:
            case ViewState.VoiceCreatorTool:
                return ViewState.CreationTools;
            case ViewState.TransformLinksTool:
                return ViewState.WeaponTools;
            case ViewState.MaterialTransferTool:
            case ViewState.ComponentTransferTool:
            case ViewState.ClearAssetBundlesTool:
                return ViewState.UtilityTools;
            default:
                return ViewState.MainMenu;
        }
    }

    private void DrawMainMenu()
    {
        GUILayout.Label("Select a category:", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("📦 Creation Tools", categoryButtonStyle))
        {
            currentView = ViewState.CreationTools;
            scrollPosition = Vector2.zero;
        }

        GUILayout.Space(5);

        if (GUILayout.Button("🔧 Weapon Tools", categoryButtonStyle))
        {
            currentView = ViewState.WeaponTools;
            scrollPosition = Vector2.zero;
        }

        GUILayout.Space(5);

        if (GUILayout.Button("⚙️ Utility Tools", categoryButtonStyle))
        {
            currentView = ViewState.UtilityTools;
            scrollPosition = Vector2.zero;
        }
    }

    private void DrawCreationToolsCategory()
    {
        DrawToolSection(
            "📦 Loot Item Creator",
            "Loot items are static items: Quest items, cases, hideout items, basically anything you can't wear or attach to a weapon, that ISN'T animated.",
            "Open Tool",
            () => currentView = ViewState.LootItemTool
        );
        DrawToolSection(
            "🎽 Skinned Object Creator",
            "Skinned items are anything the character wears that requires weight painting. Chest rigs, backpacks, armor, etc.",
            "Open Tool",
            () => currentView = ViewState.SkinnedObjectTool
        );
        DrawToolSection(
            "🪖 Dress Object Creator",
            "Dress objects are items the character wears that DON'T require weight painting (helmets) or objects that go into mod slots (masks, NVG, etc).",
            "Open Tool",
            () => currentView = ViewState.DressObjectTool
        );
        DrawToolSection(
            "👤 Clothing & Head Creator",
            "Heads and clothing both have the same setup, so this works for both.",
            "Open Tool",
            () => currentView = ViewState.ClothingHeadTool
        );
        DrawToolSection(
            "🤲 Character Arms Creator",
            "First person arms creator for custom models.",
            "Open Tool",
            () => currentView = ViewState.CharacterArmsTool
        );
        DrawToolSection(
            "📐 Custom Rig Layout Editor",
            "Automatically configure your custom rig layout after you've made it, and optionally export the grid servercode.",
            "Open Tool",
            () => currentView = ViewState.RigLayoutTool
        );
        DrawToolSection(
            "🎙️ Custom Voice Creator",
            "Automatically creates and configures tagbanks for your custom voicelines. This REQUIRES each audioclip be named the same as its corresponding tagbank.",
            "Open Tool",
            () => currentView = ViewState.VoiceCreatorTool
        );
    }

    private void DrawWeaponToolsCategory()
    {
        DrawToolSection(
            "🔗 Auto Transform Links",
            "Automatically configures your transform links for animated items. It fills out all the bones in the proper order, and converts the rotational data to quaternions.",
            "Open Tool",
            () => currentView = ViewState.TransformLinksTool
        );
    }

    private void DrawUtilityToolsCategory()
    {
        DrawToolSection(
            "🎨 Material Transfer Tool",
            "Transfer materials from one GameObject hierarchy to another by matching material names. Useful for applying materials to re-imported models.",
            "Open Tool",
            () => currentView = ViewState.MaterialTransferTool
        );

        DrawToolSection(
            "🎨 Component Transfer Tool",
            "Transfer components from one GameObject hierarchy to another by matching gameobject names. Useful for applying scripts to re-imported models.",
            "Open Tool",
            () => currentView = ViewState.ComponentTransferTool
        );

        DrawToolSection(
            "🧹 Clear Asset Bundle Labels",
            "Remove all Asset Bundle labels from assets in a selected folder. Useful for cleaning up asset bundle ripped from the game.",
            "Open Tool",
            () => currentView = ViewState.ClearAssetBundlesTool
        );
    }

    private void DrawLootItemTool()
    {
        GUILayout.Label("📦 Loot Item Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Loot items are static items: Quest items, cases, hideout items, basically anything you can't wear or attach to a weapon, that ISN'T animated.",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label(
            "Click the button to turn selected GameObjects into GameReady loot objects:"
        );

        EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0);
        if (GUILayout.Button("Create GameReady Loot Object(s)", buttonStyle))
        {
            GameReadyLootItemEditor.CreateGameReadyLootObjects();
            Repaint();
        }

        EditorGUI.EndDisabledGroup();

        if (Selection.gameObjects.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Select one or more GameObjects in the hierarchy to enable this tool.",
                MessageType.Warning
            );
        }
    }

    private void DrawSkinnedObjectTool()
    {
        GUILayout.Label("🎽 Skinned Object Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Skinned items are anything the character wears that requires weight painting. Chest rigs, backpacks, armor, etc.",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Enter the number of items you want to create:");
        skinnedNumItems = EditorGUILayout.IntField("Number of Items", skinnedNumItems);

        if (skinnedMainObjects == null || skinnedMainObjects.Length != skinnedNumItems)
        {
            skinnedMainObjects = new GameObject[skinnedNumItems];
            skinnedSkinObjects = new List<GameObject[]>();
            skinnedLootObjects = new List<GameObject[]>();
            skinnedSkinTypes = new SkinnedObjectType[skinnedNumItems];
            skinnedBackpackMeshes = new Mesh[skinnedNumItems][];
            skinnedVestMeshes = new Mesh[skinnedNumItems][];

            for (int i = 0; i < skinnedNumItems; i++)
            {
                skinnedSkinObjects.Add(new GameObject[0]);
                skinnedLootObjects.Add(new GameObject[0]);
                skinnedBackpackMeshes[i] = new Mesh[4];
                skinnedVestMeshes[i] = new Mesh[2];
            }
        }

        GUILayout.Label("Global Skin Preset (auto-loaded):");
        defaultSkinPreset =
            EditorGUILayout.ObjectField(
                "Global Skin Preset",
                defaultSkinPreset,
                typeof(Preset),
                false
            ) as Preset;

        if (defaultSkinPreset == null)
        {
            EditorGUILayout.HelpBox(
                " Preset not assigned. Please select ExampleSkeletonSkin from Assets/ScriptPresets/",
                MessageType.Warning
            );
        }

        for (int i = 0; i < skinnedNumItems; i++)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Item " + (i + 1));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            skinnedMainObjects[i] = (GameObject)
                EditorGUILayout.ObjectField(
                    "Gameobject_MAIN",
                    skinnedMainObjects[i],
                    typeof(GameObject),
                    true
                );

            EditorGUILayout.Space(5);
            GUILayout.Label("Special Skin Type (Optional):");

            SkinnedObjectType previousType = skinnedSkinTypes[i];
            skinnedSkinTypes[i] = (SkinnedObjectType)
                EditorGUILayout.EnumPopup("Skin Type", skinnedSkinTypes[i]);

            if (
                previousType == SkinnedObjectType.None
                && skinnedSkinTypes[i] != SkinnedObjectType.None
            ) { }
            else if (
                previousType != skinnedSkinTypes[i]
                && skinnedSkinTypes[i] != SkinnedObjectType.None
            )
            {
                if (previousType != SkinnedObjectType.None)
                {
                    if (previousType == SkinnedObjectType.Backpack)
                    {
                        skinnedBackpackMeshes[i] = new Mesh[4];
                    }
                    else if (previousType == SkinnedObjectType.Vest)
                    {
                        skinnedVestMeshes[i] = new Mesh[2];
                    }
                }
            }

            if (skinnedSkinTypes[i] == SkinnedObjectType.Backpack)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("Backpack Skin Meshes:");
                skinnedBackpackMeshes[i][0] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Body Mesh",
                        skinnedBackpackMeshes[i][0],
                        typeof(Mesh),
                        false
                    );
                skinnedBackpackMeshes[i][1] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Armor Mesh",
                        skinnedBackpackMeshes[i][1],
                        typeof(Mesh),
                        false
                    );
                skinnedBackpackMeshes[i][2] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Vest Mesh",
                        skinnedBackpackMeshes[i][2],
                        typeof(Mesh),
                        false
                    );
                skinnedBackpackMeshes[i][3] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "ArmorVest Mesh",
                        skinnedBackpackMeshes[i][3],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
            }
            else if (skinnedSkinTypes[i] == SkinnedObjectType.Vest)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("Vest Skin Meshes:");
                skinnedVestMeshes[i][0] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Body Mesh",
                        skinnedVestMeshes[i][0],
                        typeof(Mesh),
                        false
                    );
                skinnedVestMeshes[i][1] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Armor Mesh",
                        skinnedVestMeshes[i][1],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Select Skin GameObjects"))
            {
                GameObject[] selected = Selection.gameObjects;
                if (selected != null && selected.Length > 0)
                {
                    List<GameObject> skins = new List<GameObject>();
                    foreach (GameObject go in selected)
                        if (go != skinnedMainObjects[i])
                            skins.Add(go);
                    skinnedSkinObjects[i] = skins.ToArray();
                }
            }

            GUILayout.Label("Number of Skin Objects: " + skinnedSkinObjects[i].Length);

            if (GUILayout.Button("Select Loot GameObjects"))
            {
                GameObject[] selected = Selection.gameObjects;
                if (selected != null && selected.Length > 0)
                {
                    List<GameObject> loots = new List<GameObject>();
                    foreach (GameObject go in selected)
                        if (go != skinnedMainObjects[i])
                            loots.Add(go);
                    skinnedLootObjects[i] = loots.ToArray();
                }
            }

            GUILayout.Label("Number of Loot Objects: " + skinnedLootObjects[i].Length);

            EditorGUILayout.EndVertical();
        }

        EditorGUI.BeginDisabledGroup(!AllSkinnedObjectsSet() || defaultSkinPreset == null);
        if (GUILayout.Button("Create Gameready Skinned Object(s)", buttonStyle))
        {
            Preset[] presetsArray = new Preset[skinnedNumItems];
            for (int i = 0; i < skinnedNumItems; i++)
            {
                presetsArray[i] = defaultSkinPreset;
            }

            GameReadySkinnedObjectCreatorEditor.CreateGamereadyObjects(
                skinnedNumItems,
                skinnedMainObjects,
                skinnedSkinObjects,
                skinnedLootObjects,
                presetsArray,
                skinnedSkinTypes,
                skinnedBackpackMeshes,
                skinnedVestMeshes
            );
            Repaint();
        }

        EditorGUI.EndDisabledGroup();
    }

    private bool AllSkinnedObjectsSet()
    {
        if (defaultSkinPreset == null)
            return false;

        for (int i = 0; i < skinnedNumItems; i++)
        {
            if (
                skinnedMainObjects[i] == null
                || skinnedSkinObjects[i].Length == 0
                || skinnedLootObjects[i].Length == 0
            )
                return false;

            if (skinnedSkinTypes[i] == SkinnedObjectType.Backpack)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (skinnedBackpackMeshes[i][j] == null)
                    {
                        return false;
                    }
                }
            }
            else if (skinnedSkinTypes[i] == SkinnedObjectType.Vest)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (skinnedVestMeshes[i][j] == null)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void DrawDressObjectTool()
    {
        GUILayout.Label("🪖 Dress Object Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Dress objects are items the character wears that DON'T require weight painting (helmets) or objects that go into mod slots (masks, NVG, etc).",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Enter the number of dress items you want to create:");
        dressNumItems = EditorGUILayout.IntField("Number of Items", dressNumItems);

        if (dressMeshObjects.Length != dressNumItems)
        {
            dressMeshObjects = new GameObject[dressNumItems];
            dressUseRainCondensator = new bool[dressNumItems];
            dressUseCustomItem = new bool[dressNumItems];
            dressBaseMeshes = new Mesh[dressNumItems];
            dressCustomMeshes = new Mesh[dressNumItems];
            dressItemTypes = new EClippingCustoms[dressNumItems];
            dressDecalTextureTypes = new EDecalTextureType[dressNumItems];
        }

        for (int i = 0; i < dressNumItems; i++)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Item " + (i + 1));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            dressMeshObjects[i] = (GameObject)
                EditorGUILayout.ObjectField(
                    "Mesh GameObject",
                    dressMeshObjects[i],
                    typeof(GameObject),
                    true
                );

            EditorGUILayout.Space();
            dressDecalTextureTypes[i] = (EDecalTextureType)
                EditorGUILayout.EnumPopup("Decal Texture Type", dressDecalTextureTypes[i]);

            EditorGUILayout.Space();
            dressUseRainCondensator[i] = EditorGUILayout.Toggle(
                "Rain Condensator",
                dressUseRainCondensator[i]
            );
            dressUseCustomItem[i] = EditorGUILayout.Toggle(
                "Add Custom Item Script",
                dressUseCustomItem[i]
            );

            if (dressUseCustomItem[i])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Custom Item Settings", EditorStyles.miniBoldLabel);

                dressBaseMeshes[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Base Mesh",
                        dressBaseMeshes[i],
                        typeof(Mesh),
                        false
                    );

                dressCustomMeshes[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Custom Mesh",
                        dressCustomMeshes[i],
                        typeof(Mesh),
                        false
                    );

                dressItemTypes[i] = (EClippingCustoms)
                    EditorGUILayout.EnumFlagsField("Item Type", dressItemTypes[i]);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUI.BeginDisabledGroup(!AllDressObjectsSet());
        if (GUILayout.Button("Create Game Ready Dress Object(s)", buttonStyle))
        {
            GameReadyDressObjectCreatorEditor.CreateGameReadyDressObjects(
                dressNumItems,
                dressMeshObjects,
                dressUseRainCondensator,
                dressUseCustomItem,
                dressBaseMeshes,
                dressCustomMeshes,
                dressItemTypes,
                dressDecalTextureTypes
            );
            Repaint();
        }

        EditorGUI.EndDisabledGroup();
    }

    private bool AllDressObjectsSet()
    {
        for (int i = 0; i < dressNumItems; i++)
        {
            if (dressMeshObjects[i] == null)
                return false;

            if (dressUseCustomItem[i])
            {
                if (dressBaseMeshes[i] == null || dressCustomMeshes[i] == null)
                    return false;
            }
        }

        return true;
    }

    private void DrawClothingHeadTool()
    {
        GUILayout.Label("👤 Clothing & Head Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Heads and clothing both have the same setup, so this works for both.",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Enter the number of items you want to create:");
        clothingNumItems = EditorGUILayout.IntField("Number of Items", clothingNumItems);

        if (clothingMainObjects.Length != clothingNumItems)
        {
            clothingMainObjects = new GameObject[clothingNumItems];
            clothingSkinObjects.Clear();
            clothingSkinTypes = new ClothingHeadSkinType[clothingNumItems];
            clothingBaseMesh = new Mesh[clothingNumItems];
            clothingArmorMesh = new Mesh[clothingNumItems];
            clothingVestMesh = new Mesh[clothingNumItems];
            clothingHeadBaseMesh = new Mesh[clothingNumItems];
            clothingHeadFaceCoverMesh = new Mesh[clothingNumItems];

            for (int i = 0; i < clothingNumItems; i++)
                clothingSkinObjects.Add(new GameObject[0]);
        }

        GUILayout.Label("Global Skin Preset (auto-loaded):");
        defaultSkinPreset =
            EditorGUILayout.ObjectField(
                "Global Skin Preset",
                defaultSkinPreset,
                typeof(Preset),
                false
            ) as Preset;

        if (defaultSkinPreset == null)
        {
            EditorGUILayout.HelpBox(
                " Preset not assigned. Please select ExampleSkeletonSkin from Assets/ScriptPresets/",
                MessageType.Warning
            );
        }

        for (int i = 0; i < clothingNumItems; i++)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Item " + (i + 1));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            clothingMainObjects[i] = (GameObject)
                EditorGUILayout.ObjectField(
                    "Gameobject_MAIN",
                    clothingMainObjects[i],
                    typeof(GameObject),
                    true
                );

            EditorGUILayout.Space(5);
            GUILayout.Label("Special Skin Type (Optional):");

            ClothingHeadSkinType previousType = clothingSkinTypes[i];
            clothingSkinTypes[i] = (ClothingHeadSkinType)
                EditorGUILayout.EnumPopup("Skin Type", clothingSkinTypes[i]);

            if (
                previousType != clothingSkinTypes[i]
                && clothingSkinTypes[i] != ClothingHeadSkinType.None
            )
            {
                if (previousType != ClothingHeadSkinType.None)
                {
                    if (previousType == ClothingHeadSkinType.TorsoSkin)
                    {
                        clothingBaseMesh[i] = null;
                        clothingArmorMesh[i] = null;
                        clothingVestMesh[i] = null;
                    }
                    else if (previousType == ClothingHeadSkinType.HeadSkin)
                    {
                        clothingHeadBaseMesh[i] = null;
                        clothingHeadFaceCoverMesh[i] = null;
                    }
                }
            }

            if (clothingSkinTypes[i] == ClothingHeadSkinType.TorsoSkin)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("TorsoSkin Meshes:");
                clothingBaseMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Base Mesh",
                        clothingBaseMesh[i],
                        typeof(Mesh),
                        false
                    );
                clothingArmorMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Armor Mesh",
                        clothingArmorMesh[i],
                        typeof(Mesh),
                        false
                    );
                clothingVestMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Vest Mesh",
                        clothingVestMesh[i],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            else if (clothingSkinTypes[i] == ClothingHeadSkinType.HeadSkin)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("HeadSkin Meshes:");
                clothingHeadBaseMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "Base Mesh",
                        clothingHeadBaseMesh[i],
                        typeof(Mesh),
                        false
                    );
                clothingHeadFaceCoverMesh[i] = (Mesh)
                    EditorGUILayout.ObjectField(
                        "FaceCover Mesh",
                        clothingHeadFaceCoverMesh[i],
                        typeof(Mesh),
                        false
                    );
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }

            if (GUILayout.Button("Select Skin GameObjects"))
            {
                GameObject[] selected = Selection.gameObjects;
                if (selected != null && selected.Length > 0)
                {
                    List<GameObject> skins = new List<GameObject>();
                    foreach (GameObject go in selected)
                        if (go != clothingMainObjects[i])
                            skins.Add(go);
                    clothingSkinObjects[i] = skins.ToArray();
                }
            }

            GUILayout.Label("Number of Skins: " + clothingSkinObjects[i].Length);
            EditorGUILayout.EndVertical();
        }

        EditorGUI.BeginDisabledGroup(!AllClothingObjectsSet() || defaultSkinPreset == null);
        if (GUILayout.Button("Create Game Ready Clothes/Head", buttonStyle))
        {
            GameReadyClothingHeadCreatorEditor.CreateGameReadyCharacterClothesHead(
                clothingNumItems,
                clothingMainObjects,
                clothingSkinObjects,
                defaultSkinPreset,
                clothingSkinTypes,
                clothingBaseMesh,
                clothingArmorMesh,
                clothingVestMesh,
                clothingHeadBaseMesh,
                clothingHeadFaceCoverMesh
            );
            Repaint();
        }

        EditorGUI.EndDisabledGroup();
    }

    private bool AllClothingObjectsSet()
    {
        if (defaultSkinPreset == null)
            return false;

        for (int i = 0; i < clothingNumItems; i++)
        {
            if (
                clothingMainObjects[i] == null
                || clothingSkinObjects[i] == null
                || clothingSkinObjects[i].Length == 0
            )
                return false;

            if (clothingSkinTypes[i] == ClothingHeadSkinType.TorsoSkin)
            {
                if (
                    clothingBaseMesh[i] == null
                    || clothingArmorMesh[i] == null
                    || clothingVestMesh[i] == null
                )
                {
                    return false;
                }
            }
            else if (clothingSkinTypes[i] == ClothingHeadSkinType.HeadSkin)
            {
                if (clothingHeadBaseMesh[i] == null || clothingHeadFaceCoverMesh[i] == null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void DrawCharacterArmsTool()
    {
        GUILayout.Label("🤲 Character Arms Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("First person arms creator for custom models.", MessageType.Info);
        GUILayout.Space(10);

        GUILayout.Label("Enter the number of items you want to create:");
        armsNumItems = EditorGUILayout.IntField("Number of Items", armsNumItems);

        if (armsMainObjects.Length != armsNumItems)
        {
            armsMainObjects = new GameObject[armsNumItems];
            armsSkinObjects.Clear();
            for (int i = 0; i < armsNumItems; i++)
                armsSkinObjects.Add(new GameObject[0]);
        }

        GUILayout.Label("Global Skin Preset (auto-loaded):");
        armsGlobalPreset =
            EditorGUILayout.ObjectField(
                "Global Skin Preset",
                armsGlobalPreset,
                typeof(Preset),
                false
            ) as Preset;

        if (armsGlobalPreset == null)
        {
            EditorGUILayout.HelpBox(
                " Preset not assigned. Please select ExampleSkeletonHands from Assets/ScriptPresets/",
                MessageType.Warning
            );
        }

        for (int i = 0; i < armsNumItems; i++)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Item " + (i + 1));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            armsMainObjects[i] = (GameObject)
                EditorGUILayout.ObjectField(
                    "Gameobject_MAIN",
                    armsMainObjects[i],
                    typeof(GameObject),
                    true
                );
            if (GUILayout.Button("Select Skin GameObjects"))
            {
                GameObject[] selected = Selection.gameObjects;
                if (selected != null && selected.Length > 0)
                {
                    List<GameObject> skins = new List<GameObject>();
                    foreach (GameObject go in selected)
                        if (go != armsMainObjects[i])
                            skins.Add(go);
                    armsSkinObjects[i] = skins.ToArray();
                }
            }

            GUILayout.Label("Number of Skins: " + armsSkinObjects[i].Length);
            EditorGUILayout.EndVertical();
        }

        EditorGUI.BeginDisabledGroup(!AllArmsObjectsSet() || armsGlobalPreset == null);
        if (GUILayout.Button("Create Game Ready Character Arms", buttonStyle))
        {
            GameReadyCharacterArmsCreatorEditor.CreateGameReadyCharacterArms(
                armsNumItems,
                armsMainObjects,
                armsSkinObjects,
                armsGlobalPreset
            );
            Repaint();
        }

        EditorGUI.EndDisabledGroup();
    }

    private bool AllArmsObjectsSet()
    {
        if (armsGlobalPreset == null)
            return false;

        for (int i = 0; i < armsNumItems; i++)
        {
            if (
                armsMainObjects[i] == null
                || armsSkinObjects[i] == null
                || armsSkinObjects[i].Length == 0
            )
                return false;
        }

        return true;
    }

    private void DrawRigLayoutTool()
    {
        GUILayout.Label("📐 Custom Rig Layout Editor", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "This tool configures the layout of your custom rig, and optionally exports the grid layout to a JSON file.",
            MessageType.Info
        );
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Select your main Custom Rig GameObject before continuing.",
            MessageType.Info
        );
        EditorGUILayout.Space();

        rigGameObject = Selection.activeGameObject;

        if (rigGameObject == null)
        {
            EditorGUILayout.HelpBox(
                "Please select a Custom Rig GameObject in the Hierarchy.",
                MessageType.Warning
            );
            return;
        }

        var templatedGridsView = rigGameObject.GetComponent<TemplatedGridsView>();
        if (templatedGridsView == null)
        {
            EditorGUILayout.HelpBox(
                "The selected Custom Rig GameObject does not have a TemplatedGridsView component.",
                MessageType.Error
            );
            return;
        }

        EditorGUILayout.HelpBox(
            "Renames all grid slots to GridView (n), fills out Templated Grids View script, checks the size of the layout and compares it to the Rect Transform, setting them to the same value.",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Configure Rig Layout", buttonStyle))
        {
            CustomRigEditor.ConfigureRig(
                rigGameObject,
                rigGridViews,
                rigCellWidths,
                rigCellHeights
            );
            Repaint();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Exports the grid layout to a JSON file, located in the Assets/CustomRigLayouts/Grids folder.",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Export Grid", buttonStyle))
        {
            CustomRigEditor.ExportJSON(rigGameObject, rigGridViews, rigCellWidths, rigCellHeights);
        }
    }

    private void DrawVoiceCreatorTool()
    {
        GUILayout.Label("🎙️ Custom Voice Creator", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Create Voice and Tagbanks. To use this tool, prepare all your audioclips into a directory and prefix each audioclip name with the EXACT name of the tagbank it is going into. i.e. FriendlyFire_1, or OnEnemyGrenade_excited",
            MessageType.Info
        );
        GUILayout.Space(10);

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

        if (GUILayout.Button("Create Voice Tagbanks", buttonStyle))
        {
            VoiceTagBankCreator.CreateVoiceTagbanks(voiceName, audioRootDirectory);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click to create tagbanks for the voice using the selected audio clips folder. This will automatically export the new voice Tagbanks to Assets/CustomVoices/(your voice name)",
            MessageType.Info
        );
        EditorGUILayout.Space();

        if (GUILayout.Button("Process Voice Audioclips", buttonStyle))
        {
            VoiceTagBankCreator.ProcessVoiceAudioclips(voiceName, audioRootDirectory);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click to process the selected voice's audio clips and associate them with the appropriate tagbanks.",
            MessageType.Info
        );
    }

    private void DrawTransformLinksTool()
    {
        GUILayout.Label("🔗 Auto Transform Links", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Automatically configures your transform links for animated items. It fills out all the bones in the proper order, and converts the rotational data to quaternions.",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Select the Main GameObject", EditorStyles.boldLabel);
        transformLinksMainObject = (GameObject)
            EditorGUILayout.ObjectField(transformLinksMainObject, typeof(GameObject), true);
        GUILayout.Space(10);

        if (GUILayout.Button("Apply Transform Links", buttonStyle))
        {
            TransformLinksAutomation.ApplyTransformLinks(
                transformLinksMainObject,
                transformLinksShowErrors
            );
            Repaint();
        }

        transformLinksShowErrors = EditorGUILayout.Toggle("Show Errors", transformLinksShowErrors);
    }

    // Add this method to draw the UI:
    private void DrawComponentTransferTool()
    {
        GUILayout.Label("🔄 Component Transfer Tool", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Copies components from source GameObject hierarchy to target GameObject hierarchy "
                + "when GameObject names match 1:1. Only copies components that don't already exist on the target.\n\n"
                + "Uses Unity's built-in Copy/Paste Component functionality to preserve all values.",
            MessageType.Info
        );
        GUILayout.Space(10);

        componentSourceObject =
            EditorGUILayout.ObjectField(
                "Source Object",
                componentSourceObject,
                typeof(GameObject),
                true
            ) as GameObject;
        componentTargetObject =
            EditorGUILayout.ObjectField(
                "Target Object",
                componentTargetObject,
                typeof(GameObject),
                true
            ) as GameObject;

        GUILayout.Space(10);

        componentIncludeInactive = EditorGUILayout.Toggle(
            "Include Inactive Objects",
            componentIncludeInactive
        );
        componentPreserveExisting = EditorGUILayout.Toggle(
            "Preserve Existing Components",
            componentPreserveExisting
        );

        EditorGUILayout.HelpBox(
            "Transform components are automatically excluded.",
            MessageType.None
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Transfer Components", buttonStyle))
        {
            if (componentSourceObject == null || componentTargetObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Both Source and Target objects must be assigned.",
                    "OK"
                );
                return;
            }

            ComponentTransferEditor.TransferComponents(
                componentSourceObject,
                componentTargetObject,
                componentIncludeInactive,
                componentPreserveExisting
            );
        }
    }

    private void DrawMaterialTransferTool()
    {
        GUILayout.Label("🎨 Material Transfer Tool", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Transfer materials from one GameObject hierarchy to another by matching material names. Useful for applying materials to imported models.",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Material Transfer Tool", EditorStyles.boldLabel);
        materialSourceObject =
            EditorGUILayout.ObjectField(
                "Source Object (A)",
                materialSourceObject,
                typeof(GameObject),
                true
            ) as GameObject;
        materialTargetObject =
            EditorGUILayout.ObjectField(
                "Target Object (B)",
                materialTargetObject,
                typeof(GameObject),
                true
            ) as GameObject;
        materialTransferShader = EditorGUILayout.Toggle("Transfer Shader", materialTransferShader);
        materialTransferProperties = EditorGUILayout.Toggle(
            "Transfer Properties",
            materialTransferProperties
        );
        materialCreateNew = EditorGUILayout.Toggle(
            "Create New Materials (Copy)",
            materialCreateNew
        );

        GUILayout.Space(10);
        if (GUILayout.Button("Transfer Materials", buttonStyle))
        {
            if (materialSourceObject == null || materialTargetObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Both Source and Target objects must be assigned.",
                    "OK"
                );
                return;
            }

            MaterialTransferEditor.TransferMaterials(
                materialSourceObject,
                materialTargetObject,
                materialTransferShader,
                materialTransferProperties,
                materialCreateNew
            );
        }
    }

    private void DrawToolSection(
        string title,
        string description,
        string buttonText,
        System.Action onClick
    )
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.Label(description, descriptionStyle);
        if (GUILayout.Button(buttonText, buttonStyle))
        {
            onClick?.Invoke();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawClearAssetBundlesTool()
    {
        GUILayout.Label("🧹 Clear Asset Bundle Names", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Remove all Asset Bundle names from assets in a selected folder. Useful for cleaning up asset bundle assignments.",
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Clear Asset Bundle Names Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Selected Folder:", clearBundlesFolderPath);

        if (GUILayout.Button("Select Folder", buttonStyle))
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
                    clearBundlesFolderPath =
                        "Assets" + selectedPath.Substring(Application.dataPath.Length);
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

        if (GUILayout.Button("Clear Asset Bundle Names", buttonStyle))
        {
            if (
                EditorUtility.DisplayDialog(
                    "Confirm",
                    $"This will clear all Asset Bundle names in {clearBundlesFolderPath}. Proceed?",
                    "Yes",
                    "No"
                )
            )
            {
                ClearAssetBundleLabels.ClearAssetBundleNamesInFolder(clearBundlesFolderPath);
                Repaint();
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "This tool will recursively clear Asset Bundle names for all assets in the selected folder and its subfolders.",
            MessageType.Info
        );
    }
}
