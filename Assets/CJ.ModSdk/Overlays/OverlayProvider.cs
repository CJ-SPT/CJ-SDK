using System;
using EFT.CameraControl;
using UnityEngine;

namespace CJ.ModSdk.Overlays;

/// <summary>
/// Credits: DrakiaXYZ for the overlay code
/// </summary>
public class OverlayProvider : MonoBehaviour
{
    public float MaxDist { get; private set; }
    public float UpOffset { get; private set; }
    public int FontSize { get; private set; }

    private GUIStyle _guiStyle;
    private float _screenScale = 1.0f;

    private GUIContent _content = new();
    private Rect _rect = new();

    private Func<bool> _enabled;

    private void Awake()
    {
        // If DLSS or FSR are enabled, set a screen scale value
        if (!CameraManager.Instance.SSAA.isActiveAndEnabled)
        {
            return;
        }

        _screenScale =
            CameraManager.Instance.SSAA.GetOutputWidth()
            / (float)CameraManager.Instance.SSAA.GetInputWidth();
    }

    private void OnGUI()
    {
        if (_guiStyle is null)
        {
            CreateGuiStyle();
        }

        if (!_enabled())
        {
            return;
        }

        var pos = transform.position;
        var dist = Mathf.RoundToInt(
            (transform.position - Camera.main!.transform.position).magnitude
        );

        if (_content.text.Length <= 0 || !(dist < MaxDist))
            return;

        var screenPos = Camera.main!.WorldToScreenPoint(pos + Vector3.up * UpOffset);

        // Don't render behind the camera.
        if (screenPos.z <= 0)
            return;

        SetRectSize(screenPos);

        GUI.Box(_rect, _content, _guiStyle);
    }

    public void SetOverlayContent(
        string content,
        Func<bool> shouldShow,
        Func<float> maxDist,
        Func<float> upOffset,
        Func<int> fontSize
    )
    {
        _enabled = shouldShow;
        _content.text = content;
    }

    /// <summary>
    /// Sets the rect size for the overlay to render. Should be called in the implementing classes OnGUI()
    /// </summary>
    /// <param name="screenPos">Position on the screen</param>
    private void SetRectSize(Vector3 screenPos)
    {
        var guiSize = _guiStyle.CalcSize(_content);
        _rect.x = (screenPos.x * _screenScale) - (guiSize.x / 2);
        _rect.y = Screen.height - ((screenPos.y * _screenScale) + guiSize.y);
        _rect.size = guiSize;
    }

    private void CreateGuiStyle()
    {
        _guiStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = FontSize,
            margin = new RectOffset(3, 3, 3, 3),
            richText = true,
        };
    }
}
