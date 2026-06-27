using PearTranslator.Core.Control;

namespace PearTranslator.Core.Tests.Control;

public sealed class TranslatorControllerTests
{
    [Fact]
    public void StartsRunning()
    {
        var controller = new TranslatorController();

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void PauseHidesOverlayAndResumeShowsIt()
    {
        var controller = new TranslatorController();

        controller.TogglePause();

        Assert.Equal(TranslatorRunState.Paused, controller.State);
        Assert.False(controller.ShouldCapture);
        Assert.False(controller.ShouldShowOverlay);

        controller.TogglePause();

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldCapture);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void ResumeSwitchesPausedStateBackToRunning()
    {
        var controller = new TranslatorController();
        controller.TogglePause();

        controller.Resume();

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldCapture);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void ResumeKeepsRunningStateRunning()
    {
        var controller = new TranslatorController();

        controller.Resume();

        Assert.Equal(TranslatorRunState.Running, controller.State);
    }

    [Fact]
    public void DismissHidesCurrentSubtitleUntilDifferentSourceArrives()
    {
        var controller = new TranslatorController();
        controller.AcceptStableSourceText("Hello");

        controller.DismissCurrent();

        Assert.Equal(TranslatorRunState.Dismissed, controller.State);
        Assert.False(controller.ShouldShowOverlay);

        controller.AcceptStableSourceText("Hello");

        Assert.Equal(TranslatorRunState.Dismissed, controller.State);
        Assert.False(controller.ShouldShowOverlay);

        controller.AcceptStableSourceText("Welcome back");

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void DismissResumesWhenDifferentSourceArrivesImmediately()
    {
        var controller = new TranslatorController();
        controller.AcceptStableSourceText("Hello");

        controller.DismissCurrent();
        controller.AcceptStableSourceText("Welcome back");

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldShowOverlay);
    }

    [Fact]
    public void RestoreDismissedSwitchesDismissedStateBackToRunning()
    {
        var controller = new TranslatorController();
        controller.AcceptStableSourceText("Hello");
        controller.DismissCurrent();

        controller.RestoreDismissed();

        Assert.Equal(TranslatorRunState.Running, controller.State);
        Assert.True(controller.ShouldCapture);
        Assert.True(controller.ShouldShowOverlay);
    }
}
