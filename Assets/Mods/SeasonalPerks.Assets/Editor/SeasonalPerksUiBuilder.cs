using System;
using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Rebuild recovered layouts with SPT-compatible uGUI components. Live MonoScripts are never bundled.
public static class SeasonalPerksUiBuilder
{
    private const string Root = "Assets/Mods/SeasonalPerks.Assets";
    private static Font _font;

    [MenuItem("SDK/Seasonal Perks/Build recovered UI")]
    public static void Build()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Directory.CreateDirectory(Root + "/UI");
        var artwork = JArray.Parse(File.ReadAllText(Root + "/SelectionArtwork/provenance.json"));
        var hubArtwork = JArray.Parse(File.ReadAllText(Root + "/HubArtwork/provenance.json"));
        foreach (JObject entry in artwork) entry["folder"] = "SelectionArtwork";
        foreach (JObject entry in hubArtwork) { entry["folder"] = "HubArtwork"; artwork.Add(entry); }
        foreach (var entry in artwork)
        {
            var path = Root + "/" + (string)entry["folder"] + "/" + (string)entry["file"];
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            var border = entry["border"].Values<float>().ToArray();
            importer.spriteBorder = new Vector4(border[0], border[1], border[2], border[3]);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        var materialPath = Root + "/UI/selection-additive.mat";
        var audio = JArray.Parse(File.ReadAllText(Root + "/Audio/provenance.json"));
        foreach (var entry in audio)
        {
            var path = Root + "/Audio/" + (string)entry["file"];
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }
        if (!AssetDatabase.LoadAssetAtPath<Material>(materialPath))
        {
            AssetDatabase.CreateAsset(
                new Material(Shader.Find("SeasonalPerks/UI Additive")),
                materialPath
            );
        }
        var paths = Directory
            .GetFiles(Root + "/Recovered", "*.json")
            .Where(source =>
                Path.GetFileName(source).StartsWith("level")
                || Path.GetFileName(source).StartsWith("sharedassets")
            )
            .Select(source =>
            {
                var root = Recover(JObject.Parse(File.ReadAllText(source)), null);
                root.SetActive(true);
                var path = Root + "/UI/" + Path.GetFileNameWithoutExtension(source) + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
                return path;
            })
            .Concat(
                new[]
                {
                    SeasonalSelectionCameraBuilder.Build(),
                    Root + "/Fonts/Bender.ttf",
                    materialPath,
                }
            )
            .Concat(artwork.Select(entry => Root + "/" + (string)entry["folder"] + "/" + (string)entry["file"]))
            .Concat(audio.Select(entry => Root + "/Audio/" + (string)entry["file"]))
            .Concat(Directory.GetFiles(Root + "/HubMedia", "*.webm"))
            .ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var output = Path.GetFullPath(
            Path.Combine(Application.dataPath, "../../SeasonalPerks/Client/Resources")
        );
        Directory.CreateDirectory(output);
        var result = BuildPipeline.BuildAssetBundles(
            output,
            new[]
            {
                new AssetBundleBuild
                {
                    assetBundleName = "seasonalperks_ui.bundle",
                    assetNames = paths,
                },
            },
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64
        );
        if (result == null)
        {
            throw new Exception("Seasonal UI build failed");
        }
        // UI artwork is bundled; perk icons must only be served by the server.
        foreach (var dependency in AssetDatabase.GetDependencies(paths, true))
        {
            if (dependency.StartsWith(Root + "/Icons/", StringComparison.Ordinal))
            {
                throw new Exception("Perk icon leaked into UI bundle: " + dependency);
            }
        }
        Debug.Log(
            "Seasonal UI: built "
                + paths.Length
                + " UI assets, including "
                + artwork.Count
                + " artwork sprites; no perk icons bundled."
        );
    }

