using System;
using System.Collections.Generic;
using System.Linq;
using SeasonalPerks.UI.Audio;
using SeasonalPerks.UI.Creation;
using SeasonalPerks.UI.Models;
using SeasonalPerks.UI.Modifiers;
using SeasonalPerks.UI.Profiles;
using SeasonalPerks.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SeasonalPerksUiChecks
{
    public static string[] Run(SeasonalScreen view, PerkEntry[] perks)
    {
        var results = new List<string>();
        var sounds = new List<InterfaceSound>();
        view.SoundRequested = sound => sounds.Add(sound);
        var eventObject = new GameObject("PreviewEvents", typeof(EventSystem));
        var events = eventObject.GetComponent<EventSystem>();
        void Check(bool value, string description)
        {
            if (!value)
            {
                throw new Exception("UI check failed: " + description);
            }
            results.Add(description);
        }
        Button Button(string name) =>
            view
                .Root.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == name && button.gameObject.activeInHierarchy);
        void Click(Button button) =>
            ExecuteEvents.Execute(
                button.gameObject,
                new PointerEventData(events) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler
            );
        string Id(string name) => perks.Single(perk => perk.Name == name).Id;
        var state = new ScreenState
        {
            Perks = perks,
            Characters = new[]
            {
                new CharacterEntry { Mode = "normal", Exists = true },
                new CharacterEntry { Mode = "seasonal", Exists = false },
            },
        };
        view.SetBusy(false);
        view.StartupSelection = true;
        view.SetState(state, ScreenPage.Characters);
        view.Open(ScreenPage.Characters);
        var hoverSounds = new List<bool>();
        view.ProfileHoverSound = seasonal => hoverSounds.Add(seasonal);
        var hoverCards = view.Root.GetComponentsInChildren<ProfileCardHover>();
        var normalCard = hoverCards.Single(card => !card.Seasonal);
        var seasonalSoundCard = hoverCards.Single(card => card.Seasonal);
        var pointer = new PointerEventData(events);
        ExecuteEvents.Execute(normalCard.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(normalCard.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Check(
            hoverSounds.SequenceEqual(new[] { false }),
            "Normal card plays its hover sound once per entry"
        );
        ExecuteEvents.Execute(normalCard.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        ExecuteEvents.Execute(normalCard.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Check(hoverSounds.Count == 2, "Leaving and reentering a card plays hover sound again");
        ExecuteEvents.Execute(
            seasonalSoundCard.gameObject,
            pointer,
            ExecuteEvents.pointerEnterHandler
        );
        Check(
            hoverSounds.Last() && hoverSounds.Count == 3,
            "Seasonal card requests its distinct hover clip"
        );
        view.SetBusy(true);
        ExecuteEvents.Execute(
            seasonalSoundCard.gameObject,
            pointer,
            ExecuteEvents.pointerExitHandler
        );
        ExecuteEvents.Execute(
            seasonalSoundCard.gameObject,
            pointer,
            ExecuteEvents.pointerEnterHandler
        );
        Check(hoverSounds.Count == 3, "Busy profile selection suppresses hover feedback");
        view.SetBusy(false);
        ExecuteEvents.Execute(normalCard.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        ExecuteEvents.Execute(
            seasonalSoundCard.gameObject,
            pointer,
            ExecuteEvents.pointerExitHandler
        );
        view.ProfileHoverSound = null;
        Canvas.ForceUpdateCanvases();
        view.Fit();
        Canvas.ForceUpdateCanvases();
        var selection = view
            .Root.GetComponentsInChildren<RectTransform>()
            .Single(rect => rect.name == "LiveProfileSelection");
        var background = (RectTransform)selection.Find("background");
        var viewportCorners = new Vector3[4];
        var backgroundCorners = new Vector3[4];
        ((RectTransform)view.Root.transform).GetWorldCorners(viewportCorners);
        background.GetWorldCorners(backgroundCorners);
        Check(
            viewportCorners
                .Zip(backgroundCorners, (a, b) => Vector3.Distance(a, b))
                .All(distance => distance < .1f),
            "Selection background fills the entire viewport without side borders"
        );
        var topGlow = selection.Find("top-glow-seasonal").GetComponent<Image>();
        Check(
            topGlow.enabled
                && topGlow.sprite
                && topGlow.material.shader.name == "SeasonalPerks/UI Additive"
                && !topGlow.raycastTarget,
            "Original green screen glow uses additive blending without blocking input"
        );
        var profileCards = view.Root.GetComponentsInChildren<ProfileCardHover>();
        Check(profileCards.Length == 2, "Startup has exactly Normal and Seasonal cards");
        Check(
            profileCards.All(card =>
                ((RectTransform)card.transform).sizeDelta == new Vector2(390, 800)
            ),
            "Live card proportions are preserved"
        );
        Check(
            !view.Root.GetComponentsInChildren<Button>().Any(button => button.name == "BACK"),
            "Startup requires a profile selection"
        );
        Check(
            !view.Root.GetComponentsInChildren<Text>().Any(text => text.text == "PERSONAL PERKS"),
            "Startup omits the editor navigation bar"
        );
        var chosen = "";
        view.SwitchRequested = mode => chosen = mode;
        sounds.Clear();
        ExecuteEvents.Execute(
            Button("Select-normal").gameObject,
            pointer,
            ExecuteEvents.pointerEnterHandler
        );
        ExecuteEvents.Execute(
            Button("Select-normal").gameObject,
            pointer,
            ExecuteEvents.pointerEnterHandler
        );
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.ButtonHover }),
            "Button hover plays once per entry"
        );
        Button("Select-normal").interactable = false;
        Click(Button("Select-normal"));
        Check(sounds.Count == 1, "Disabled buttons do not play click feedback");
        Button("Select-normal").interactable = true;
        sounds.Clear();
        Click(Button("Select-normal"));
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.ButtonClick }),
            "Profile selection plays exactly one standard click"
        );
        Check(chosen == "normal", "Select routes the chosen profile");
        var seasonalCard = profileCards.Single(card => card.Seasonal);
        Check(
            !seasonalCard.Details.blocksRaycasts,
            "Collapsed seasonal details do not intercept input"
        );
        seasonalCard.Apply(1);
        Check(
            Math.Abs(
                seasonalCard.Info.anchoredPosition.y
                    + 24
                    - seasonalCard.InfoBackground.sizeDelta.y
                    + 400
            ) < .1f,
            "Expanded information gradient reaches the card footer"
        );
        Check(
            seasonalCard.InfoBackground.GetComponent<Image>().type == Image.Type.Sliced
                && seasonalCard.InfoBackground.GetComponent<Image>().sprite.border.w == 75,
            "Information gradient preserves the original sliced fade"
        );
        seasonalCard.AnimateGlow(2);
        Check(
            seasonalCard.IdleFrames.Length == 3
                && Math.Abs(seasonalCard.IdleFrames[0].color.a - .4f) < .01f
                && Math.Abs(seasonalCard.IdleFrames[1].color.a - .4f) < .01f,
            "Seasonal glow crossfades original artwork frames"
        );
        Check(
            seasonalCard.Details.blocksRaycasts && seasonalCard.Info.anchoredPosition.y == 305,
            "Seasonal hover expands details below the header"
        );
        Check(
            !seasonalCard.Details.GetComponentsInChildren<Button>().Any(),
            "Live hover card has no custom editor shortcut buttons"
        );
        Check(
            seasonalCard
                .Details.GetComponentsInChildren<Text>()
                .Any(text => text.name == "StatsTitle" && text.text == "SEASON STATS"),
            "Live season statistics section remains visible before backend implementation"
        );
        Check(
            seasonalCard
                .Details.GetComponentsInChildren<Text>()
                .Where(text => text.name == "Caption")
                .Select(text => text.text)
                .SequenceEqual(
                    new[] { "Battle Pass rewards", "Story Chapters", "K/D", "Survivals" }
                ),
            "Hover card preserves live statistic labels and order"
        );
        Check(
            seasonalCard
                .Details.GetComponentsInChildren<Text>()
                .Where(text => text.name == "Value")
                .All(text => text.text == ""),
            "Unimplemented statistics do not invent progression values"
        );
        Check(
            seasonalCard.Description.text
                == "• Temporary seasonal character in Tarkov\n• Progress resets each season",
            "Seasonal card preserves the captured live description"
        );
        view.SetBusy(true);
        chosen = "";
        Click(Button("Select-normal"));
        Check(chosen == "", "Busy startup blocks duplicate selection");
        view.SetBusy(false);
        view.RequestClose();
        Check(
            view.Page == ScreenPage.Characters && view.Root.activeSelf,
            "Escape keeps the startup selector open"
        );
        view.SwitchRequested = null;
        var originalIdentity = view.IdentityRequested;
        CreationDraft draft = null;
        Action completeIdentity = null;
        view.IdentityRequested = (host, value, complete, back) =>
        {
            draft = value;
            completeIdentity = complete;
            return new IdentityStub(back);
        };
        Click(Button("Select-seasonal"));
        Check(
            view.Page == ScreenPage.CreationIdentity,
            "New seasonal card opens native identity creation"
        );
        draft.Nickname = "CreationTest";
        draft.Side = "Bear";
        draft.HeadId = "preview-head";
        draft.VoiceId = "preview-voice";
        completeIdentity();
        Check(
            view.Page == ScreenPage.CreationCommon,
            "Identity advances to common modifiers before personal perks"
        );
        Check(
            view.Root.GetComponentsInChildren<Button>()
                .Count(button => perks.Any(perk => perk.Common && perk.Id == button.name)) == 6,
            "Creation overview includes all six common modifiers"
        );
        var commonHover = Button(Id("Handyman")).GetComponent<PerkCardHover>();
        ExecuteEvents.Execute(commonHover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Check(commonHover.Highlight.activeSelf, "Common cards highlight on pointer entry");
        Check(
            !view.Root.GetComponentsInChildren<Transform>().Any(node => node.name == "PerkTooltip"),
            "Common card hover does not create a tooltip"
        );
        var commonBackground = commonHover.transform.Find("Content/Background");
        Check(
            commonBackground.Find("BackgroundGrid").GetComponent<Image>().sprite.name
                == "modifier-grid"
                && commonBackground.Find("BackgroundGrid").GetComponent<Image>().type
                    == Image.Type.Tiled
                && commonBackground.Find("BackgroundGradient").GetComponent<Image>().sprite.name
                    == "modifier-gradient"
                && commonBackground.Find("BackgroundSadow").GetComponent<Image>().sprite.name
                    == "modifier-shadow",
            "Common cards restore the original gradient, tiled grid and shadow"
        );
        ExecuteEvents.Execute(commonHover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Check(!commonHover.Highlight.activeSelf, "Common highlight clears on pointer exit");
        Click(Button(Id("Handyman")));
        Check(view.Selected.Length == 0, "Common modifiers cannot be selected during creation");
        Click(Button("NEXT"));
        Check(view.Page == ScreenPage.CreationPersonal, "Common Next opens personal modifiers");
        var personalHover = Button(Id("Hercules")).GetComponent<PerkCardHover>();
        Check(
            personalHover.Idle.activeSelf
                && !personalHover.Selected.activeSelf
                && !personalHover.Highlight.activeSelf,
            "Personal cards start with only their idle tint"
        );
        ExecuteEvents.Execute(personalHover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Check(
            personalHover.Highlight.activeSelf && !personalHover.Idle.activeSelf,
            "Personal hover replaces the idle tint with the native neutral highlight"
        );
        Check(
            !view.Root.GetComponentsInChildren<Transform>().Any(node => node.name == "PerkTooltip"),
            "Personal card hover does not create a tooltip"
        );
        Click(Button(Id("Hercules")));
        Check(
            personalHover.Highlight.activeSelf && !personalHover.Selected.activeSelf,
            "Selecting beneath the pointer retains the hover highlight"
        );
        ExecuteEvents.Execute(personalHover.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Check(
            personalHover.Selected.activeSelf
                && !personalHover.Idle.activeSelf
                && !personalHover.Highlight.activeSelf,
            "Pointer exit reveals the selected tint without a stuck hover"
        );
        ExecuteEvents.Execute(personalHover.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        view.SetBusy(true);
        Check(
            !personalHover.Highlight.activeSelf,
            "Busy state clears a card highlight immediately"
        );
        view.SetBusy(false);
        Check(!Button("NEXT").interactable, "Creation Next rejects overspending");
        Click(Button(Id("Allergic")));
        Check(!view.Selected.Contains(Id("Allergic")), "Creation excludes unsupported modifiers");
        Click(Button("BACK"));
        Check(
            view.Page == ScreenPage.CreationCommon && view.Selected.Contains(Id("Hercules")),
            "Creation Back preserves selected perks"
        );
        Click(Button("BACK"));
        Check(
            view.Page == ScreenPage.CreationIdentity
                && draft.Appearance
                && draft.Nickname == "CreationTest",
            "Back from common restores the appearance step and nickname"
        );
        completeIdentity();
        Click(Button("NEXT"));
        sounds.Clear();
        Click(Button("RESET"));
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.PerkReset }),
            "Creation reset uses the original perk reset clip"
        );
        Check(view.DialogOpen, "Creation Reset asks before clearing the draft");
        Click(Button("CANCEL"));
        Check(
            view.Selected.Contains(Id("Hercules")),
            "Cancelling Reset preserves creation selections"
        );
        Click(Button(Id("Hercules")));
        var creationSaves = 0;
        view.SaveRequested = () => creationSaves++;
        sounds.Clear();
        Click(Button("NEXT"));
        Check(
            view.DialogOpen && creationSaves == 0,
            "Creation Next opens Save modifiers without submitting"
        );
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.ButtonClick }),
            "Opening Save modifiers plays one native button click"
        );
        Check(
            view.Root.GetComponentsInChildren<Text>().Any(text => text.text == "Save modifiers")
                && view.Root.GetComponentsInChildren<Text>()
                    .Any(text => text.text == "No modifiers selected"),
            "Empty creation review uses the live caption and empty state"
        );
        view.RequestCloseFromInput();
        Check(
            !view.DialogOpen && creationSaves == 0 && view.Page == ScreenPage.CreationPersonal,
            "Escape cancels Save modifiers without creating or leaving the page"
        );
        Click(Button(Id("Hercules")));
        Click(Button(Id("Polydipsia")));
        Click(Button(Id("Chronic Fatigue Syndrome")));
        Click(Button(Id("Exhaustion")));
        Click(Button(Id("Hemophilia")));
        var selectedBeforeReview = view.Selected;
        Click(Button("NEXT"));
        Check(view.DialogOpen && creationSaves == 0, "Populated creation review does not submit");
        var review = view
            .Root.GetComponentsInChildren<ScrollRect>()
            .Single(scroll => scroll.name == "SelectedModifiers");
        Canvas.ForceUpdateCanvases();
        Check(
            review
                .content.GetComponentsInChildren<Image>()
                .Count(image => image.name == "Icon" && image.sprite && image.color.a > 0) == 5,
            "Every selected modifier displays its requested icon"
        );
        Check(
            view.Root.GetComponentsInChildren<Image>()
                .Single(image => image.name == "Frame")
                .fillCenter == false,
            "The recovered window border does not obscure the dialog background"
        );
        Check(
            review.content.GetChild(0).name == "PositiveGroup"
                && review.content.GetChild(1).name == "NegativeGroup",
            "Save modifiers lists positives before negatives"
        );
        Check(
            review.content.GetComponentsInChildren<Text>().Any(text => text.text == "POSITIVE (1)")
                && review
                    .content.GetComponentsInChildren<Text>()
                    .Any(text => text.text == "NEGATIVE (4)"),
            "Confirmation group counts reflect the draft"
        );
        Check(
            review.content.rect.height > review.viewport.rect.height,
            "Confirmation rows overflow into the scroll view"
        );
        review.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();
        Check(
            review.verticalNormalizedPosition < .01f,
            "Confirmation scroll reaches the final modifier"
        );
        Click(Button("CANCEL"));
        Check(
            !view.DialogOpen
                && creationSaves == 0
                && view.Selected.SequenceEqual(selectedBeforeReview),
            "Cancel returns to the unchanged modifier draft"
        );
        Click(Button("NEXT"));
        var acceptCreation = Button("ACCEPT");
        sounds.Clear();
        Click(acceptCreation);
        Check(!view.DialogOpen && creationSaves == 1, "Accept submits creation exactly once");
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.ButtonClick }),
            "Accept plays one native click"
        );
        Check(
            !Button("NEXT").IsInteractable() && !Button("BACK").IsInteractable(),
            "Creation locks Next and Back before dispatch"
        );
        Click(Button("NEXT"));
        view.RequestCloseFromInput();
        Check(
            creationSaves == 1 && view.Page == ScreenPage.CreationPersonal,
            "Pending creation blocks duplicate submission and Escape"
        );
        view.SetBusy(false);
        view.SetMessage("Retryable creation failure", true);
        Check(
            view.Page == ScreenPage.CreationPersonal
                && view.HeadId == "preview-head"
                && view.VoiceId == "preview-voice",
            "A failed creation preserves the chosen head and voice for retry"
        );
        Click(Button("NEXT"));
        Check(
            view.DialogOpen && creationSaves == 1,
            "Retry reopens Save modifiers before dispatch"
        );
        Click(Button("ACCEPT"));
        Check(
            creationSaves == 2 && !view.DialogOpen,
            "Retry dispatches once after the failed creation is unlocked"
        );
        view.SetBusy(false);
        view.SaveRequested = null;
        view.IdentityRequested = originalIdentity;
        view.StartupSelection = false;
        view.SetState(state, ScreenPage.Personal);
        view.Open(ScreenPage.Personal);
        Check(
            Button("REVIEW SELECTION").interactable,
            "Zero-perk creation is allowed at zero budget"
        );
        sounds.Clear();
        Click(Button(Id("Hercules")));
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.PerkOn }),
            "Selecting a perk plays only its toggle-on sound"
        );
        Check(view.Selected.Contains(Id("Hercules")), "Click selects a beneficial perk");
        Check(!Button("REVIEW SELECTION").interactable, "Overspending disables confirmation");
        var before = view.Selected.Length;
        Click(Button(Id("Allergic")));
        Check(view.Selected.Length == before, "Unavailable perk cannot be selected");
        Click(Button(Id("Average")));
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.PerkOn }),
            "Unavailable and conflicting perks do not play successful toggle sounds"
        );
        Check(!view.Selected.Contains(Id("Average")), "Mutual exclusion is enforced in the view");
        Check(
            view.Root.GetComponentsInChildren<Text>(true).Any(text => text.text == "CONFLICT"),
            "Conflicting cards have a visible state"
        );
        var search = view
            .Root.GetComponentsInChildren<InputField>()
            .Single(input => input.name == "Search");
        search.text = "Hercules";
        Check(
            view.Root.GetComponentsInChildren<Button>()
                .Count(button => perks.Any(perk => perk.Id == button.name)) == 1,
            "Search filters visible perk cards"
        );
        Check(view.Selected.Contains(Id("Hercules")), "Search preserves selections");
        search.text = "no perk matches this";
        Check(
            view.Root.GetComponentsInChildren<Text>().Count(text => text.name == "Empty") == 2,
            "Empty search results are shown in both groups"
        );
        search.text = "";
        var scroll = view.Root.GetComponentsInChildren<ScrollRect>().First();
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = .5f;
        sounds.Clear();
        Click(Button(Id("Hercules")));
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.PerkOff }),
            "Deselecting a perk plays only its toggle-off sound"
        );
        Check(
            Math.Abs(scroll.verticalNormalizedPosition - .5f) < .03f,
            "Selection changes preserve scroll position"
        );
        state.EnforceBudget = false;
        view.SetState(state, ScreenPage.Personal);
        Click(Button(Id("Hercules")));
        Check(
            Button("REVIEW SELECTION").interactable,
            "Configured free selection permits negative balance"
        );
        Click(Button("REVIEW SELECTION"));
        Check(view.DialogOpen, "Review opens a confirmation dialog");
        before = view.Selected.Length;
        sounds.Clear();
        Click(Button(Id("Hemophilia")));
        Check(sounds.Count == 0, "Modal blocks underlying perk feedback");
        Check(view.Selected.Length == before, "Underlying cards cannot change through a modal");
        Click(Button("CANCEL"));
        Check(!view.DialogOpen && view.Dirty, "Cancelling review preserves the draft");
        sounds.Clear();
        view.RequestCloseFromInput();
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.Back }),
            "Keyboard back plays one escape sound"
        );
        Check(view.DialogOpen, "Closing a dirty screen asks to discard the draft");
        Click(Button("CANCEL"));
        var saves = 0;
        view.SaveRequested = () => saves++;
        view.ReviewSelection();
        sounds.Clear();
        Click(Button("CREATE CHARACTER"));
        Check(
            sounds.SequenceEqual(new[] { InterfaceSound.ButtonClick }),
            "Confirmation plays exactly one click before dispatch"
        );
        Check(saves == 1 && !view.DialogOpen, "Confirmation dispatches one save request");
        view.SaveRequested = null;
        view.SetBusy(true, "Saving...");
        before = view.Selected.Length;
        sounds.Clear();
        Click(Button(Id("Hercules")));
        view.RequestCloseFromInput();
        Check(sounds.Count == 0, "Busy state suppresses perk and keyboard back sounds");
        Check(view.Selected.Length == before, "Busy state blocks edits");
        view.SetBusy(false);
        view.SetMessage("A retryable connection error.", true);
        Check(view.Selected.Contains(Id("Hercules")), "Errors preserve the draft selection");
        state.Characters[1].Exists = true;
        state.AllowEdits = false;
        state.Selected = new[] { Id("Hercules") };
        view.SetState(state, ScreenPage.Personal);
        Click(Button(Id("Hercules")));
        Check(
            view.Selected.Contains(Id("Hercules")) && !Button("REVIEW SELECTION").interactable,
            "Read-only server settings disable editing"
        );
        state.ActiveMode = "normal";
        view.SetState(state, ScreenPage.Summary);
        Check(
            !view
                .Root.GetComponentsInChildren<Button>()
                .Any(button => perks.Any(perk => perk.Id == button.name)),
            "Normal character summary has no seasonal perks"
        );
        state.CanOpenEditor = false;
        state.IsScav = true;
        view.SetState(state, ScreenPage.Summary);
        Check(
            !Button("REVIEW SELECTION").interactable,
            "Raid or server restrictions disable the summary edit action"
        );
        Check(
            view.Root.GetComponentsInChildren<Text>()
                .Any(text => text.text == "Seasonal PMC perks do not apply to your Scav."),
            "Scav summary explains PMC-only perks"
        );
        view.DismissDialog();
        view.SoundRequested = null;
        UnityEngine.Object.DestroyImmediate(eventObject);
        return results.ToArray();
    }

    private sealed class IdentityStub : ICreationIdentity
    {
        private readonly Action _back;

        public IdentityStub(Action back) => _back = back;

        public void Back() => _back();

        public void Dispose() { }
    }
}
