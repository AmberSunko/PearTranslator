namespace PearTranslator.Core.Control;

public static class TranslatorRunStateStatusFormatter
{
    public static string Format(TranslatorRunState state)
    {
        return state switch
        {
            TranslatorRunState.Paused => "已暂停",
            TranslatorRunState.Dismissed => "已隐藏当前字幕",
            _ => "正在运行"
        };
    }
}