    static GameObject Recover(JObject n, Transform parent)
    {
        var go = new GameObject((string)n["name"], typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        var t = n["rect"];
        if (t != null)
        {
            r.anchorMin = V(t["m_AnchorMin"]);
            r.anchorMax = V(t["m_AnchorMax"]);
            r.pivot = V(t["m_Pivot"]);
            r.anchoredPosition = V(t["m_AnchoredPosition"]);
            r.sizeDelta = V(t["m_SizeDelta"]);
        }
        foreach (var c in n["components"])
        {
            var f = c["fields"];
            if (f == null)
            {
                continue;
            }
            switch ((string)c["type"])
            {
                case "UnityEngine.UI.Image":
                    var image = go.AddComponent<Image>();
                    image.color = ImageColor(go.name, C(f["m_Color"]));
                    if (c["artwork"] != null && c["artwork"].Type == JTokenType.String)
                    {
                        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/HubArtwork/" + (string)c["artwork"] + ".png");
                        image.color = C(f["m_Color"]);
                        image.type = (Image.Type)((int?)f["m_Type"] ?? 0);
                        image.preserveAspect = B(f, "m_PreserveAspect");
                    }
                    image.raycastTarget = false;
                    // Image sprites are intentionally omitted. Icons are supplied per perk at runtime.
                    if (go.name == "NetworkImageView")
                    {
                        image.color = Color.clear;
                    }
                    break;
                case "TMPro.TextMeshProUGUI":
                case "CustomTextMeshProUGUI":
                    var text = go.AddComponent<Text>();
                    text.font = _font;
                    text.text = (string)f["m_text"] ?? "";
                    text.fontSize = Mathf.RoundToInt((float?)f["m_fontSize"] ?? 20);
                    text.color = C(f["m_fontColor"]);
                    text.alignment = TextAnchor.MiddleLeft;
                    text.raycastTarget = false;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    break;
                case "UnityEngine.UI.LayoutElement":
                    var le = go.AddComponent<LayoutElement>();
                    le.minWidth = F(f, "m_MinWidth", -1);
                    le.minHeight = F(f, "m_MinHeight", -1);
                    le.preferredWidth = F(f, "m_PreferredWidth", -1);
                    le.preferredHeight = F(f, "m_PreferredHeight", -1);
                    le.flexibleWidth = F(f, "m_FlexibleWidth", -1);
                    le.flexibleHeight = F(f, "m_FlexibleHeight", -1);
                    le.ignoreLayout = B(f, "m_IgnoreLayout");
                    break;
                case "UnityEngine.UI.ContentSizeFitter":
                    var fit = go.AddComponent<ContentSizeFitter>();
                    fit.horizontalFit = (ContentSizeFitter.FitMode)(int)f["m_HorizontalFit"];
                    fit.verticalFit = (ContentSizeFitter.FitMode)(int)f["m_VerticalFit"];
                    break;
                case "UnityEngine.UI.HorizontalLayoutGroup":
                case "UnityEngine.UI.VerticalLayoutGroup":
                    HorizontalOrVerticalLayoutGroup layout =
                        (string)c["type"] == "UnityEngine.UI.HorizontalLayoutGroup"
                            ? (HorizontalOrVerticalLayoutGroup)
                                go.AddComponent<HorizontalLayoutGroup>()
                            : go.AddComponent<VerticalLayoutGroup>();
                    layout.spacing = F(f, "m_Spacing");
                    layout.childControlWidth = B(f, "m_ChildControlWidth");
                    layout.childControlHeight = B(f, "m_ChildControlHeight");
                    layout.childForceExpandWidth = B(f, "m_ChildForceExpandWidth");
                    layout.childForceExpandHeight = B(f, "m_ChildForceExpandHeight");
                    layout.childAlignment = (TextAnchor)(int)f["m_ChildAlignment"];
                    var pad = f["m_Padding"];
                    layout.padding = new RectOffset(
                        (int)pad["m_Left"],
                        (int)pad["m_Right"],
                        (int)pad["m_Top"],
                        (int)pad["m_Bottom"]
                    );
                    break;
                case "CanvasGroup":
                    var cg = go.AddComponent<CanvasGroup>();
                    cg.alpha = F(f, "m_Alpha", 1);
                    cg.blocksRaycasts = true;
                    break;
                case "UnityEngine.UI.RectMask2D":
                    go.AddComponent<RectMask2D>();
                    break;
            }
        }
        foreach (JObject child in n["children"])
        {
            Recover(child, go.transform);
        }
        go.SetActive((bool?)n["active"] ?? true);
        return go;
    }

    static bool B(JToken f, string k) => f[k] != null && f[k].ToString() is "True" or "true" or "1";

    static Color ImageColor(string name, Color captured)
    {
        // Captured sprite tints are not the sprite itself. Opaque white masks must not
        // become white rectangles when reconstructing a layout without bundled images.
        if (
            name
            is "BackgroundGrid"
                or "BackgroundSadow"
                or "Corners"
                or "UpR"
                or "UpL"
                or "DownR"
                or "DownL"
        )
        {
            return Color.clear;
        }
        if (name is "BackgroundGradient" or "Window" or "Background_inside")
        {
            return new Color(.055f, .055f, .045f, .99f);
        }
        if (captured.r > .95f && captured.g > .95f && captured.b > .95f)
        {
            return new Color(.4f, .39f, .32f, captured.a * .35f);
        }
        return captured;
    }

    static float F(JToken f, string k, float fallback = 0) => (float?)f[k] ?? fallback;

    static Vector2 V(JToken t) => new Vector2((float)t["x"], (float)t["y"]);

    static Color C(JToken t) =>
        t == null
            ? Color.white
            : new Color((float)t["r"], (float)t["g"], (float)t["b"], (float)t["a"]);
}
