using AzureRagLibrarian.Services;
using Xunit;

namespace AzureRagLibrarian.Tests;

public sealed class RagChatSessionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("exit")]
    [InlineData("EXIT")]
    public void IsExitCommand_WhenInputEndsSession_ReturnsTrue(string? input)
    {
        Assert.True(RagChatSession.IsExitCommand(input));
    }

    [Fact]
    public void IsExitCommand_WhenInputIsAQuestion_ReturnsFalse()
    {
        Assert.False(RagChatSession.IsExitCommand("When do Quiet Hours start?"));
    }
}
