using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class MapLootEditorUiBuilder
{
    private const string RootFolder = "Assets/Mods/MapLootEditor.Assets";
    private const string UiFolder = RootFolder + "/UI";
    private const string CanvasPath = UiFolder + "/MapLootEditor.prefab";
    private const string MarkerPath = UiFolder + "/LootPointMarker.prefab";
    private const string GizmoPath = UiFolder + "/PlacementGizmo.prefab";
    private const string BundleName = "maplooteditor_ui.bundle";
    private static Font _font;

    [MenuItem("SDK/Map Loot Editor/Build UI Bundle")]
    public static void Build()
    {
        Directory.CreateDirectory(UiFolder);
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!_font) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        CreateCanvas();
        CreateMarker();
        CreateGizmo();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var output = GetArgument("-mapLootEditorOutput");
        if (string.IsNullOrWhiteSpace(output))
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../MapLootEditor/Client/MapLootEditor.Client/Resources"));
        Directory.CreateDirectory(output);
        var manifest = BuildPipeline.BuildAssetBundles(output, new[]
        {
            new AssetBundleBuild
            {
                assetBundleName = BundleName,
                assetNames = new[] { CanvasPath, MarkerPath, GizmoPath },
            },
        }, BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows64);
        if (manifest == null || !File.Exists(Path.Combine(output, BundleName)))
            throw new InvalidOperationException("Unity failed to build maplooteditor_ui.bundle.");
        Debug.Log("Map Loot Editor UI bundle built at " + output);
    }

    private static void CreateCanvas()
    {
        var root = new GameObject("MapLootEditor", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Stretch(root.GetComponent<RectTransform>());
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        // The editor must leave the raid scene readable. Only the compact tool panel is opaque.
        Image("Backdrop", root.transform, new Color(0, 0, 0, 0), Vector2.zero, Vector2.zero, true);

        var panel = Image("Panel", root.transform, new Color(0.035f, 0.043f, 0.044f, 0.97f), new Vector2(520, 940), Vector2.zero);
        panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0, 0.5f);
        panel.rectTransform.pivot = new Vector2(0, 0.5f);
        panel.rectTransform.anchoredPosition = new Vector2(18, 0);

        var header = Image("Header", panel.transform, new Color(0.135f, 0.17f, 0.17f, 1), new Vector2(516, 82), new Vector2(0, 427));
        Text("Title", header.transform, "MAP LOOT EDITOR", 21, TextAnchor.MiddleLeft, new Vector2(250, 38), new Vector2(-122, 18));
        Text("Status", header.transform, "NEXT RAID ONLY", 11, TextAnchor.MiddleRight, new Vector2(230, 34), new Vector2(126, 18));
        Text("MeanLabel", header.transform, "MAP SPAWN  MEAN", 9, TextAnchor.MiddleLeft, new Vector2(105, 24), new Vector2(-190, -22));
        Input("MapMean", header.transform, "VANILLA", new Vector2(92, 27), new Vector2(-87, -22));
        Text("DeviationLabel", header.transform, "DEVIATION", 9, TextAnchor.MiddleLeft, new Vector2(70, 24), new Vector2(-8, -22));
        Input("MapDeviation", header.transform, "VANILLA", new Vector2(92, 27), new Vector2(76, -22));
        Text("Scope", header.transform, "APPLIES NEXT RAID", 9, TextAnchor.MiddleRight, new Vector2(145, 24), new Vector2(180, -22));

        var tabBar = Rect("Tabs", panel.transform, new Vector2(488, 36), new Vector2(0, 366));
        Button("Groups", tabBar, "LOOSE GROUPS", new Vector2(158, 32), new Vector2(-165, 0));
        Button("Containers", tabBar, "CONTAINERS", new Vector2(158, 32), Vector2.zero);
        Button("Protected", tabBar, "PROTECTED", new Vector2(158, 32), new Vector2(165, 0));
        Input("Search", panel.transform, "Search groups, items, containers…", new Vector2(488, 34), new Vector2(0, 326));

        var list = Image("List", panel.transform, new Color(0.012f, 0.016f, 0.016f, 0.98f), new Vector2(488, 220), new Vector2(0, 193));
        var viewport = Image("Viewport", list.transform, new Color(0, 0, 0, 0), new Vector2(484, 216), Vector2.zero);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var content = Rect("Content", viewport.transform, new Vector2(462, 216), new Vector2(-7, 0));
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4); layout.spacing = 2;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; layout.childControlHeight = true;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var row = Button("RowTemplate", content, "ENTRY", new Vector2(454, 30), Vector2.zero);
        var rowLayout = row.gameObject.AddComponent<LayoutElement>(); rowLayout.minHeight = 30; rowLayout.preferredHeight = 30;
        row.gameObject.SetActive(false);
        var scroll = list.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport.rectTransform; scroll.content = content; scroll.horizontal = false;

        var inspector = Image("Inspector", panel.transform, new Color(0.065f, 0.078f, 0.078f, 0.98f), new Vector2(488, 374), new Vector2(0, -116));
        Text("Heading", inspector.transform, "SELECT A GROUP OR CONTAINER", 16, TextAnchor.MiddleLeft, new Vector2(454, 30), new Vector2(0, 164));
        LabelInput(inspector.transform, "PositionX", "POSITION X", -160, 116);
        LabelInput(inspector.transform, "PositionY", "POSITION Y", 0, 116);
        LabelInput(inspector.transform, "PositionZ", "POSITION Z", 160, 116);
        LabelInput(inspector.transform, "RotationY", "ROTATION Y", -160, 57);
        LabelInput(inspector.transform, "Probability", "TOTAL CHANCE", 0, 57);
        LabelInput(inspector.transform, "Weight", "MEMBER / ITEM WEIGHT", 160, 57);
        Toggle("AlwaysSpawn", inspector.transform, "ALWAYS SPAWN", new Vector2(-165, 9));
        Toggle("Disabled", inspector.transform, "DISABLED", new Vector2(-15, 9));
        Toggle("UseGravity", inspector.transform, "GRAVITY", new Vector2(135, 9));
        Toggle("RandomRotation", inspector.transform, "RANDOM ROTATION", new Vector2(135, -19));

        var loot = Image("LootChoices", inspector.transform, new Color(0.022f, 0.027f, 0.027f, 1), new Vector2(454, 102), new Vector2(0, -81));
        Text("Title", loot.transform, "ITEM CHOICES / SHARED CONTAINER POOL", 10, TextAnchor.UpperLeft, new Vector2(360, 20), new Vector2(-35, 36));
        var lootViewport = Image("Viewport", loot.transform, new Color(0, 0, 0, 0), new Vector2(366, 70), new Vector2(-34, -13));
        lootViewport.gameObject.AddComponent<RectMask2D>();
        var lootContent = Rect("Content", lootViewport.transform, new Vector2(360, 70), Vector2.zero);
        lootContent.anchorMin = lootContent.anchorMax = new Vector2(0.5f, 1); lootContent.pivot = new Vector2(0.5f, 1); lootContent.anchoredPosition = Vector2.zero;
        var lootLayout = lootContent.gameObject.AddComponent<VerticalLayoutGroup>();
        lootLayout.spacing = 2; lootLayout.childForceExpandHeight = false; lootLayout.childForceExpandWidth = true; lootLayout.childControlHeight = true;
        var lootFitter = lootContent.gameObject.AddComponent<ContentSizeFitter>(); lootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var lootRow = Button("RowTemplate", lootContent, "ITEM   WEIGHT", new Vector2(360, 22), Vector2.zero); lootRow.gameObject.SetActive(false);
        var lootRowLayout = lootRow.gameObject.AddComponent<LayoutElement>(); lootRowLayout.minHeight = 22; lootRowLayout.preferredHeight = 22;
        var lootScroll = loot.gameObject.AddComponent<ScrollRect>(); lootScroll.viewport = lootViewport.rectTransform; lootScroll.content = lootContent; lootScroll.horizontal = false;
        Button("Add", loot.transform, "+", new Vector2(42, 31), new Vector2(197, 16));
        Button("Remove", loot.transform, "−", new Vector2(42, 31), new Vector2(197, -20));

        var memberBar = Rect("MemberActions", inspector.transform, new Vector2(454, 34), new Vector2(0, -153));
        Button("AddMember", memberBar, "ADD MEMBER", new Vector2(106, 30), new Vector2(-174, 0));
        Button("RemoveMember", memberBar, "REMOVE", new Vector2(106, 30), new Vector2(-58, 0));
        Button("DuplicateGroup", memberBar, "DUPLICATE", new Vector2(106, 30), new Vector2(58, 0));
        Button("NewGroup", memberBar, "NEW GROUP", new Vector2(106, 30), new Vector2(174, 0));

        var actions = Rect("Actions", panel.transform, new Vector2(488, 136), new Vector2(0, -388));
        Button("Undo", actions, "UNDO", new Vector2(78, 31), new Vector2(-201, 42));
        Button("Redo", actions, "REDO", new Vector2(78, 31), new Vector2(-119, 42));
        Button("Discard", actions, "DISCARD", new Vector2(90, 31), new Vector2(-31, 42));
        Button("Import", actions, "IMPORT", new Vector2(78, 31), new Vector2(57, 42));
        Button("Export", actions, "EXPORT", new Vector2(78, 31), new Vector2(139, 42));
        Text("Controls", actions, "WASD + Q/E MOVE   •   HOLD RMB TO LOOK   •   WHEEL SPEED", 10, TextAnchor.MiddleLeft, new Vector2(454, 24), new Vector2(0, 5));
        Text("Placement", actions, "LMB SELECT   •   CTRL+LMB PLACE   •   ALT DISABLES SNAP", 9, TextAnchor.MiddleLeft, new Vector2(300, 22), new Vector2(-77, -19));
        Button("Close", actions, "CLOSE", new Vector2(94, 38), new Vector2(-180, -48));
        var apply = Button("Apply", actions, "APPLY NEXT RAID", new Vector2(190, 42), new Vector2(139, -48));
        apply.targetGraphic.color = new Color(0.38f, 0.36f, 0.22f, 1);

        root.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, CanvasPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateMarker()
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "LootPointMarker"; marker.transform.localScale = Vector3.one * 0.18f;
        var collider = marker.GetComponent<Collider>(); if (collider) UnityEngine.Object.DestroyImmediate(collider);
        PrefabUtility.SaveAsPrefabAsset(marker, MarkerPath); UnityEngine.Object.DestroyImmediate(marker);
    }

    private static void CreateGizmo()
    {
        var root = new GameObject("PlacementGizmo");
        Axis(root.transform, "X", Vector3.right, Color.red); Axis(root.transform, "Y", Vector3.up, Color.green); Axis(root.transform, "Z", Vector3.forward, Color.blue);
        PrefabUtility.SaveAsPrefabAsset(root, GizmoPath); UnityEngine.Object.DestroyImmediate(root);
    }

    private static void Axis(Transform parent, string name, Vector3 direction, Color color)
    {
        var axis = GameObject.CreatePrimitive(PrimitiveType.Cylinder); axis.name = name; axis.transform.SetParent(parent, false);
        axis.transform.localScale = new Vector3(0.025f, 0.35f, 0.025f); axis.transform.localPosition = direction * 0.35f;
        axis.transform.up = direction; axis.GetComponent<Renderer>().sharedMaterial.color = color;
        var collider = axis.GetComponent<Collider>(); if (collider) UnityEngine.Object.DestroyImmediate(collider);
    }

    private static void LabelInput(Transform parent, string name, string label, float x, float y)
    {
        var holder = Rect(name, parent, new Vector2(148, 52), new Vector2(x, y));
        Text("Label", holder, label, 8, TextAnchor.UpperLeft, new Vector2(140, 17), new Vector2(0, 16));
        Input("Input", holder, "0", new Vector2(140, 29), new Vector2(0, -8));
    }

    private static RectTransform Input(string name, Transform parent, string placeholder, Vector2 size, Vector2 position)
    {
        var image = Image(name, parent, new Color(0.015f, 0.02f, 0.021f, 1), size, position); image.raycastTarget = true;
        var input = image.gameObject.AddComponent<InputField>();
        var value = Text("Text", image.transform, "", 13, TextAnchor.MiddleLeft, new Vector2(size.x - 18, size.y - 4), Vector2.zero); value.raycastTarget = false;
        var hint = Text("Placeholder", image.transform, placeholder, 13, TextAnchor.MiddleLeft, new Vector2(size.x - 18, size.y - 4), Vector2.zero); hint.color = new Color(0.48f, 0.51f, 0.5f, 1); hint.raycastTarget = false;
        input.textComponent = value; input.placeholder = hint; input.targetGraphic = image;
        return image.rectTransform;
    }

    private static Button Button(string name, Transform parent, string label, Vector2 size, Vector2 position)
    {
        var image = Image(name, parent, new Color(0.18f, 0.23f, 0.24f, 1), size, position); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Text("Label", image.transform, label, 11, TextAnchor.MiddleCenter, size - new Vector2(8, 4), Vector2.zero);
        return button;
    }

    private static Toggle Toggle(string name, Transform parent, string label, Vector2 position)
    {
        var holder = Rect(name, parent, new Vector2(140, 26), position);
        var background = Image("Background", holder, new Color(0.02f, 0.025f, 0.026f, 1), new Vector2(20, 20), new Vector2(-57, 0)); background.raycastTarget = true;
        var check = Image("Checkmark", background.transform, new Color(0.62f, 0.78f, 0.68f, 1), new Vector2(13, 13), Vector2.zero);
        var toggle = holder.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = background; toggle.graphic = check;
        Text("Label", holder, label, 9, TextAnchor.MiddleLeft, new Vector2(112, 24), new Vector2(15, 0));
        return toggle;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }

    private static Image Image(string name, Transform parent, Color color, Vector2 size, Vector2 position, bool stretch = false)
    {
        var rect = Rect(name, parent, size, position); if (stretch) Stretch(rect);
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }

    private static Text Text(string name, Transform parent, string value, int size, TextAnchor anchor, Vector2 dimensions, Vector2 position)
    {
        var rect = Rect(name, parent, dimensions, position); var text = rect.gameObject.AddComponent<Text>(); text.font = _font; text.text = value;
        text.fontSize = size; text.alignment = anchor; text.color = new Color(0.82f, 0.84f, 0.81f, 1); text.horizontalOverflow = HorizontalWrapMode.Overflow; return text;
    }

    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static string GetArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++) if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}
