using UnityEngine;

namespace ChainedBeasts.DivineNumbers;

internal static class ControllerContext
{
    private static bool MatchesClient(ClientIdentifier? client, int deviceId)
    {
        if (client == null) return false;
        var device = GameInput.GetClientDevice(client);
        return device != null && device.deviceId == deviceId;
    }

    internal static bool MatchesPlayer(int playerIndex, int deviceId)
    {
        var session = NetworkSession.Instance;
        return playerIndex >= 0 && session != null &&
            MatchesClient(session.GetClientByPlayerIndex((ulong)playerIndex), deviceId);
    }

    internal static bool Focused(Component owner, int deviceId)
    {
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
        owner.GetComponentInParent<UILobbyShopSelectionPage>() != null ||
        owner.GetComponentInParent<UILobbyTrainerUpgradePage>() != null ||
        owner.GetComponentInParent<UICodexPage>() != null ||
        owner.GetComponentInParent<UIClientNavigationButton>() != null;
}
