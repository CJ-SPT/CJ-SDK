using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SeasonalPerks.UI;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SeasonalPerksUiPreview
{
    private const string Root = "Assets/Mods/SeasonalPerks.Assets";

    [MenuItem("SDK/Seasonal Perks/Render UI previews")]
    public static void Render()
    {
        var previous = PlayerSettings.colorSpace;
        try
        {
            // Both supplied players serialize m_ActiveColorSpace=0 (Gamma).
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            RenderGamma();
        }
        finally
        {
            PlayerSettings.colorSpace = previous;
        }
    }

    private static void RenderGamma()
    {
        var project = Path.GetFullPath(Path.Combine(Application.dataPath, "../../SeasonalPerks"));
        var output = Path.Combine(project, "Research/UI");
        Directory.CreateDirectory(output);
        var catalogue = JObject.Parse(
            File.ReadAllText(Path.Combine(project, "data/catalogue.json"))
        );
        var locale = JObject.Parse(File.ReadAllText(Path.Combine(project, "data/locales/en.json")));
        var implemented = new HashSet<string>(
            Regex
                .Matches(
                    File.ReadAllText(Path.Combine(project, "Shared/EffectSupport.cs"))
                        .Split(new[] { "};" }, StringSplitOptions.None)[0],
                    "\"([a-z_]+)\""
                )
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
        );
        var icons = new Dictionary<string, string>();
        var perks = new List<PerkEntry>();
        foreach (var common in new[] { true, false })
        {
            foreach (JObject perk in catalogue[common ? "common" : "personal"])
            {
                var id = (string)perk["id"];
                var effects = (JArray)perk["effects"];
                var supported =
                    effects.Count > 0
                    && effects.All(effect => implemented.Contains((string)effect["effectId"]));
                perks.Add(
                    new PerkEntry
                    {
                        Id = id,
                        Name = (string)locale[id + " name"],
                        Description = (string)locale[id + " description"],
                        Points = (int?)perk["points"] ?? 0,
                        Common = common,
                        Enabled = common && supported,
                        Unavailable = supported
                            ? ""
                            : "This modifier is not yet available in the backport.",
                        Conflicts = perk["mutuallyExclusiveSeasonalPerkIds"]
                            .Values<string>()
                            .ToArray(),
                    }
                );
                icons[id] = Root + "/Icons/" + Path.GetFileName((string)perk["imageUrl"]);
            }
        }
        var state = new ScreenState
        {
            Perks = perks.ToArray(),
            Characters = new[]
            {
                new CharacterEntry
                {
                    Mode = "normal",
                    Name = "Standard PMC",
                    Level = 24,
                    Exists = true,
                },
                new CharacterEntry
                {
                    Mode = "seasonal",
                    Name = "Seasonal PMC",
                    Level = 1,
                    Exists = false,
                },
            },
        };
        var sprites = new Dictionary<string, Sprite>();
        foreach (
            var resolution in new[]
            {
                new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440),
                new Vector2Int(1902, 992),
            }
        )
        {
            var cameraObject = new GameObject("SeasonalPreviewCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.cullingMask = 1 << 5;
            var target = new RenderTexture(resolution.x, resolution.y, 24);
            camera.targetTexture = target;
            var canvasObject = new GameObject(
                "SeasonalPreviewCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1800, 980);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var view = new SeasonalScreen(
                canvasObject.transform,
                name => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/UI/" + name + ".prefab"),
                AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Bender.ttf")
            );
            view.IconRequested = (id, image) =>
            {
                if (!sprites.TryGetValue(id, out var sprite))
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(icons[id]);
                    sprites[id] = sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(.5f, .5f)
                    );
                }
                image.sprite = sprite;
                image.color = Color.white;
                image.enabled = true;
            };
            view.ArtworkRequested = (name, image) =>
            {
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    Root + "/SelectionArtwork/" + name + ".png"
                );
                if (!image.sprite)
                {
                    throw new InvalidOperationException("Missing UI artwork sprite: " + name);
                }
                image.enabled = true;
            };
            view.CharacterRequested = (mode, image) =>
            {
                // Editor layout stand-ins only. The client loads each profile's actual equipment model.
                image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    Root
                        + "/SelectionArtwork/"
                        + (mode == "normal" ? "normal-empty" : "seasonal-empty")
                        + ".png"
                );
                image.color = Color.white;
                image.uvRect = new Rect(0, 0, 1, 1);
            };
            view.GlowMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                Root + "/UI/selection-additive.mat"
            );
            void Capture(string name)
            {
                foreach (var transform in canvasObject.GetComponentsInChildren<Transform>(true))
                {
                    transform.gameObject.layer = 5;
                }
                Canvas.ForceUpdateCanvases();
                view.Fit();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)view.Root.transform);
                foreach (var label in canvasObject.GetComponentsInChildren<Text>())
                {
                    label.font.RequestCharactersInTexture(
                        label.text,
                        label.fontSize,
                        label.fontStyle
                    );
                }
                foreach (var graphic in canvasObject.GetComponentsInChildren<Graphic>())
                {
                    graphic.SetAllDirty();
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
                texture.Apply();
                File.WriteAllBytes(
                    Path.Combine(output, name + "-" + resolution.y + ".png"),
                    texture.EncodeToPNG()
                );
                Object.DestroyImmediate(texture);
                RenderTexture.active = previous;
            }
            state.ActiveMode = "normal";
            state.Characters[1].Exists = false;
            state.Selected = Array.Empty<string>();
            view.StartupSelection = true;
            view.SetState(state, ScreenPage.Characters);
            view.Open(ScreenPage.Characters);
            Capture("characters");
            view.Root.GetComponentsInChildren<ProfileCardHover>()
                .Single(card => card.Seasonal)
                .Apply(1);
            Capture("characters-hover");
            view.StartupSelection = false;
            view.ShowPage(ScreenPage.Personal);
            Capture("selection-new");
            view.ShowPage(ScreenPage.CreationCommon);
            Capture("creation-common");
            var hoverEvents = new GameObject("HoverPreviewEvents", typeof(EventSystem));
            var pointer = new PointerEventData(hoverEvents.GetComponent<EventSystem>());
            var commonHover = view.Root.GetComponentsInChildren<PerkCardHover>().First();
            commonHover.OnPointerEnter(pointer);
            Capture("creation-common-hover");
            view.ShowPage(ScreenPage.CreationPersonal);
            Capture("creation-personal");
            var personalHover = view.Root.GetComponentsInChildren<PerkCardHover>().First();
            personalHover.OnPointerEnter(pointer);
            Capture("creation-personal-hover");
            view.Root.GetComponentsInChildren<Button>()
                .Single(button => button.name == "NEXT")
                .onClick.Invoke();
            Capture("creation-confirmation-empty");
            view.DismissDialog();
            state.Selected = perks
                .Where(perk =>
                    new[]
                    {
                        "Hercules",
                        "Polydipsia",
                        "Chronic Fatigue Syndrome",
                        "Exhaustion",
                        "Hemophilia",
                    }.Contains(perk.Name)
                )
                .Select(perk => perk.Id)
                .ToArray();
            view.SetState(state, ScreenPage.CreationPersonal);
            view.Root.GetComponentsInChildren<Button>()
                .Single(button => button.name == "NEXT")
                .onClick.Invoke();
            Capture("creation-confirmation");
            view
                .Root.GetComponentsInChildren<ScrollRect>()
                .Single(scroll => scroll.name == "SelectedModifiers")
                .verticalNormalizedPosition = 0;
            Capture("creation-confirmation-scrolled");
            view.DismissDialog();
            view.SetBusy(true);
            Capture("creation-submitting");
            view.SetBusy(false);
            Object.DestroyImmediate(hoverEvents);
            state.Characters[1].Exists = true;
            state.Selected = perks
                .Where(perk =>
                    new[]
                    {
                        "Hercules",
                        "Polydipsia",
                        "Chronic Fatigue Syndrome",
                        "Exhaustion",
                    }.Contains(perk.Name)
                )
                .Select(perk => perk.Id)
                .ToArray();
            view.SetState(state, ScreenPage.Personal);
            Capture("selection-saved");
            view.ReviewSelection();
            Capture("confirmation");
            view.DismissDialog();
            view.ShowPage(ScreenPage.Global);
            Capture("globals");
            state.ActiveMode = "seasonal";
            var fullScreen = view;
            fullScreen.Root.SetActive(false);
            view = new SeasonalScreen(
                canvasObject.transform,
                name => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/UI/" + name + ".prefab"),
                AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/Bender.ttf"),
                true
            );
            view.IconRequested = fullScreen.IconRequested;
            view.ArtworkRequested = fullScreen.ArtworkRequested;
            // Match the native content area below the experience bar and tab strip.
            var embeddedRoot = (RectTransform)view.Root.transform;
            embeddedRoot.anchorMin = new Vector2(0, .05f);
            embeddedRoot.anchorMax = new Vector2(1, .73f);
            view.SetState(state, ScreenPage.Summary);
            view.Open(ScreenPage.Summary);
            Capture("skills-perks");
            var modifierChecks = SeasonalPerksModifiersChecks.Run(view, state);
            File.WriteAllText(
                Path.Combine(output, "modifier-checks-" + resolution.y + ".json"),
                new JObject
                {
                    ["passed"] = modifierChecks.Length,
                    ["checks"] = new JArray(modifierChecks),
                }.ToString()
            );
            view.SetState(state, ScreenPage.Summary);
            var modifierScroll = view.Root.GetComponentInChildren<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            modifierScroll.verticalNormalizedPosition = 0;
            Capture("skills-perks-bottom");
            state.ActiveMode = "normal";
            view.SetState(state, ScreenPage.Summary);
            Capture("skills-normal");
            state.ActiveMode = "seasonal";
            view.Dispose();
            view = fullScreen;
            var checks = SeasonalPerksUiChecks.Run(view, perks.ToArray());
            File.WriteAllText(
                Path.Combine(output, "interaction-checks-" + resolution.y + ".json"),
                new JObject
                {
                    ["passed"] = checks.Length,
                    ["checks"] = new JArray(checks),
                }.ToString()
            );
            view.Dispose();
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(cameraObject);
        }
        foreach (var sprite in sprites.Values)
        {
            Object.DestroyImmediate(sprite);
        }
        Debug.Log("Seasonal UI: rendered 42 previews using the actual UI project views.");
    }
}
