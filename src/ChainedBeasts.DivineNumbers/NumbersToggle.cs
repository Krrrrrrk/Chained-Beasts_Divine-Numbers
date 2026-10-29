namespace ChainedBeasts.DivineNumbers;

internal enum ControllerToggleButton { RightStickClick, LeftStickClick, Disabled }
internal readonly record struct ControllerButtonState(int DeviceId, bool Down);

// Pure input state: Unity/device sampling and UI focus selection live elsewhere.
internal sealed class NumbersToggle
{
    private readonly HashSet<int> _down = new();
    internal int? ControllerDeviceId { get; private set; }
    internal bool Requested { get; private set; }

    internal void Update(bool focused, bool hasContext, bool ctrlHeld,
        IReadOnlyList<ControllerButtonState> buttons)
    {
        bool Present(int id) => buttons.Any(button => button.DeviceId == id);
        if (!focused || !hasContext || (ControllerDeviceId is int owner && !Present(owner)))
            ControllerDeviceId = null;

        foreach (var button in buttons)
        {
            var wasDown = _down.Contains(button.DeviceId);
            if (button.Down) _down.Add(button.DeviceId); else _down.Remove(button.DeviceId);
            // Track edges even outside menus/unfocused so holding the button
            // while entering a menu or returning focus cannot arm the toggle.
            if (focused && hasContext && button.Down && !wasDown)
                ControllerDeviceId = ControllerDeviceId == button.DeviceId ? null : button.DeviceId;
        }
        _down.RemoveWhere(id => !Present(id));
        Requested = focused && (ctrlHeld || ControllerDeviceId != null);
    }

    internal void Reset()
    {
        ControllerDeviceId = null;
        Requested = false;
        _down.Clear();
    }
}
