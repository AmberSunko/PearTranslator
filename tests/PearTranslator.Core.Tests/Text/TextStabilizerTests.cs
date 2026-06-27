using PearTranslator.Core.Text;

namespace PearTranslator.Core.Tests.Text;

public sealed class TextStabilizerTests
{
    [Fact]
    public void RequiresSameNormalizedTextTwice()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe("  Hello   world "));
        Assert.Equal("Hello world", stabilizer.Observe("Hello world"));
    }

    [Fact]
    public void ResetsWhenTextChanges()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe("Hello"));
        Assert.Null(stabilizer.Observe("Welcome"));
        Assert.Equal("Welcome", stabilizer.Observe("Welcome"));
    }

    [Fact]
    public void IgnoresBlankText()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe(" "));
        Assert.Null(stabilizer.Observe(""));
    }

    [Fact]
    public void PreservesLineBreaksInStableText()
    {
        var stabilizer = new TextStabilizer(requiredRepeats: 2);

        Assert.Null(stabilizer.Observe("Open   the door\r\nTake the key"));

        Assert.Equal("Open the door\nTake the key", stabilizer.Observe("Open the door\nTake   the key"));
    }
}
