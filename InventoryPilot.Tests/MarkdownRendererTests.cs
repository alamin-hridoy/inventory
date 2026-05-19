using InventoryPilot.Services;

namespace InventoryPilot.Tests;

public class MarkdownRendererTests
{
    private readonly MarkdownRenderer _renderer = new();

    [Fact]
    public void Render_ConvertsMarkdownToHtml()
    {
        var html = _renderer.Render("**Important** item");

        Assert.Contains("<strong>Important</strong>", html);
    }

    [Fact]
    public void Render_DisablesRawHtml()
    {
        var html = _renderer.Render("<script>alert('xss')</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script", html);
    }

    [Fact]
    public void Render_WithBlankInput_ReturnsEmptyString()
    {
        var html = _renderer.Render(" ");

        Assert.Equal(string.Empty, html);
    }
}
