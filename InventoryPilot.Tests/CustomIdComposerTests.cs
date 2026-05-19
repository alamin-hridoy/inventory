using InventoryPilot.Models;
using InventoryPilot.Services;

namespace InventoryPilot.Tests;

public class CustomIdComposerTests
{
    private readonly CustomIdComposer _composer = new();

    [Fact]
    public void BuildPreview_WithNoElements_UsesDefaultItemId()
    {
        var result = _composer.BuildPreview([]);

        Assert.Equal("ITEM-000013", result);
    }

    [Fact]
    public void Generate_WithNoElements_UsesDefaultItemIdWithSequence()
    {
        var result = _composer.Generate([], 42);

        Assert.Equal("ITEM-000042", result);
    }

    [Fact]
    public void BuildPreview_RendersElementsInSortOrder()
    {
        var elements = new[]
        {
            new InventoryCustomIdElement
            {
                ElementType = CustomIdElementTypes.Sequence,
                Format = "D4",
                SortOrder = 2
            },
            new InventoryCustomIdElement
            {
                ElementType = CustomIdElementTypes.Fixed,
                FixedText = "COMP-",
                SortOrder = 0
            },
            new InventoryCustomIdElement
            {
                ElementType = CustomIdElementTypes.DateTime,
                Format = "yyyy-",
                SortOrder = 1
            }
        };

        var result = _composer.BuildPreview(elements, sequence: 7);

        Assert.Equal("COMP-2025-0007", result);
    }

    [Theory]
    [InlineData("COMP-2025-0007", true)]
    [InlineData("COMP-2025-7", false)]
    [InlineData("OTHER-2025-0007", false)]
    public void IsValid_ValidatesAgainstFixedDateAndSequenceFormat(string customId, bool expected)
    {
        var elements = new[]
        {
            new InventoryCustomIdElement { ElementType = CustomIdElementTypes.Fixed, FixedText = "COMP-", SortOrder = 0 },
            new InventoryCustomIdElement { ElementType = CustomIdElementTypes.DateTime, Format = "yyyy-", SortOrder = 1 },
            new InventoryCustomIdElement { ElementType = CustomIdElementTypes.Sequence, Format = "D4", SortOrder = 2 }
        };

        var result = _composer.IsValid(customId, elements);

        Assert.Equal(expected, result);
    }
}
