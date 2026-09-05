using System;
using System.Collections.Generic;
using System.Linq;
using SeasonalPerks.UI;
using UnityEngine;
using UnityEngine.UI;

public static class SeasonalPerksModifiersChecks
{
    public static string[] Run(SeasonalScreen view, ScreenState state)
    {
        var checks = new List<string>();
        void Check(bool value, string message)
        {
            if (!value)
                throw new Exception("Modifier UI: " + message);
            checks.Add(message);
        }
        var originalMode = state.ActiveMode;
        var originalScav = state.IsScav;
        var originalSelected = state.Selected;
        Canvas.ForceUpdateCanvases();
        var scroll = view.Root.GetComponentsInChildren<ScrollRect>();
        Check(scroll.Length == 1, "One shared scroll view for all modifier groups");
        Check(
            !view.Root.GetComponentsInChildren<Button>().Any()
                && !view.Root.GetComponentsInChildren<InputField>().Any(),
            "Display contains no editor buttons or search field"
        );
        Check(view.Root.GetComponent<Image>().color.a == 0, "Native background remains visible");
        var content = scroll[0].content;
        Check(
            content
                .Cast<Transform>()
                .Select(child => child.name)
                .SequenceEqual(
                    new[] { "CommonModifiers", "PositiveModifiers", "NegativeModifiers" }
                ),
            "Groups appear in common, positive, negative order"
        );
        var rows = content
            .GetComponentsInChildren<HorizontalLayoutGroup>()
            .Where(row => row.enabled)
            .ToArray();
        var cards = rows.SelectMany(row => row.transform.Cast<Transform>()).ToArray();
        var expected = state
            .Perks.Where(perk => perk.Common ? perk.Enabled : state.Selected.Contains(perk.Id))
            .ToArray();
        Check(
            cards.Length == expected.Length
                && cards.All(card => expected.Any(perk => perk.Id == card.name)),
            "Only enabled common and selected personal modifiers are shown"
        );
        Check(
            rows.All(row => row.transform.childCount <= 2),
            "Every row contains at most two cards"
        );
        Check(
            cards.All(card => Mathf.Abs(((RectTransform)card).rect.width - 780) < .1f),
            "Cards retain native 780 unit width"
        );
        Check(
            cards.All(card =>
                card.GetComponentsInChildren<Text>()
                    .All(text => text.preferredHeight <= text.rectTransform.rect.height + 1)
            ),
            "Card text fits without vertical clipping"
        );
        Check(
            cards
                .SelectMany(card => card.GetComponentsInChildren<Image>())
                .Where(image => image.name == "BackgroundGrid")
                .All(image => image.sprite && image.type == Image.Type.Tiled),
            "Recovered grid artwork tiles across each card"
        );
        scroll[0].verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();
        Check(
            scroll[0].verticalNormalizedPosition < .01f,
            "Shared scrollbar reaches negative modifiers at the bottom"
        );
        state.Selected = state.Perks.Where(perk => !perk.Common).Select(perk => perk.Id).ToArray();
        view.SetState(state, ScreenPage.Summary);
        Canvas.ForceUpdateCanvases();
        Check(
            view.Root.GetComponentsInChildren<Text>()
                .Where(text => text.name == "Description" || text.name == "Name")
                .All(text => text.preferredHeight <= text.rectTransform.rect.height + 1),
            "Entire personal catalogue fits, including long descriptions and unavailable notices"
        );
        state.ActiveMode = "normal";
        view.SetState(state, ScreenPage.Summary);
        Check(
            view.Root.GetComponentsInChildren<Text>().Any(text => text.name == "NoModifiers")
                && !view.Root.GetComponentsInChildren<HorizontalLayoutGroup>().Any(),
            "Normal character shows no seasonal modifier cards"
        );
        state.ActiveMode = "seasonal";
        state.IsScav = true;
        view.SetState(state, ScreenPage.Summary);
        Check(
            view.Root.GetComponentsInChildren<Text>()
                .Any(text => text.name == "NoModifiers" && text.text.Contains("Scav")),
            "Scav has its own empty state"
        );
        state.IsScav = false;
        state.Selected = Array.Empty<string>();
        view.SetState(state, ScreenPage.Summary);
        Check(
            !view
                .Root.GetComponentsInChildren<RectTransform>()
                .Any(rect => rect.name == "PositiveModifiers" || rect.name == "NegativeModifiers"),
            "Empty personal groups are omitted"
        );
        state.ActiveMode = originalMode;
        state.IsScav = originalScav;
        state.Selected = originalSelected;
        view.SetState(state, ScreenPage.Summary);
        return checks.ToArray();
    }
}
