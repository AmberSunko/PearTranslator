using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using PearTranslator.App.Wpf.Hotkeys;
using PearTranslator.App.Wpf.Overlay;
using PearTranslator.App.Wpf.RegionSelection;
using PearTranslator.App.Wpf.Tray;
using PearTranslator.Core.Abstractions;
using PearTranslator.Core.Assets;
using PearTranslator.Core.Configuration;
using PearTranslator.Core.Control;
using PearTranslator.Core.Pipeline;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace PearTranslator.App.Wpf;

public partial class MainWindow : Window
{
    private const double TitleBarHeight = 46;
    private const double DefaultFooterHeight = 72;
    private static readonly Regex LatencyPattern = new(@"(?<latency>\d+)\s*ms\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly WpfBrush FastLatencyBrush = CreateFrozenBrush(WpfColor.FromRgb(0x2F, 0xBF, 0x71));
    private static readonly WpfBrush NormalLatencyBrush = CreateFrozenBrush(WpfColor.FromRgb(0x55, 0x6D, 0xEA));
    private static readonly WpfBrush SlowLatencyBrush = CreateFrozenBrush(WpfColor.FromRgb(0xD5, 0x32, 0x2F));
    private readonly TranslatorSettingsStore _settingsStore;
    private readonly OverlayWindow _realtimeOverlayWindow;
    private readonly OverlayWindow _oneShotOverlayWindow;
    private readonly DualOverlayPresenter _overlayPresenter;
    private readonly CompositionRoot _composition;
    private readonly GlobalHotkeyService _hotkeys;
    private readonly TrayIconService _tray;
    private TranslatorSettings _settings;
    private bool _isLoadingSettings;
    private HwndSource? _keyboardCueSource;
    private RegionSelectionWindow? _activeRegionSelector;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;

        _settingsStore = TranslatorSettingsStore.CreateDefault();
        _settings = _settingsStore.Load();
        _realtimeOverlayWindow = new OverlayWindow();
        _oneShotOverlayWindow = new OverlayWindow();
        _overlayPresenter = new DualOverlayPresenter(
            _realtimeOverlayWindow,
            _oneShotOverlayWindow,
            _settings.Overlay.OneShotDisplaySeconds);
        _overlayPresenter.PositionOverlayEnabled = _settings.Overlay.PositionOverlay;
        _overlayPresenter.ExcludeOverlayFromCapture = _settings.Overlay.ExcludeOverlayFromCapture;
        _overlayPresenter.TargetLanguage = _settings.Translation.TargetLanguage;
        _overlayPresenter.UiLanguage = _settings.Appearance.UiLanguage;
        _composition = new CompositionRoot(_overlayPresenter, _settings);
        _hotkeys = new GlobalHotkeyService(this);
        _tray = new TrayIconService(this);

        _hotkeys.SelectRegionPressed += (_, _) => SelectRegion();
        _hotkeys.PausePressed += (_, _) => TogglePause();
        _hotkeys.DismissPressed += (_, _) => DismissCurrent();
        _hotkeys.OneShotPressed += async (_, _) => await RunOneShotAsync();
        _hotkeys.ShortcutSummaryChanged += (_, _) => ShortcutTextBlock.Text = CurrentText.FormatShortcutSummary(_hotkeys.ShortcutSummary);
        ShortcutTextBlock.Text = CurrentText.FormatShortcutSummary(_hotkeys.ShortcutSummary);
        _composition.StatusChanged += (_, message) => SetConnectionStatus(message);
        _composition.ConnectionStatusChanged += (_, message) => SetConnectionStatus(message);
        Loaded += async (_, _) =>
        {
            ApplyWindowChromeState(IsActive);
            await TestTranslationConnectionAsync();
        };
        Activated += (_, _) => ApplyWindowChromeState(isActive: true);
        Deactivated += (_, _) => ApplyWindowChromeState(isActive: false);
        SizeChanged += (_, _) => ApplyWindowChromeState(IsActive);

        Closed += (_, _) =>
        {
            _keyboardCueSource?.RemoveHook(SuppressKeyboardCueMessages);
            _composition.Stop();
            _hotkeys.Dispose();
            _tray.Dispose();
            _overlayPresenter.Dispose();
            _realtimeOverlayWindow.Close();
            _oneShotOverlayWindow.Close();
        };

        InitializeSettingsControls();
        ApplyUiLanguage();
        UpdateProviderControls();
        UpdateRuntimeAssetStatus();
        UpdateRunStateStatus();
        SetConnectionStatus(BuildConnectionWaitingStatus(_settings));
    }

