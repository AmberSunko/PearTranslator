using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace PearTranslator.App.Wpf.Hotkeys;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly IHotkeyRegistrar _registrar;
    private readonly Dictionary<int, HotkeyCommand> _commandsById = [];
    private readonly Window _window;
    private LowLevelKeyboardHotkeyFallback? _selectRegionFallback;
    private HwndSource? _source;

    public GlobalHotkeyService(Window window)
        : this(window, new User32HotkeyRegistrar())
    {
    }

    public GlobalHotkeyService(Window window, IHotkeyRegistrar registrar)
    {
        _window = window;
        _registrar = registrar;
        ShortcutSummary = HotkeyRegistrationPlanner.DefaultShortcutSummary;
        _window.SourceInitialized += OnSourceInitialized;
    }

    public event EventHandler? SelectRegionPressed;
    public event EventHandler? PausePressed;
    public event EventHandler? DismissPressed;
    public event EventHandler? OneShotPressed;
    public event EventHandler? ShortcutSummaryChanged;

    public string ShortcutSummary { get; private set; }

    public void Dispose()
    {
        if (_source is null)
        {
            return;
        }

        var handle = new WindowInteropHelper(_window).Handle;
        foreach (var id in _commandsById.Keys)
        {
            _registrar.UnregisterHotKey(handle, id);
        }

        _commandsById.Clear();
        _source.RemoveHook(OnWindowMessage);
        _selectRegionFallback?.Dispose();
        _selectRegionFallback = null;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(OnWindowMessage);

        var state = HotkeyRegistrationPlanner.RegisterDefaults(_registrar, handle);
        _commandsById.Clear();
        foreach (var registration in state.Registrations)
        {
            _commandsById[registration.Id] = registration.Command;
        }

        if (state.Registrations.All(registration => registration.Command != HotkeyCommand.SelectRegion))
        {
            _selectRegionFallback = new LowLevelKeyboardHotkeyFallback(
                HotkeyRegistrationPlanner.SelectRegionGesture,
                () => _window.Dispatcher.BeginInvoke(() => SelectRegionPressed?.Invoke(this, EventArgs.Empty)));
        }

        ShortcutSummary = state.ShortcutSummary;
        ShortcutSummaryChanged?.Invoke(this, EventArgs.Empty);
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        handled = true;
        if (!_commandsById.TryGetValue(wParam.ToInt32(), out var command))
        {
            return IntPtr.Zero;
        }

        if (command == HotkeyCommand.SelectRegion)
        {
            SelectRegionPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (command == HotkeyCommand.Pause)
        {
            PausePressed?.Invoke(this, EventArgs.Empty);
        }
        else if (command == HotkeyCommand.Dismiss)
        {
            DismissPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (command == HotkeyCommand.OneShot)
        {
            OneShotPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }
}

public static class HotkeyRegistrationPlanner
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;

    public static HotkeyGesture SelectRegionGesture { get; } = new(ModControl | ModAlt, Key.R, "Ctrl+Alt+R");

    private static readonly IReadOnlyList<HotkeyBinding> Defaults =
    [
        new(
            HotkeyCommand.SelectRegion,
            Id: 1,
            [SelectRegionGesture]),
        new(HotkeyCommand.Pause, Id: 2, [new HotkeyGesture(ModControl | ModAlt, Key.T, "Ctrl+Alt+T")]),
        new(HotkeyCommand.Dismiss, Id: 3, [new HotkeyGesture(ModControl | ModAlt, Key.X, "Ctrl+Alt+X")]),
        new(HotkeyCommand.OneShot, Id: 4, [new HotkeyGesture(ModControl | ModAlt, Key.S, "Ctrl+Alt+S")])
    ];

    public static string DefaultShortcutSummary => BuildShortcutSummary(Defaults
        .Select(binding => new HotkeyRegistration(binding.Id, binding.Command, binding.Gestures[0]))
        .ToArray());

    public static HotkeyRegistrationState RegisterDefaults(IHotkeyRegistrar registrar, IntPtr handle)
    {
        var registrations = new List<HotkeyRegistration>();
        var failures = new List<HotkeyRegistrationFailure>();

        foreach (var binding in Defaults)
        {
            foreach (var gesture in binding.Gestures)
            {
                if (registrar.RegisterHotKey(handle, binding.Id, gesture.Modifiers, gesture.VirtualKey))
                {
                    registrations.Add(new HotkeyRegistration(binding.Id, binding.Command, gesture));
                    break;
                }

                failures.Add(new HotkeyRegistrationFailure(binding.Command, gesture));
            }
        }

        return new HotkeyRegistrationState(
            registrations,
            failures,
            BuildShortcutSummary(BuildDisplayRegistrations(registrations)));
    }

    private static IReadOnlyList<HotkeyRegistration> BuildDisplayRegistrations(
        IReadOnlyList<HotkeyRegistration> registrations)
    {
        if (registrations.Any(registration => registration.Command == HotkeyCommand.SelectRegion))
        {
            return registrations;
        }

        return registrations
            .Concat([new HotkeyRegistration(1, HotkeyCommand.SelectRegion, SelectRegionGesture)])
            .ToArray();
    }

    private static string BuildShortcutSummary(IReadOnlyList<HotkeyRegistration> registrations)
    {
        return string.Join(
            "  |  ",
            $"选择区域：{GetDisplayText(registrations, HotkeyCommand.SelectRegion)}",
            $"暂停/继续：{GetDisplayText(registrations, HotkeyCommand.Pause)}",
            $"隐藏当前：{GetDisplayText(registrations, HotkeyCommand.Dismiss)}",
            $"单次截图：{GetDisplayText(registrations, HotkeyCommand.OneShot)}");
    }

    private static string GetDisplayText(IReadOnlyList<HotkeyRegistration> registrations, HotkeyCommand command)
    {
        return registrations.FirstOrDefault(registration => registration.Command == command)?.Gesture.DisplayText
            ?? "未注册";
    }

    private sealed record HotkeyBinding(
        HotkeyCommand Command,
        int Id,
        IReadOnlyList<HotkeyGesture> Gestures);
}

public interface IHotkeyRegistrar
{
    bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    bool UnregisterHotKey(IntPtr hWnd, int id);
}

public enum HotkeyCommand
{
    SelectRegion,
    Pause,
    Dismiss,
    OneShot
}

public sealed record HotkeyGesture(uint Modifiers, Key Key, string DisplayText)
{
    public uint VirtualKey { get; } = (uint)KeyInterop.VirtualKeyFromKey(Key);
}

public sealed record HotkeyRegistration(int Id, HotkeyCommand Command, HotkeyGesture Gesture);

public sealed record HotkeyRegistrationFailure(HotkeyCommand Command, HotkeyGesture Gesture);

public sealed record HotkeyRegistrationState(
    IReadOnlyList<HotkeyRegistration> Registrations,
    IReadOnlyList<HotkeyRegistrationFailure> Failures,
    string ShortcutSummary);

internal sealed class LowLevelKeyboardHotkeyFallback : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkShift = 0x10;
    private readonly Action _onPressed;
    private readonly LowLevelKeyboardProc _proc;
    private readonly HotkeyGesture _gesture;
    private IntPtr _hookHandle;
    private bool _gestureIsDown;

    public LowLevelKeyboardHotkeyFallback(HotkeyGesture gesture, Action onPressed)
    {
        _gesture = gesture;
        _onPressed = onPressed;
        _proc = OnKeyboardMessage;
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _proc, IntPtr.Zero, 0);
    }

    public void Dispose()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private IntPtr OnKeyboardMessage(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        if (message is WmKeyDown or WmSysKeyDown)
        {
            var data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if (data.VirtualKey == _gesture.VirtualKey && ModifiersMatch())
            {
                if (!_gestureIsDown)
                {
                    _gestureIsDown = true;
                    _onPressed();
                }
            }
        }
        else if (message is WmKeyUp or WmSysKeyUp && !IsGestureCurrentlyDown())
        {
            _gestureIsDown = false;
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private bool ModifiersMatch()
    {
        return ModifierMatches(HotkeyRegistrationPlanner.ModControl, VkControl)
            && ModifierMatches(HotkeyRegistrationPlanner.ModAlt, VkMenu)
            && ModifierMatches(HotkeyRegistrationPlanner.ModShift, VkShift);
    }

    private bool ModifierMatches(uint modifier, int virtualKey)
    {
        var required = (_gesture.Modifiers & modifier) != 0;
        return required == IsKeyDown(virtualKey);
    }

    private bool IsGestureCurrentlyDown()
    {
        return IsKeyDown((int)_gesture.VirtualKey) && ModifiersMatch();
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & unchecked((short)0x8000)) != 0;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KbdLlHookStruct
    {
        public readonly uint VirtualKey;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}

public sealed class User32HotkeyRegistrar : IHotkeyRegistrar
{
    public bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey)
    {
        return RegisterHotKeyNative(hWnd, id, modifiers, virtualKey);
    }

    public bool UnregisterHotKey(IntPtr hWnd, int id)
    {
        return UnregisterHotKeyNative(hWnd, id);
    }

    [DllImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
    private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
    private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);
}
