using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ChainedBeasts.DivineNumbers;

internal sealed class NativeDetailsPanel : IDisposable
{
    internal const string PanelName = "Divine Numbers details";
    private GameObject? _canvasRoot;
    private UIStatusTooltip _view = null!;
    private TMP_Text _text = null!;
    private RectTransform _rect = null!;
    private Canvas _canvas = null!;
    private string? _lastText;
    private float _lastWidth;
    private float _lastAvailableHeight;
    private float _height;
    private static readonly Il2CppStructArray<Vector3> Corners = new(4);
    internal bool IsValid => _canvasRoot != null && _view != null && _text != null && _rect != null;

    internal NativeDetailsPanel(UIStatusTooltip template, Transform parent)
    {
        try
        {
            _canvasRoot = new GameObject("Divine Numbers overlay", Il2CppType.Of<RectTransform>());
            _canvasRoot.hideFlags = HideFlags.DontSave;
            _canvasRoot.transform.SetParent(parent, false);
            _canvas = _canvasRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30000;
            _canvas.targetDisplay = 0;
            // The copied box has no input module or raycaster and cannot steal hover/clicks.
            _view = Object.Instantiate(template, _canvasRoot.transform, false);
            _view.gameObject.name = PanelName;
            _view.Hide();
            _view.gameObject.SetActive(true);
            _rect = _view.transform.Cast<RectTransform>();
            _rect.anchorMin = _rect.anchorMax = Vector2.zero;
            _rect.pivot = new Vector2(0, 1);
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;
            _text = _view.statusDesc.GetComponent<TMP_Text>();
            if (_text == null) throw new InvalidOperationException("Native status panel has no TMP description");
            // Only the private clone loses localization. Vanilla Localize components stay enabled.
            foreach (var localize in _view.GetComponentsInChildren<I2.Loc.Localize>(true)) localize.enabled = false;
            foreach (var graphic in _view.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            foreach (var group in _view.GetComponentsInChildren<CanvasGroup>(true))
            { group.blocksRaycasts = false; group.interactable = false; }
            _text.richText = true;
            _text.enableAutoSizing = false;
            _text.fontSize = Math.Min(_text.fontSize, 26);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Overflow;
            _text.alignment = TextAlignmentOptions.TopLeft;
        }
        catch { Dispose(); throw; }
    }
    internal void Show(NumericDetail detail, Bounds anchor, Color color)
    {
        var scale = Mathf.Clamp(Screen.height / 1080f, .5f, 3f);
        _canvas.scaleFactor = scale;
        var width = Math.Min(430f, Screen.width / scale - 48);
        var content = "<b>Divine Numbers</b>  <size=75%>[Ctrl]</size>\n" +
            "<color=#FFE29A>" + detail.Name + "</color>\n\n" + detail.Body;
        var maxHeight = Screen.height / scale - 48;
        if (content != _lastText || width != _lastWidth || maxHeight != _lastAvailableHeight)
        {
            _text.text = content;
            // Native prefab text has 40px horizontal and 10px vertical inset.
            // Reduce type size only on small windows so the entire card stays on-screen.
            _text.fontSize = 26;
            var height = _text.GetPreferredValues(content, width - 40, 10000).y + 24;
            while (height > maxHeight && _text.fontSize > 14)
            {
                _text.fontSize -= 1;
                height = _text.GetPreferredValues(content, width - 40, 10000).y + 24;
            }
            _height = height;
            _rect.sizeDelta = new Vector2(width, _height);
            _lastWidth = width;
            _lastAvailableHeight = maxHeight;
            _lastText = content;
        }
        var position = PanelPlacement.Place(anchor, width * scale, _height * scale,
            Screen.width, Screen.height, 16 * scale, 18 * scale);
        _rect.anchoredPosition = new Vector2(position.Left / scale, position.Top / scale);
        _view.Show(color);
    }
    internal void Hide() { if (_view != null && _view.root != null) _view.Hide(); }
    internal static Camera? CameraFor(RectTransform rect)
    {
        var canvas = rect.GetComponentInParent<Canvas>();
        return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
    internal static Bounds ScreenBounds(RectTransform rect)
    {
        rect.GetWorldCorners(Corners);
        var camera = CameraFor(rect);
        var left = float.PositiveInfinity; var bottom = left;
        var right = float.NegativeInfinity; var top = right;
        for (var i = 0; i < 4; i++)
        {
            var p = RectTransformUtility.WorldToScreenPoint(camera, Corners[i]);
            left = Math.Min(left, p.x); right = Math.Max(right, p.x);
            bottom = Math.Min(bottom, p.y); top = Math.Max(top, p.y);
        }
        return new Bounds(left, bottom, right, top);
    }
    internal static Bounds Union(Bounds a, Bounds b) => new(Math.Min(a.Left, b.Left), Math.Min(a.Bottom, b.Bottom),
        Math.Max(a.Right, b.Right), Math.Max(a.Top, b.Top));
    public void Dispose()
    {
        if (_canvasRoot != null) { _canvasRoot.SetActive(false); Object.Destroy(_canvasRoot); }
        _canvasRoot = null;
    }
}
