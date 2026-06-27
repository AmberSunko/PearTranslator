namespace PearTranslator.App.Wpf;

public static class KeyboardCueMessagePolicy
{
    public const int WmSysKeyDown = 0x0104;
    public const int WmSysKeyUp = 0x0105;
    public const int WmChangeUiState = 0x0127;
    public const int WmUpdateUiState = 0x0128;
    public const int VkMenu = 0x12;
    public const int UisSet = 1;
    public const int UisClear = 2;
    public const int UisfHideFocus = 0x1;
    public const int UisfHideAccel = 0x2;

    public static IntPtr HideKeyboardCuesWParam { get; } =
        MakeWParam(UisSet, UisfHideFocus | UisfHideAccel);

    public static bool ShouldSuppress(int message, IntPtr wParam)
    {
        if ((message is WmSysKeyDown or WmSysKeyUp) && wParam.ToInt32() == VkMenu)
        {
            return true;
        }

        if (message is not WmChangeUiState and not WmUpdateUiState)
        {
            return false;
        }

        var action = LoWord(wParam);
        var flags = HiWord(wParam);
        return action == UisClear &&
            (flags & (UisfHideFocus | UisfHideAccel)) != 0;
    }

    public static IntPtr MakeWParam(int lowWord, int highWord)
    {
        return (IntPtr)((highWord << 16) | (lowWord & 0xFFFF));
    }

    private static int LoWord(IntPtr value)
    {
        return value.ToInt32() & 0xFFFF;
    }

    private static int HiWord(IntPtr value)
    {
        return (value.ToInt32() >> 16) & 0xFFFF;
    }
}
