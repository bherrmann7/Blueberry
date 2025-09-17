using Xunit;
using BluelBerry;

namespace bb.Tests;

public class AppOptionsParserTests
{
    [Fact]
    public void Parse_WithValidArguments_ShouldReturnExpectedOptions()
    {
        var args = new[] { "--model", "gpt-4o", "--endpoint", "https://api.openai.com/v1" };

        var options = AppOptionsParser.Parse<AppOptions>(args);

        Assert.Equal("gpt-4o", options.model);
        Assert.Equal("https://api.openai.com/v1", options.endpoint);
    }

    [Fact]
    public void Parse_WithNoArguments_ShouldReturnDefaultOptions()
    {
        var args = Array.Empty<string>();

        var options = AppOptionsParser.Parse<AppOptions>(args);

        Assert.NotNull(options);
        Assert.Equal("gpt-oss:20b", options.model);
    }
}