using UnityEngine;
using UnityEngine.InputSystem;

namespace ChainedBeasts.DivineNumbers;

internal static class ControllerContext
{
    internal static ClientIdentifier? DeviceClient(int deviceId)
    {
        var device = InputSystem.GetDeviceById(deviceId);
        // GetClientDevice is not the inverse of GetDeviceClient: the latter also
        // implements the game's unassigned/global single-player input fallback.
        return device == null ? null : GameInput.GetDeviceClient(device);
    }

    internal static bool MatchesClient(ClientIdentifier? client, int deviceId) =>
        client != null && DeviceClient(deviceId) == client;

    internal static bool MatchesPlayer(int playerIndex, int deviceId)
    {
        var client = DeviceClient(deviceId);
        return playerIndex >= 0 && client != null && client.PlayerIndex == (ulong)playerIndex;
    }

    internal static bool Focused(Component owner, int deviceId)
    {
        var doctor = owner.GetComponentInParent<UILobbyDoctorSelectionPage>();
        if (doctor != null) return PotionFocused(doctor, deviceId);
        var shop = owner.GetComponentInParent<UILobbyShopSelectionPage>();
        if (shop != null)
        {
            if (shop.focusedPlayerIndices != null)
                foreach (var player in shop.focusedPlayerIndices)
                    if (MatchesPlayer(player, deviceId)) return true;
            return false;
        }
        var trainer = owner.GetComponentInParent<UILobbyTrainerUpgradePage>();
        if (trainer != null)
        {
            var nav = trainer.navPage;
            return nav != null && (nav.ownerPlayerIndex < 0 || MatchesPlayer(nav.ownerPlayerIndex, deviceId)) &&
                (nav.lastFocusedState || trainer.isSelected);
        }
        var codex = owner.GetComponentInParent<UICodexPage>();
        if (codex != null)
            return codex.Discovered && codex.gamepadHighlightContainer != null &&
                codex.gamepadHighlightContainer.activeInHierarchy;
        var button = owner.GetComponentInParent<UIClientNavigationButton>();
        return button != null && button.lastFocusedState &&
            (button.ownerPlayerIndex < 0 || MatchesPlayer(button.ownerPlayerIndex, deviceId));
    }

    internal static bool PotionFocused(UILobbyDoctorSelectionPage page, int deviceId)
    {
        var client = DeviceClient(deviceId);
        var flags = page.focusedPlayerIndices;
        // Doctor cards store one boolean per player, unlike the general shop's
        // list of focused player indices. Do not interpret bools as player IDs.
        return client != null && flags != null && client.PlayerIndex < (ulong)flags.Length &&
            flags[(int)client.PlayerIndex];
    }

    internal static bool OwnsTooltip(UIPlayerInfoTooltip tooltip, int deviceId)
    {
        var info = tooltip.GetComponentInParent<UIPlayerInfoPage>();
        if (info != null) return MatchesClient(info.clientID, deviceId);
        var surgeon = tooltip.GetComponentInParent<UILobbySurgeonPlayerPage>();
        if (surgeon != null) return surgeon.isSelected && MatchesPlayer(surgeon.playerIndex, deviceId);
        // Shared menus can display a single native tooltip without a player page.
        return true;
    }

    internal static bool HasFocusableParent(Component owner) =>
        owner.GetComponentInParent<UILobbyDoctorSelectionPage>() != null ||
        owner.GetComponentInParent<UILobbyShopSelectionPage>() != null ||
        owner.GetComponentInParent<UILobbyTrainerUpgradePage>() != null ||
        owner.GetComponentInParent<UICodexPage>() != null ||
        owner.GetComponentInParent<UIClientNavigationButton>() != null;
}
