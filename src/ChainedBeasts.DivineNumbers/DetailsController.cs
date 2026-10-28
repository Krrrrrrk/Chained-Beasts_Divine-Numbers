using BepInEx.Logging;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace ChainedBeasts.DivineNumbers;

public sealed class DetailsController : MonoBehaviour
{
    private sealed record Binding(Component Owner, RectTransform Anchor, NumericDetail? Detail,
        UIPlayerInfoTooltip? Tooltip = null, UILobbyTrainerUpgradePage? Trainer = null,
        UIGameServerLobbyPage? Lobby = null);
    private readonly Dictionary<int, Binding> _bindings = new();
    private DetailCatalog? _catalog;
    private ManualLogSource? _log;
    private NativeDetailsPanel? _panel;
    private bool _failed;
    private float _templateRetryAt;
    public DetailsController(IntPtr pointer) : base(pointer) { }
    [HideFromIl2Cpp]
    internal void Configure(DetailCatalog catalog, ManualLogSource log) => (_catalog, _log) = (catalog, log);

    [HideFromIl2Cpp]
    internal void Bind(UIPlayerInfoTooltip view, int skill) => Guard(() => Register(
        view, view.transform.TryCast<RectTransform>(), skill, view));
    [HideFromIl2Cpp]
    internal void Bind(UILobbyTrainerUpgradePage view, int skill) => Guard(() => Register(
        view, view.transform.TryCast<RectTransform>(), skill, trainer: view));
    [HideFromIl2Cpp]
    internal void Bind(UIGameLevelLobbyCharacterSkillInfo view, int skill) => Guard(() => Register(
        view, view.transform.TryCast<RectTransform>(), skill));
    [HideFromIl2Cpp]
    internal void Unbind(UIPlayerInfoTooltip owner) => Bind(owner, -1);
    [HideFromIl2Cpp]
    internal void Bind(UIGameServerLobbyPage page) => Guard(() =>
    {
        var id = page.GetInstanceID();
        _bindings.Remove(id);
        var description = page.characterDescription;
        if (!page.connected || page.uiState == null || page.uiState.isRestoringCheckpoint || description == null) return;
        var anchor = description.transform.TryCast<RectTransform>();
        if (anchor == null) return;
        NumericDetail? detail = null;
        // Native Refresh assigns the exact skill term for the displayed variant.
        // Locked variants instead get an unlock-requirement term, which has no entry.
        _catalog?.TryGetTerm(description.Term, out detail!);
        _bindings[id] = new Binding(page, anchor, detail, Lobby: page);
    });
    private void Register(Component owner, RectTransform? anchor, int skill,
        UIPlayerInfoTooltip? tooltip = null, UILobbyTrainerUpgradePage? trainer = null)
    {
        var id = owner.GetInstanceID();
        // Pooled widgets may switch to an unsupported skill or another item type.
        _bindings.Remove(id);
        NumericDetail? detail = null;
        _catalog?.TryGet(skill, out detail!);
        // Unsupported hovered/selected widgets must still block a previously selected
        // supported widget in the same UI. They receive no numeric card of their own.
        if (anchor != null) _bindings[id] = new Binding(owner, anchor, detail, tooltip, trainer);
    }

    public void LateUpdate()
    {
        if (_failed) return;
        Guard(Tick);
    }
    private void Tick()
    {
        foreach (var item in _bindings.ToArray())
            if (item.Value.Owner == null || item.Value.Anchor == null) _bindings.Remove(item.Key);

        var keyboard = Keyboard.current;
        if (!Application.isFocused || keyboard == null ||
            !(keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed))
        {
            _panel?.Hide();
            return;
        }

        Binding? selected = null;
        var best = 0;
        var mouse = Mouse.current;
        var position = mouse == null ? new Vector2(-10000, -10000) : mouse.position.ReadValue();
        foreach (var binding in _bindings.Values)
        {
            if (!Visible(binding.Owner)) continue;
            if (binding.Lobby != null && (binding.Lobby.characterDescription == null ||
                !Visible(binding.Lobby.characterDescription))) continue;
            var hovered = RectTransformUtility.RectangleContainsScreenPoint(binding.Anchor, position,
                NativeDetailsPanel.CameraFor(binding.Anchor));
            var score = binding.Tooltip != null ? (hovered ? 6 : 3)
                : binding.Trainer != null ? (binding.Trainer.isHovered ? 6 : binding.Trainer.isSelected ? 2 : 0)
                : binding.Lobby != null ? (hovered ? 6 : IsLocalLobbyPage(binding.Lobby) ? 4 : 0)
                : hovered ? 5 : 0;
            if (score > best) { best = score; selected = binding; }
        }
        if (selected?.Detail == null) { _panel?.Hide(); return; }

        if (_panel == null || !_panel.IsValid)
        {
            _panel?.Dispose();
            _panel = null;
            if (Time.unscaledTime < _templateRetryAt) return;
            _templateRetryAt = Time.unscaledTime + 1;
            var template = FindTemplate(selected);
            if (template == null) return; // Native UI assets may not have loaded yet.
            _panel = new NativeDetailsPanel(template, transform);
        }
        var bounds = NativeDetailsPanel.ScreenBounds(selected.Anchor);
        // Keep both vanilla Shift boxes and the main description out of the new box's way.
        var status = selected.Tooltip != null ? selected.Tooltip.statusTooltips : selected.Trainer?.statusTooltips;
        if (status != null)
            foreach (var tip in status)
                if (tip != null && tip.root != null && tip.root.activeInHierarchy)
                    bounds = NativeDetailsPanel.Union(bounds, NativeDetailsPanel.ScreenBounds(tip.transform.Cast<RectTransform>()));
        var color = selected.Tooltip != null ? selected.Tooltip.playerColor
            : selected.Trainer != null ? selected.Trainer.defaultColor : Color.white;
        _panel.Show(selected.Detail, bounds, color);
    }
    private static bool Visible(Component owner)
    {
        if (owner == null || !owner.gameObject.activeInHierarchy) return false;
        // Closed menus can retain active children under a transparent CanvasGroup.
        foreach (var group in owner.GetComponentsInParent<CanvasGroup>(true))
            if (group != null && group.alpha <= .001f) return false;
        return true;
    }
    private static bool IsLocalLobbyPage(UIGameServerLobbyPage page)
    {
        var session = NetworkSession.Instance;
        return page.connected && page.playerIndex >= 0 && session != null &&
            session.IsLocalClient(session.GetClientByPlayerIndex((ulong)page.playerIndex));
    }
    private static UIStatusTooltip? FindTemplate(Binding binding)
    {
        if (binding.Tooltip != null && binding.Tooltip.statusTooltips != null)
            foreach (var tip in binding.Tooltip.statusTooltips)
                if (tip != null) return tip;
        if (binding.Trainer != null && binding.Trainer.statusTooltipPrefab != null)
            return binding.Trainer.statusTooltipPrefab;
        foreach (var tip in Resources.FindObjectsOfTypeAll<UIStatusTooltip>())
            if (tip != null && tip.gameObject.name != NativeDetailsPanel.PanelName) return tip;
        return null;
    }
    private void Guard(Action action)
    {
        if (_failed) return;
        try { action(); }
        catch (Exception ex)
        {
            _failed = true;
            _log?.LogError("Numbers box disabled after UI error; original UI continues: " + ex);
            Cleanup();
        }
    }
    [HideFromIl2Cpp]
    internal void Cleanup()
    {
        _panel?.Dispose();
        _panel = null;
        _bindings.Clear();
    }
    public void OnDestroy() => Cleanup();
}
