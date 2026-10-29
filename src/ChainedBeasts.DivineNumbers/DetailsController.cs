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
        UIGameServerLobbyPage? Lobby = null, I2.Loc.Localize? Description = null,
        UILobbyShopSelectionPage? Shop = null, UIPlayerInfoPage? Hud = null,
        UILobbyDoctorSelectionPage? Potion = null, UILobbyTrainerProgress? Progress = null);
    private readonly Dictionary<int, Binding> _bindings = new();
    private DetailCatalog? _catalog;
    private ManualLogSource? _log;
    private NativeDetailsPanel? _panel;
    private bool _failed;
    private float _templateRetryAt;
    private float _descriptionScanAt;
    private bool _discoveryWasRequested;
    private ControllerToggleButton _controllerButton;
    private readonly NumbersToggle _toggle = new();
    private readonly List<ControllerButtonState> _controllerButtons = new();
    private int _padCount = -1;
    private int _visiblePotionCount = -1;
    private string? _controllerSelection;
    public DetailsController(IntPtr pointer) : base(pointer) { }
    [HideFromIl2Cpp]
    internal void Configure(DetailCatalog catalog, ManualLogSource log, ControllerToggleButton controllerButton) =>
        (_catalog, _log, _controllerButton) = (catalog, log, controllerButton);

    [HideFromIl2Cpp]
    internal void Bind(UIPlayerInfoTooltip view, int skill) => Guard(() => Register(
        view, view.transform.TryCast<RectTransform>(), skill, view));
    [HideFromIl2Cpp]
    internal void Bind(UIPlayerInfoTooltip view, string kind, int id, int severity) => Guard(() =>
    {
        NumericDetail? detail = null;
        _catalog?.TryGet(kind, id, severity, out detail!);
        var anchor = view.transform.TryCast<RectTransform>();
        _bindings.Remove(view.GetInstanceID());
        if (anchor != null) _bindings[view.GetInstanceID()] = new Binding(view, anchor, detail, Tooltip: view);
    });
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
        var ctrlHeld = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
        _controllerButtons.Clear();
        if (_controllerButton != ControllerToggleButton.Disabled)
        {
            var pads = Gamepad.all;
            for (var i = 0; i < pads.Count; i++)
            {
                var pad = pads[i];
                if (pad == null || !pad.added) continue;
                var button = _controllerButton == ControllerToggleButton.LeftStickClick ? pad.leftStickButton : pad.rightStickButton;
                _controllerButtons.Add(new ControllerButtonState(pad.deviceId, button.isPressed || button.wasPressedThisFrame));
            }
            if (_padCount != _controllerButtons.Count)
            {
                _padCount = _controllerButtons.Count;
                _log?.LogInfo($"Numbers input: {_padCount} connected gamepad(s); toggle={_controllerButton}.");
            }
        }
        var discoveryRequested = Application.isFocused && (ctrlHeld || _toggle.ControllerDeviceId != null ||
            _controllerButtons.Any(button => button.Down));

        // Serialized descriptions (Codex, surgery, shop, training) need not pass
        // through a skill widget. Discover keys on Ctrl/controller requests;
        // do not patch localization or scan resources every frame.
        if (discoveryRequested && (!_discoveryWasRequested || Time.unscaledTime >= _descriptionScanAt))
        {
            _descriptionScanAt = Time.unscaledTime + .5f;
            foreach (var page in Resources.FindObjectsOfTypeAll<UIPlayerInfoPage>())
                if (page != null && page.gameObject.scene.IsValid() && Visible(page))
                {
                    var slot = FirstHudSlot(page);
                    if (slot == null) continue;
                    var anchor = slot.transform.TryCast<RectTransform>();
                    if (anchor != null) _bindings[page.GetInstanceID()] = new Binding(page, anchor, null, Hud: page);
                }
            // Bind the serialized offer directly. Its potion preview is a sibling
            // of the sign, so a localized text-child hitbox is insufficient.
            foreach (var potion in Resources.FindObjectsOfTypeAll<UILobbyDoctorSelectionPage>())
            {
                if (potion == null || !potion.gameObject.scene.IsValid() || !Visible(potion) || potion.remedyDescription == null) continue;
                var anchor = PotionCardAnchor(potion);
                if (anchor == null) continue;
                NumericDetail? detail = null;
                _catalog?.TryGetTerm(potion.remedyDescription.Term, out detail!);
                // Register water/random/unsupported offers too: their focus must
                // clear numbers rather than fall back to the player's beast.
                _bindings[potion.GetInstanceID()] = new Binding(potion, anchor, detail,
                    Description: potion.remedyDescription, Potion: potion);
            }
            foreach (var shop in Resources.FindObjectsOfTypeAll<UILobbyShopSelectionPage>())
            {
                if (shop == null || !shop.gameObject.scene.IsValid() || !Visible(shop) || shop.itemDescription == null) continue;
                var anchor = ShopCardAnchor(shop);
                if (anchor == null) continue;
                NumericDetail? detail = null;
                _catalog?.TryGetTerm(shop.itemDescription.Term, out detail!);
                _bindings[shop.GetInstanceID()] = new Binding(shop, anchor, detail, Description: shop.itemDescription, Shop: shop);
            }
            foreach (var progress in Resources.FindObjectsOfTypeAll<UILobbyTrainerProgress>())
            {
                if (progress == null || !progress.gameObject.scene.IsValid() || !Visible(progress) || progress.injuryDescription == null) continue;
                var anchor = progress.injuryDescription.transform.TryCast<RectTransform>();
                if (anchor == null) continue;
                NumericDetail? detail = null;
                _catalog?.TryGetTerm(progress.injuryDescription.Term, out detail!);
                _bindings[progress.GetInstanceID()] = new Binding(progress, anchor, detail,
                    Description: progress.injuryDescription, Progress: progress);
            }
            foreach (var choice in Resources.FindObjectsOfTypeAll<UILobbyTrainerUpgradePage>())
                if (choice != null && choice.gameObject.scene.IsValid() && Visible(choice))
                    Register(choice, choice.transform.TryCast<RectTransform>(), (int)choice.SkillType, trainer: choice);
            foreach (var description in Resources.FindObjectsOfTypeAll<I2.Loc.Localize>())
            {
                if (description == null || !description.gameObject.scene.IsValid() ||
                    !Visible(description) || _catalog == null) continue;
                if (description.GetComponentInParent<UIStatusTooltip>()?.gameObject.name == NativeDetailsPanel.PanelName) continue;
                _catalog.TryGetTerm(description.Term, out var detail);
                // Unsupported focused descriptions must clear the old controller
                // card too. Limit these blockers to actual description labels.
                if (detail == null && !(IsDescriptionKey(description.Term) && ControllerContext.HasFocusableParent(description))) continue;
                var shop = description.GetComponentInParent<UILobbyShopSelectionPage>();
                // Status explanations nested under a shop page retain their own
                // hitbox. Only the offer's main description owns the whole card.
                if (shop != null && shop.itemDescription == description) continue; // Direct offer binding above.
                var potion = description.GetComponentInParent<UILobbyDoctorSelectionPage>();
                if (potion != null && potion.remedyDescription == description) continue;
                var progress = description.GetComponentInParent<UILobbyTrainerProgress>();
                if (progress != null && progress.injuryDescription == description) continue;
                var anchor = description.transform.TryCast<RectTransform>();
                if (anchor != null) _bindings[description.GetInstanceID()] = new Binding(
                    description, anchor, detail, Description: description);
            }
        }
        var potionCount = _bindings.Values.Count(binding => binding.Potion != null && Eligible(binding));
        if (discoveryRequested && potionCount != _visiblePotionCount)
        {
            _visiblePotionCount = potionCount;
            _log?.LogInfo($"Numbers healer cards: {potionCount} visible.");
        }
        _discoveryWasRequested = discoveryRequested;
        foreach (var button in _controllerButtons)
            if (_toggle.IsNewPress(button))
            {
                _log?.LogInfo($"Numbers controller click: device={button.DeviceId}; client={ControllerContext.DeviceClient(button.DeviceId)?.PlayerIndex.ToString() ?? "unmapped"}; focused={Application.isFocused}.");
            }
        var hasContext = _bindings.Values.Any(Eligible);
        var previousDevice = _toggle.ControllerDeviceId;
        _toggle.Update(Application.isFocused, hasContext, ctrlHeld, _controllerButtons);
        if (previousDevice != _toggle.ControllerDeviceId)
            _log?.LogInfo($"Numbers controller toggle: {(_toggle.ControllerDeviceId == null ? "off" : "on")}; context={hasContext}.");
        if (!_toggle.Requested) { _panel?.Hide(); return; }
        var controllerOnly = !ctrlHeld && _toggle.ControllerDeviceId != null;
        var selection = new InspectionSelection<Binding>();
        var mouse = Mouse.current;
        var position = mouse == null ? new Vector2(-10000, -10000) : mouse.position.ReadValue();
        foreach (var stored in _bindings.Values)
        {
            var binding = stored;
            if (!Eligible(binding)) continue;
            // Pooled labels can change their term between scans. Resolve again
            // before display so a new/unsupported description never inherits data.
            if (binding.Description != null)
            {
                NumericDetail? current = null;
                _catalog?.TryGetTerm(Visible(binding.Description) ? binding.Description.Term : null, out current!);
                binding = binding with { Detail = current };
            }
            if (binding.Shop != null && controllerOnly)
            {
                // Merchant item text may be generated independently of I2 terms.
                // Resolve the current typed purchase, once its title is visible.
                NumericDetail? current = null;
                if (binding.Shop.itemName != null && Visible(binding.Shop.itemName))
                    _catalog?.TryGet("purchase", (int)binding.Shop.PurchaseType, 0, out current!);
                binding = binding with { Detail = current };
            }
            if (binding.Trainer != null)
            {
                NumericDetail? current = null;
                _catalog?.TryGet((int)binding.Trainer.SkillType, out current!);
                binding = binding with { Detail = current };
            }
            if (binding.Hud != null)
            {
                var slot = FirstHudSlot(binding.Hud);
                if (slot == null || !Visible(slot)) continue;
                NumericDetail? current = null;
                _catalog?.TryGet((int)binding.Hud.visualSkillList[0], out current!);
                binding = binding with { Detail = current, Anchor = slot.transform.Cast<RectTransform>() };
            }
            if (binding.Lobby != null && (binding.Lobby.characterDescription == null ||
                !Visible(binding.Lobby.characterDescription))) continue;
            var hovered = !controllerOnly && (binding.Shop != null || binding.Potion != null
                ? PanelPlacement.Contains(OfferBounds(binding), position.x, position.y)
                : RectTransformUtility.RectangleContainsScreenPoint(binding.Anchor, position, NativeDetailsPanel.CameraFor(binding.Anchor)));
            var score = controllerOnly ? ControllerScore(binding, _toggle.ControllerDeviceId!.Value)
                : binding.Description != null ? (hovered ? 8 : 0)
                : binding.Tooltip != null ? (hovered ? 6 : 3)
                : binding.Trainer != null ? (binding.Trainer.isHovered ? 6 : binding.Trainer.isSelected ? 2 : 0)
                : binding.Lobby != null ? (hovered ? 6 : IsLocalLobbyPage(binding.Lobby) ? 4 : 0)
                : hovered ? 5 : 0;
            selection.Consider(binding, score, hud: binding.Hud != null,
                offer: binding.Potion != null || binding.Shop != null || binding.Trainer != null || binding.Progress != null);
        }
        var selected = selection.Selected(controllerOnly);
        if (controllerOnly)
        {
            var key = selected == null ? $"none; offersVisible={selection.OffersVisible}" :
                selected.Potion != null ? $"healer offer {selected.Potion.Index}; term={selected.Potion.remedyDescription?.Term}; card={selected.Detail != null}" :
                selected.Shop != null ? $"merchant offer {selected.Shop.Index}; purchase={(int)selected.Shop.PurchaseType}; term={selected.Shop.itemDescription?.Term}; card={selected.Detail != null}" :
                selected.Progress != null ? $"training affliction; player={selected.Progress.playerIndex}; term={selected.Progress.injuryDescription?.Term}; card={selected.Detail != null}" :
                selected.Trainer != null ? $"skill offer {selected.Trainer.Index}; skill={(int)selected.Trainer.SkillType}; card={selected.Detail != null}" :
                $"{selected.Owner.GetType().Name}; card={selected.Detail != null}";
            if (key != _controllerSelection) { _controllerSelection = key; _log?.LogInfo("Numbers controller selection: " + key); }
        }
        else _controllerSelection = null;
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
        var bounds = selected.Shop != null || selected.Potion != null ? OfferBounds(selected) : NativeDetailsPanel.ScreenBounds(selected.Anchor);
        // Keep both vanilla Shift boxes and the main description out of the new box's way.
        var status = selected.Tooltip != null ? selected.Tooltip.statusTooltips
            : selected.Trainer != null ? selected.Trainer.statusTooltips
            : selected.Potion != null ? selected.Potion.statusTooltips : selected.Shop?.statusTooltips;
        if (status != null)
            foreach (var tip in status)
                if (tip != null && tip.root != null && tip.root.activeInHierarchy)
                    bounds = NativeDetailsPanel.Union(bounds, NativeDetailsPanel.ScreenBounds(tip.transform.Cast<RectTransform>()));
        var color = selected.Tooltip != null ? selected.Tooltip.playerColor
            : selected.Trainer != null ? selected.Trainer.defaultColor : Color.white;
        var hint = _toggle.ControllerDeviceId == null ? "Ctrl" :
            _controllerButton == ControllerToggleButton.LeftStickClick ? "L3: On" : "R3: On";
        _panel.Show(selected.Detail, bounds, color, hint);
    }
    private static bool Eligible(Binding binding) => Visible(binding.Owner) && Visible(binding.Anchor) &&
        // Concealed/unrevealed offers remain focus blockers even when their
        // description child is hidden. Their numeric detail resolves to null.
        (binding.Potion != null || binding.Shop != null || binding.Description == null || Visible(binding.Description)) &&
        (binding.Hud == null || (FirstHudSlot(binding.Hud) is { } slot && Visible(slot))) &&
        (binding.Lobby == null || (binding.Lobby.characterDescription != null && Visible(binding.Lobby.characterDescription)));

    private static UIGameLevelLobbySlot? FirstHudSlot(UIPlayerInfoPage page)
    {
        // Native controller info is held with RB; forcing it open lasts only one
        // frame. Inspect the first actual visible HUD skill directly instead.
        return page.visualSkillList != null && page.visualSkillList.Count > 0 &&
            page.skillSlots != null && page.skillSlots.Length > 0 ? page.skillSlots[0] : null;
    }

    private static Bounds OfferBounds(Binding binding)
    {
        var bounds = NativeDetailsPanel.ScreenBounds(binding.Anchor);
        void Include(Component? part)
        {
            if (part == null || !Visible(part)) return;
            var rect = part.transform.TryCast<RectTransform>();
            if (rect != null && rect.rect.width > 0 && rect.rect.height > 0)
                bounds = PanelPlacement.Union(bounds, NativeDetailsPanel.ScreenBounds(rect));
        }
        if (binding.Potion != null)
        {
            var potion = binding.Potion;
            Include(potion.remedyDescription);
            Include(potion.remedyName);
            Include(potion.waterName);
            Include(potion.healingValue);
            Include(potion.ui3DView);
            Include(potion.potionIcon);
            if (potion.backgroundTint != null)
                for (var i = 0; i < potion.backgroundTint.Length; i++) Include(potion.backgroundTint[i]);
        }
        else if (binding.Shop != null)
        {
            var shop = binding.Shop;
            Include(shop.itemDescription);
            Include(shop.itemName);
            Include(shop.costValue);
            Include(shop.ui3DView);
            if (shop.backgroundTint != null)
                for (var i = 0; i < shop.backgroundTint.Length; i++) Include(shop.backgroundTint[i]);
        }
        return bounds;
    }
    private static RectTransform? PotionCardAnchor(UILobbyDoctorSelectionPage potion)
    {
        var anchor = potion.navigationButton?.InteractionAnchor;
        if (anchor != null && anchor.rect.width > 0 && anchor.rect.height > 0) return anchor;
        anchor = potion.offsetContentRoot?.transform.TryCast<RectTransform>() ?? potion.transform.TryCast<RectTransform>();
        return anchor != null && anchor.rect.width > 0 && anchor.rect.height > 0 ? anchor : null;
    }
    private static RectTransform? ShopCardAnchor(UILobbyShopSelectionPage? shop)
    {
        if (shop == null) return null;
        // This is the game's full native interaction rectangle, including the
        // header, frame, empty space and price, rather than its text child.
        var anchor = shop.navigationButton?.InteractionAnchor;
        if (anchor != null && anchor.rect.width > 0 && anchor.rect.height > 0) return anchor;
        anchor = shop.root?.TryCast<RectTransform>() ?? shop.transform.TryCast<RectTransform>();
        return anchor != null && anchor.rect.width > 0 && anchor.rect.height > 0 ? anchor : null;
    }
    private static bool IsDescriptionKey(string? term) => term != null &&
        (((term.StartsWith("Skills/", StringComparison.Ordinal) || term.StartsWith("Injuries/", StringComparison.Ordinal) ||
            term.StartsWith("Purchases/", StringComparison.Ordinal)) && term.Contains("_Desc", StringComparison.Ordinal)) ||
            (term.StartsWith("UI/", StringComparison.Ordinal) && term.EndsWith("TooltipDesc", StringComparison.Ordinal)));
    private static int ControllerScore(Binding binding, int deviceId)
    {
        if (binding.Potion != null) return ControllerContext.PotionFocused(binding.Potion, deviceId) ? 10 : 0;
        if (binding.Shop != null) return ControllerContext.MerchantFocused(binding.Shop, deviceId) ? 10 : 0;
        if (binding.Progress != null) return ControllerContext.MatchesPlayer(binding.Progress.playerIndex, deviceId) ? 10 : 0;
        if (binding.Hud != null) return ControllerContext.MatchesClient(binding.Hud.clientID, deviceId) ? 1 : 0;
        if (binding.Trainer != null) return ControllerContext.SkillChoiceFocused(binding.Trainer, deviceId) ? 10 : 0;
        if (binding.Description != null) return ControllerContext.Focused(binding.Description, deviceId) ? 8 : 0;
        if (binding.Tooltip != null) return ControllerContext.OwnsTooltip(binding.Tooltip, deviceId) ? 7 : 0;
        if (binding.Lobby != null) return binding.Lobby.connected && ControllerContext.MatchesPlayer(binding.Lobby.playerIndex, deviceId) ? 4 : 0;
        return ControllerContext.Focused(binding.Owner, deviceId) ? 5 : 0;
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
        if (binding.Shop != null && binding.Shop.statusTooltipPrefab != null)
            return binding.Shop.statusTooltipPrefab;
        if (binding.Potion != null && binding.Potion.statusTooltipPrefab != null)
            return binding.Potion.statusTooltipPrefab;
        foreach (var tip in Resources.FindObjectsOfTypeAll<UIStatusTooltip>())
            if (tip != null && tip.gameObject.name != NativeDetailsPanel.PanelName) return tip;
        return null;
    }
    [HideFromIl2Cpp]
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
        _controllerSelection = null;
        _toggle.Reset();
    }
    public void OnDestroy() => Cleanup();
}
