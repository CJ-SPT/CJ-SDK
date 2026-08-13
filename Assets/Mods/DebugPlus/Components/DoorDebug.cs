using System.Collections.Generic;
using System.Linq;
using CJ.ModSdk.Overlays;
using EFT.Interactive;
using UnityEngine;
using OCB = CJ.ModSdk.Overlays.OverlayContentBuilder;

namespace DebugPlus.Components;

public class DoorDebug : MonoBehaviour
{
    private List<WorldInteractiveObject> _doors = new();

    private void Awake()
    {
        // This method is just lol, but works for this use case.
        _doors = LocationScene
            .GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>()
            .Where(o => o is Door)
            .ToList();

        foreach (var door in _doors)
        {
            var provider = door.GetOrAddComponent<OverlayProvider>();
            provider.SetOverlayContent(GetDoorInfoText(door), Enable, MaxDist, UpOffset, FontSize);
        }
    }

    private static bool Enable()
    {
        return DebugPlusConfig.ShowDoorOverlays.Value;
    }

    private static float MaxDist()
    {
        return DebugPlusConfig.OverlayMaxDist.Value;
    }

    private static float UpOffset()
    {
        return DebugPlusConfig.OverlayUpDist.Value;
    }

    private static int FontSize()
    {
        return DebugPlusConfig.OverlayFontSize.Value;
    }

    private static string GetDoorInfoText(WorldInteractiveObject door)
    {
        OCB.Clear();

        OCB.AppendLabeledValue("DoorId", door.Id, Color.gray, Color.green);
        OCB.AppendLabeledValue("KeyId", door.KeyId ?? "No key", Color.gray, Color.green);
        OCB.AppendLabeledValue("Operable", door.Operatable.ToString(), Color.gray, Color.green);
        OCB.AppendLabeledValue("State", GetDoorState(door), Color.gray, Color.green);

        return OCB.ToString();
    }

    private static string GetDoorState(WorldInteractiveObject door)
    {
        return door.DoorState.ToString();
    }
}