    private MainWindowTextCatalog CurrentText => MainWindowTextCatalog.For(_settings.Appearance.UiLanguage);

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        ApplyWindowChromeState(IsActive);
        var handle = new WindowInteropHelper(this).Handle;
        _keyboardCueSource = HwndSource.FromHwnd(handle);
        _keyboardCueSource?.AddHook(SuppressKeyboardCueMessages);
        HideKeyboardCues(handle);
    }

    private IntPtr SuppressKeyboardCueMessages(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (!KeyboardCueMessagePolicy.ShouldSuppress(msg, wParam))
        {
            return IntPtr.Zero;
        }

        handled = true;
        HideKeyboardCues(hwnd);
        return IntPtr.Zero;
    }

    private static void HideKeyboardCues(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SendMessage(handle, KeyboardCueMessagePolicy.WmChangeUiState, KeyboardCueMessagePolicy.HideKeyboardCuesWParam, IntPtr.Zero);
        }
    }

    private void ApplyWindowChromeState(bool isActive)
    {
        var brush = (WpfBrush)FindResource(isActive
            ? "ActiveGlassChromeBrush"
            : "InactiveGlassChromeBrush");
        Background = brush;
        RootChrome.Background = brush;
        ApplyWindowBackdrop(isActive);
    }

    private void ApplyWindowBackdrop(bool isActive)
    {
        var contentCornerRadius = ContentPanel.CornerRadius;
        var topHeight = WindowChromeMetrics.CalculateTopGlassHeight(
            TitleBarHeight,
            contentCornerRadius.TopLeft,
            contentCornerRadius.TopRight);

        var footerMargin = FooterChrome.Margin;
        var footerHeight = WindowChromeMetrics.CalculateBottomGlassHeight(
            FooterChrome.ActualHeight,
            footerMargin.Top,
            footerMargin.Bottom,
            DefaultFooterHeight,
            ContentPanel.Margin.Bottom,
            contentCornerRadius.BottomLeft,
            contentCornerRadius.BottomRight);
        WindowBackdropService.ApplyLiquidGlass(this, topHeight, footerHeight, isActive);
    }

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnSelectRegionClicked(object sender, RoutedEventArgs e)
    {
        SelectRegion();
    }

    private void OnPauseClicked(object sender, RoutedEventArgs e)
    {
        TogglePause();
    }

    private async void OnOneShotClicked(object sender, RoutedEventArgs e)
    {
        await RunOneShotAsync();
    }

    private void OnProviderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        UpdateProviderControls();
    }

    private void OnOpenAiServiceSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        var service = GetSelectedOpenAiService();
        OpenAiBaseUriTextBox.Text = OpenAiProviderSettings.GetDefaultBaseUri(service);
        OpenAiCustomModelTextBox.Text = string.Empty;
        UpdateOpenAiModelChoices(OpenAiProviderSettings.GetDefaultModel(service));
        UpdateOpenAiServiceControls();
        UpdateCustomModelVisibility();
    }

    private void OnOpenAiModelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        UpdateCustomModelVisibility();
    }

    private void OnUiLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settings = ReadSettingsFromControls();
        _settingsStore.Save(_settings);
        ApplyUiLanguage();
        SetConnectionStatus(BuildConnectionWaitingStatus(_settings));
    }

    private async void OnRefreshOpenAiModelsClicked(object sender, RoutedEventArgs e)
    {
        var text = CurrentText;
        var previousContent = RefreshModelsButton.Content;
        RefreshModelsButton.IsEnabled = false;
        RefreshModelsButton.Content = text.Language == UiLanguage.English ? "Fetching..." : "获取中...";
        SetConnectionStatus(text.Language == UiLanguage.English ? "Fetching model list..." : "正在获取模型列表...");

        try
        {
            var selectedModel = OpenAiModelComboBox.SelectedValue as string;
            var models = await _composition.ListOpenAiCompatibleModelsAsync(
                ReadOpenAiSettingsFromControls(),
                CancellationToken.None);

            if (models.Count == 0)
            {
                SetConnectionStatus(text.Language == UiLanguage.English
                    ? "No models returned. This platform may not support /models; use a custom model."
                    : "没有获取到模型。该平台可能不支持 /models，请使用自定义模型。");
                return;
            }

            SetOpenAiModelChoices(models, selectedModel);
            SetConnectionStatus(text.Language == UiLanguage.English
                ? $"Fetched {models.Count} models."
                : $"已获取 {models.Count} 个模型。");
        }
        catch (Exception exception)
        {
            SetConnectionStatus(text.Language == UiLanguage.English
                ? $"Failed to fetch models: {exception.Message}"
                : $"获取模型失败：{exception.Message}");
        }
        finally
        {
            RefreshModelsButton.Content = previousContent;
            RefreshModelsButton.IsEnabled = true;
        }
    }

    private async void OnConfigureAssetsClicked(object sender, RoutedEventArgs e)
    {
        var text = CurrentText;
        ConfigureAssetsButton.IsEnabled = false;
        RuntimeAssetsStatusText.Text = text.Language == UiLanguage.English
            ? "Preparing to download OCR models and the ECDICT dictionary."
            : "准备下载 OCR 模型和 ECDICT 字典。";
        SetConnectionStatus(text.Language == UiLanguage.English
            ? "Configuring local OCR models and dictionary..."
            : "正在配置本地 OCR 模型和字典...");

        try
        {
            var progress = new Progress<RuntimeAssetSetupProgress>(assetProgress =>
            {
                var message = $"{assetProgress.Message} ({assetProgress.CompletedAssets}/{assetProgress.TotalAssets})";
                RuntimeAssetsStatusText.Text = message;
                SetConnectionStatus(message);
            });

            await _composition.ConfigureRuntimeAssetsAsync(progress, CancellationToken.None);
            UpdateRuntimeAssetStatus();
            SetConnectionStatus(text.Language == UiLanguage.English
                ? "Local OCR models and dictionary configured."
                : "本地 OCR 模型和字典配置完成。");
        }
        catch (OperationCanceledException)
        {
            RuntimeAssetsStatusText.Text = text.Language == UiLanguage.English
                ? "Model configuration canceled."
                : "已取消配置模型。";
            SetConnectionStatus(RuntimeAssetsStatusText.Text);
        }
        catch (Exception exception)
        {
            RuntimeAssetsStatusText.Text = text.Language == UiLanguage.English
                ? $"Configuration failed: {exception.Message}"
                : $"配置失败：{exception.Message}";
            SetConnectionStatus(text.Language == UiLanguage.English
                ? $"Model configuration failed: {exception.Message}"
                : $"配置模型失败：{exception.Message}");
        }
        finally
        {
            ConfigureAssetsButton.IsEnabled = true;
        }
    }

    private void OnSaveSettingsClicked(object sender, RoutedEventArgs e)
    {
        _settings = ReadSettingsFromControls();
        _settingsStore.Save(_settings);
        _overlayPresenter.PositionOverlayEnabled = _settings.Overlay.PositionOverlay;
        _overlayPresenter.ExcludeOverlayFromCapture = _settings.Overlay.ExcludeOverlayFromCapture;
        _overlayPresenter.TargetLanguage = _settings.Translation.TargetLanguage;
        _overlayPresenter.UiLanguage = _settings.Appearance.UiLanguage;
        _overlayPresenter.UpdateOneShotDisplaySeconds(_settings.Overlay.OneShotDisplaySeconds);
        _composition.ApplySettings(_settings);
        ApplyUiLanguage();
        UpdateProviderControls();
        UpdateRunStateStatus();
        SetConnectionStatus(BuildConnectionWaitingStatus(_settings));
        _ = TestTranslationConnectionAsync();
    }

    private void OnPositionOverlayChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settings = ReadSettingsFromControls();
        _settingsStore.Save(_settings);
        _overlayPresenter.PositionOverlayEnabled = _settings.Overlay.PositionOverlay;
        _overlayPresenter.ExcludeOverlayFromCapture = _settings.Overlay.ExcludeOverlayFromCapture;
        _overlayPresenter.TargetLanguage = _settings.Translation.TargetLanguage;
        _overlayPresenter.UiLanguage = _settings.Appearance.UiLanguage;
        _overlayPresenter.UpdateOneShotDisplaySeconds(_settings.Overlay.OneShotDisplaySeconds);
        _composition.ApplySettings(_settings);
        SetConnectionStatus(BuildConnectionWaitingStatus(_settings));
    }

    private async Task TestTranslationConnectionAsync()
    {
        try
        {
            await _composition.TestTranslationConnectionAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            SetConnectionStatus(CurrentText.Language == UiLanguage.English
                ? $"Preflight connection failed: {exception.Message}"
                : $"\u9884\u8fde\u63a5\u5931\u8d25\uff1a{exception.Message}");
        }
    }

    private void SelectRegion()
    {
        if (ShowRegionSelector() is { } region)
        {
            _composition.Capture.SetRegion(region);
            _composition.Controller.Resume();
            _overlayPresenter.ShowRealtimePending(region);
            _composition.Start();
            UpdateRunStateStatus();
            SetConnectionStatus(BuildConnectionWaitingStatus(_settings));
        }
    }

    private FrameRegion? ShowRegionSelector()
    {
        if (_activeRegionSelector is not null)
        {
            _activeRegionSelector.Activate();
            return null;
        }

        var selector = new RegionSelectionWindow { Owner = this };
        _activeRegionSelector = selector;
        selector.Closed += (_, _) => _activeRegionSelector = null;

        return selector.ShowDialog() == true && selector.SelectedRegion is { } region
            ? region
            : null;
    }

    private void TogglePause()
    {
        _composition.Controller.TogglePause();
        UpdateRunStateStatus();
    }

    private async void DismissCurrent()
    {
        try
        {
            await _composition.ToggleDismissCurrentAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            UpdateRunStateStatus();
        }
    }

    private async Task RunOneShotAsync()
    {
        _overlayPresenter.PrepareOneShotSelection();
        if (ShowRegionSelector() is not { } region)
        {
            SetConnectionStatus("已取消单次截图");
            return;
        }

        SetConnectionStatus(CurrentText.Language == UiLanguage.English
            ? "Translating current screenshot..."
            : "正在翻译当前截图...");
        var result = await _composition.RunOneShotAsync(region, CancellationToken.None);
        SetConnectionStatus(result.Outcome == TranslationLoopOutcome.Failed
            ? CurrentText.Language == UiLanguage.English
                ? "One-shot translation failed. Check translation service settings, network, or API key."
                : "单次截图翻译失败。请检查翻译服务设置、网络或 API 密钥。"
            : CurrentText.Language == UiLanguage.English
                ? "One-shot translation complete."
                : "单次截图翻译完成。");
    }

    private void InitializeSettingsControls()
    {
        _isLoadingSettings = true;

        RefreshLocalizedChoiceItems();
        UiLanguageComboBox.SelectedValue = _settings.Appearance.UiLanguage;
        ProviderComboBox.SelectedValue = _settings.Translation.Provider;
        TargetLanguageComboBox.SelectedValue = _settings.Translation.TargetLanguage;
        OpenAiServiceComboBox.SelectedValue = _settings.Translation.OpenAi.Service;
        OcrEngineComboBox.SelectedValue = _settings.Ocr.Engine;
        OcrLanguageComboBox.SelectedValue = _settings.Ocr.Language;
        OneShotDisplayComboBox.SelectedValue = Math.Max(0, _settings.Overlay.OneShotDisplaySeconds);
        OpenAiApiKeyBox.Password = _settings.Translation.OpenAi.ApiKey;
        OpenAiCustomModelTextBox.Text = _settings.Translation.OpenAi.CustomModel;
        OpenAiBaseUriTextBox.Text = _settings.Translation.OpenAi.EffectiveBaseUri;
        LocalPreviewCheckBox.IsChecked = _settings.Overlay.LocalPreviewEnabled;
        ExcludeOverlayFromCaptureCheckBox.IsChecked = _settings.Overlay.ExcludeOverlayFromCapture;
        PositionOverlayCheckBox.IsChecked = _settings.Overlay.PositionOverlay;
        OcrPositionTestCheckBox.IsChecked = _settings.Overlay.OcrPositionTest;
        UpdateOpenAiModelChoices(_settings.Translation.OpenAi.Model);
        LoadTraditionalProviderFields(_settings.Translation.Provider);

        _isLoadingSettings = false;
    }

    private TranslatorSettings ReadSettingsFromControls()
    {
        var provider = ProviderComboBox.SelectedValue is TranslationProviderKind selectedProvider
            ? selectedProvider
            : TranslationProviderKind.None;

        var current = _settings.Translation;
        return new TranslatorSettings
        {
            Translation = new TranslationSettings
            {
                Provider = provider,
                TargetLanguage = TargetLanguageComboBox.SelectedValue is TargetLanguage selectedTargetLanguage
                    ? selectedTargetLanguage
                    : TargetLanguage.SimplifiedChinese,
                OpenAi = ReadOpenAiSettingsFromControls(),
                Azure = provider == TranslationProviderKind.Azure
                    ? ReadTraditionalProviderFields()
                    : current.Azure,
                DeepL = provider == TranslationProviderKind.DeepL
                    ? ReadTraditionalProviderFields()
                    : current.DeepL,
                Google = provider == TranslationProviderKind.Google
                    ? ReadTraditionalProviderFields()
                    : current.Google
            },
            Overlay = new OverlaySettings
            {
                LocalPreviewEnabled = LocalPreviewCheckBox.IsChecked == true,
                ExcludeOverlayFromCapture = ExcludeOverlayFromCaptureCheckBox.IsChecked == true,
                PositionOverlay = PositionOverlayCheckBox.IsChecked == true,
                OcrPositionTest = OcrPositionTestCheckBox.IsChecked == true,
                OneShotDisplaySeconds = OneShotDisplayComboBox.SelectedValue is int selectedSeconds
                    ? selectedSeconds
                    : 0
            },
            Ocr = new OcrSettings
            {
                Engine = OcrEngineComboBox.SelectedValue is OcrEngineKind selectedEngine
                    ? selectedEngine
                    : OcrEngineKind.LocalRapidOcr,
                Language = OcrLanguageComboBox.SelectedValue is OcrLanguageKind selectedLanguage
                    ? selectedLanguage
                    : OcrLanguageKind.English
            },
            Appearance = new AppearanceSettings
            {
                UiLanguage = UiLanguageComboBox.SelectedValue is UiLanguage selectedUiLanguage
                    ? selectedUiLanguage
                    : UiLanguage.SimplifiedChinese
            }
        };
    }

    private OpenAiProviderSettings ReadOpenAiSettingsFromControls()
    {
        var service = GetSelectedOpenAiService();
        return new OpenAiProviderSettings
        {
            Service = service,
            ApiKey = OpenAiApiKeyBox.Password,
            Model = OpenAiModelComboBox.SelectedValue as string ?? OpenAiProviderSettings.GetDefaultModel(service),
            CustomModel = OpenAiCustomModelTextBox.Text,
            BaseUri = OpenAiBaseUriTextBox.Text
        };
    }

    private TraditionalProviderSettings ReadTraditionalProviderFields()
    {
        return new TraditionalProviderSettings
        {
            ApiKey = TraditionalApiKeyTextBox.Text,
            Endpoint = TraditionalEndpointTextBox.Text,
            Region = TraditionalEndpointTextBox.Text,
            Project = TraditionalEndpointTextBox.Text
        };
    }

    private void ApplyUiLanguage()
    {
        var text = CurrentText;
        _overlayPresenter.UiLanguage = _settings.Appearance.UiLanguage;
        ApplyStaticText(this, text);
        RefreshLocalizedChoiceItems();
        ShortcutTextBlock.Text = text.FormatShortcutSummary(_hotkeys.ShortcutSummary);
        UpdateProviderControls();
        UpdateRuntimeAssetStatus();
        UpdateRunStateStatus();
    }

    private void RefreshLocalizedChoiceItems()
    {
        var text = CurrentText;
        var wasLoading = _isLoadingSettings;
        _isLoadingSettings = true;

        var selectedUiLanguage = UiLanguageComboBox.SelectedValue is UiLanguage currentUiLanguage
            ? currentUiLanguage
            : _settings.Appearance.UiLanguage;
        var selectedProvider = ProviderComboBox.SelectedValue is TranslationProviderKind currentProvider
            ? currentProvider
            : _settings.Translation.Provider;
        var selectedTargetLanguage = TargetLanguageComboBox.SelectedValue is TargetLanguage currentTargetLanguage
            ? currentTargetLanguage
            : _settings.Translation.TargetLanguage;
        var selectedService = OpenAiServiceComboBox.SelectedValue is OpenAiCompatibleService currentService
            ? currentService
            : _settings.Translation.OpenAi.Service;
        var selectedEngine = OcrEngineComboBox.SelectedValue is OcrEngineKind currentEngine
            ? currentEngine
            : _settings.Ocr.Engine;
        var selectedOcrLanguage = OcrLanguageComboBox.SelectedValue is OcrLanguageKind currentOcrLanguage
            ? currentOcrLanguage
            : _settings.Ocr.Language;
        var selectedSeconds = OneShotDisplayComboBox.SelectedValue is int currentSeconds
            ? currentSeconds
            : Math.Max(0, _settings.Overlay.OneShotDisplaySeconds);
        var selectedModel = OpenAiModelComboBox.SelectedValue as string ?? _settings.Translation.OpenAi.Model;

        UiLanguageComboBox.ItemsSource = new[]
        {
            new UiLanguageChoice(UiLanguage.SimplifiedChinese, text.Language == UiLanguage.English ? "Simplified Chinese" : "简体中文"),
            new UiLanguageChoice(UiLanguage.English, "English")
        };

        ProviderComboBox.ItemsSource = new[]
        {
            new ProviderChoice(TranslationProviderKind.None, text.ProviderNone),
            new ProviderChoice(TranslationProviderKind.OpenAi, text.ProviderOpenAi),
            new ProviderChoice(TranslationProviderKind.Azure, text.ProviderAzure),
            new ProviderChoice(TranslationProviderKind.DeepL, text.ProviderDeepL),
            new ProviderChoice(TranslationProviderKind.Google, text.ProviderGoogle),
            new ProviderChoice(TranslationProviderKind.Mock, text.ProviderMock)
        };

        OpenAiServiceComboBox.ItemsSource = new[]
        {
            new ServiceChoice(OpenAiCompatibleService.OpenAi, text.ServiceLabel(OpenAiCompatibleService.OpenAi)),
            new ServiceChoice(OpenAiCompatibleService.DeepSeek, text.ServiceLabel(OpenAiCompatibleService.DeepSeek)),
            new ServiceChoice(OpenAiCompatibleService.Qwen, text.ServiceLabel(OpenAiCompatibleService.Qwen)),
            new ServiceChoice(OpenAiCompatibleService.Kimi, text.ServiceLabel(OpenAiCompatibleService.Kimi)),
            new ServiceChoice(OpenAiCompatibleService.Zhipu, text.ServiceLabel(OpenAiCompatibleService.Zhipu)),
            new ServiceChoice(OpenAiCompatibleService.Doubao, text.ServiceLabel(OpenAiCompatibleService.Doubao)),
            new ServiceChoice(OpenAiCompatibleService.Custom, text.ServiceLabel(OpenAiCompatibleService.Custom))
        };

        OcrEngineComboBox.ItemsSource = new[]
        {
            new OcrEngineChoice(OcrEngineKind.Windows, text.OcrEngineWindows),
            new OcrEngineChoice(OcrEngineKind.LocalRapidOcr, text.OcrEngineLocal)
        };

        TargetLanguageComboBox.ItemsSource = new[]
        {
            new TargetLanguageChoice(TargetLanguage.SimplifiedChinese, text.TargetSimplifiedChinese),
            new TargetLanguageChoice(TargetLanguage.English, text.TargetEnglish)
        };

        OcrLanguageComboBox.ItemsSource = new[]
        {
            new OcrLanguageChoice(OcrLanguageKind.Auto, text.OcrAuto),
            new OcrLanguageChoice(OcrLanguageKind.English, text.TargetEnglish),
            new OcrLanguageChoice(OcrLanguageKind.Chinese, text.OcrChinese),
            new OcrLanguageChoice(OcrLanguageKind.Japanese, text.OcrJapanese),
            new OcrLanguageChoice(OcrLanguageKind.Korean, text.OcrKorean)
        };

        OneShotDisplayComboBox.ItemsSource = new[]
        {
            new OneShotDisplayDurationChoice(0, text.OneShotKeep),
            new OneShotDisplayDurationChoice(3, text.Language == UiLanguage.English ? "3 sec" : "3 秒"),
            new OneShotDisplayDurationChoice(5, text.Language == UiLanguage.English ? "5 sec" : "5 秒"),
            new OneShotDisplayDurationChoice(10, text.Language == UiLanguage.English ? "10 sec" : "10 秒"),
            new OneShotDisplayDurationChoice(30, text.Language == UiLanguage.English ? "30 sec" : "30 秒"),
            new OneShotDisplayDurationChoice(60, text.Language == UiLanguage.English ? "60 sec" : "60 秒")
        };

        UiLanguageComboBox.SelectedValue = selectedUiLanguage;
        ProviderComboBox.SelectedValue = selectedProvider;
        TargetLanguageComboBox.SelectedValue = selectedTargetLanguage;
        OpenAiServiceComboBox.SelectedValue = selectedService;
        OcrEngineComboBox.SelectedValue = selectedEngine;
        OcrLanguageComboBox.SelectedValue = selectedOcrLanguage;
        OneShotDisplayComboBox.SelectedValue = selectedSeconds;
        UpdateOpenAiModelChoices(selectedModel);

        _isLoadingSettings = wasLoading;
    }

    private static void ApplyStaticText(DependencyObject root, MainWindowTextCatalog text)
    {
        if (root is TextBlock textBlock &&
            !string.IsNullOrWhiteSpace(textBlock.Text) &&
            text.TryTranslateStaticText(textBlock.Text, out var localizedText))
        {
            textBlock.Text = localizedText;
        }
        else if (root is System.Windows.Controls.Button button &&
            button.Content is string buttonContent &&
            text.TryTranslateStaticText(buttonContent, out var localizedButtonContent))
        {
            button.Content = localizedButtonContent;
        }
        else if (root is System.Windows.Controls.CheckBox checkBox &&
            checkBox.Content is string checkBoxContent &&
            text.TryTranslateStaticText(checkBoxContent, out var localizedCheckBoxContent))
        {
            checkBox.Content = localizedCheckBoxContent;
        }

        var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            ApplyStaticText(System.Windows.Media.VisualTreeHelper.GetChild(root, index), text);
        }
    }

    private void UpdateProviderControls()
    {
        var provider = ProviderComboBox.SelectedValue is TranslationProviderKind selectedProvider
            ? selectedProvider
            : TranslationProviderKind.None;

        OpenAiSettingsPanel.Visibility = provider == TranslationProviderKind.OpenAi
            ? Visibility.Visible
            : Visibility.Collapsed;
        TraditionalSettingsPanel.Visibility = IsTraditionalProvider(provider)
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (IsTraditionalProvider(provider))
        {
            LoadTraditionalProviderFields(provider);
        }

        ProviderHintText.Text = CurrentText.ProviderHint(provider);

        UpdateOpenAiServiceControls();
        UpdateCustomModelVisibility();
    }

    private void UpdateRuntimeAssetStatus()
    {
        var status = _composition.GetRuntimeAssetStatus();
        RuntimeAssetsStatusText.Text = CurrentText.RuntimeAssetsStatus(status.IsComplete, status.MissingAssets.Count);
    }

    private void UpdateOpenAiModelChoices(string? requestedModel = null)
    {
        var service = GetSelectedOpenAiService();
        SetOpenAiModelChoices(OpenAiProviderSettings.GetModelPresets(service), requestedModel);
    }

    private void SetOpenAiModelChoices(IEnumerable<string> models, string? requestedModel)
    {
        var service = GetSelectedOpenAiService();
        var modelList = models
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Append(OpenAiProviderSettings.CustomModelValue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        OpenAiModelComboBox.ItemsSource = modelList
            .Select(model => new ModelChoice(model, CurrentText.ModelLabel(model)))
            .ToArray();

        OpenAiModelComboBox.SelectedValue = modelList.Contains(
            string.IsNullOrWhiteSpace(requestedModel) ? string.Empty : requestedModel.Trim(),
            StringComparer.OrdinalIgnoreCase)
            ? requestedModel
            : NormalizeOpenAiModel(service, modelList[0]);
    }

    private void UpdateOpenAiServiceControls()
    {
        var service = GetSelectedOpenAiService();
        var canEditEndpoint = service is OpenAiCompatibleService.OpenAi or OpenAiCompatibleService.Custom;

        if (!canEditEndpoint || string.IsNullOrWhiteSpace(OpenAiBaseUriTextBox.Text))
        {
            OpenAiBaseUriTextBox.Text = OpenAiProviderSettings.GetDefaultBaseUri(service);
        }

        OpenAiBaseUriTextBox.IsEnabled = canEditEndpoint;
        OpenAiBaseUriTextBox.Opacity = canEditEndpoint ? 1.0 : 0.65;
    }

    private void UpdateCustomModelVisibility()
    {
        CustomModelPanel.Visibility =
            string.Equals(OpenAiModelComboBox.SelectedValue as string, OpenAiProviderSettings.CustomModelValue, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void LoadTraditionalProviderFields(TranslationProviderKind provider)
    {
        var settings = provider switch
        {
            TranslationProviderKind.Azure => _settings.Translation.Azure,
            TranslationProviderKind.DeepL => _settings.Translation.DeepL,
            TranslationProviderKind.Google => _settings.Translation.Google,
            _ => new TraditionalProviderSettings()
        };

        TraditionalProviderTitle.Text = CurrentText.TraditionalProviderTitle(provider);
        TraditionalApiKeyTextBox.Text = settings.ApiKey;
        TraditionalEndpointTextBox.Text = FirstNonBlank(settings.Endpoint, settings.Region, settings.Project);
    }

    private OpenAiCompatibleService GetSelectedOpenAiService()
    {
        return OpenAiServiceComboBox.SelectedValue is OpenAiCompatibleService selectedService
            ? selectedService
            : OpenAiCompatibleService.OpenAi;
    }

    private static bool IsTraditionalProvider(TranslationProviderKind provider)
    {
        return provider is TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google;
    }

    private static string NormalizeOpenAiModel(OpenAiCompatibleService service, string? model)
    {
        var value = string.IsNullOrWhiteSpace(model) ? OpenAiProviderSettings.GetDefaultModel(service) : model.Trim();
        return OpenAiProviderSettings.GetModelPresets(service).Contains(value)
            ? value
            : OpenAiProviderSettings.CustomModelValue;
    }

    private string BuildProviderStatus(TranslatorSettings settings)
    {
        return CurrentText.BuildProviderStatus(settings);
    }

    private void UpdateRunStateStatus()
    {
        StatusText.Text = CurrentText.RunState(_composition.Controller.State);
        PauseButton.Content = CurrentText.PauseCommand(_composition.Controller.State);
    }

    private void SetConnectionStatus(string message)
    {
        ConnectionStatusText.Inlines.Clear();

        var match = LatencyPattern.Match(message);
        if (!match.Success ||
            !int.TryParse(match.Groups["latency"].Value, out var latencyMilliseconds))
        {
            ConnectionStatusText.Inlines.Add(new Run(message));
            return;
        }

        if (match.Index > 0)
        {
            ConnectionStatusText.Inlines.Add(new Run(message[..match.Index]));
        }

        ConnectionStatusText.Inlines.Add(new Run(match.Value)
        {
            Foreground = GetLatencyBrush(latencyMilliseconds)
        });

        var afterLatencyIndex = match.Index + match.Length;
        if (afterLatencyIndex < message.Length)
        {
            ConnectionStatusText.Inlines.Add(new Run(message[afterLatencyIndex..]));
        }
    }

    private static WpfBrush GetLatencyBrush(int latencyMilliseconds)
    {
        return TranslationLatencyClassifier.Classify(TimeSpan.FromMilliseconds(latencyMilliseconds)) switch
        {
            TranslationLatencyBand.Fast => FastLatencyBrush,
            TranslationLatencyBand.Normal => NormalLatencyBrush,
            _ => SlowLatencyBrush
        };
    }

    private static WpfBrush CreateFrozenBrush(WpfColor color)
    {
        var brush = new WpfSolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private string BuildConnectionWaitingStatus(TranslatorSettings settings)
    {
        var waitingMessage = CurrentText.WaitingReason(settings.Translation.TargetLanguage);
        if (CurrentText.Language == UiLanguage.English)
        {
            return settings.Translation.Provider switch
            {
                TranslationProviderKind.OpenAi =>
                    $"{CurrentText.ServiceLabel(settings.Translation.OpenAi.Service)} · {waitingMessage}",
                TranslationProviderKind.Azure or TranslationProviderKind.DeepL or TranslationProviderKind.Google =>
                    $"{settings.Translation.Provider}: {waitingMessage}",
                TranslationProviderKind.Mock =>
                    $"Mock: {waitingMessage}",
                _ =>
                    $"No API · {waitingMessage}"
            };
        }

        return TranslationConnectionStatusFormatter.FormatNoRequest(settings, waitingMessage);
    }

    private static string FirstNonBlank(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed record ProviderChoice(TranslationProviderKind Kind, string Name);

    private sealed record ServiceChoice(OpenAiCompatibleService Service, string Name);

    private sealed record ModelChoice(string Value, string Name);

    private sealed record UiLanguageChoice(UiLanguage Language, string Name);

    private sealed record TargetLanguageChoice(TargetLanguage Language, string Name);

    private sealed record OcrEngineChoice(OcrEngineKind Engine, string Name);

    private sealed record OcrLanguageChoice(OcrLanguageKind Language, string Name);

    private sealed record OneShotDisplayDurationChoice(int Seconds, string Name);
}
