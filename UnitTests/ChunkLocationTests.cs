using Domain.Entities;
using Domain.Utils;

namespace UnitTests;

[TestFixture]
public sealed class ChunkLocationTests
{
    [Test]
    public void BuildLocation_NoLocationMetadata_ReturnsNull()
    {
        var chunk = new Chunk();

        var result = chunk.BuildLocation();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void BuildLocation_SamePageAndSection_FormatsSingleLocation()
    {
        var chunk = new Chunk
        {
            StartPageNumber = 4,
            EndPageNumber = 4,
            StartSectionTitle = "Normalization",
            EndSectionTitle = "Normalization",
        };

        var result = chunk.BuildLocation();

        Assert.That(result, Is.EqualTo("Page: 4 • Section: Normalization"));
    }

    [Test]
    public void BuildLocation_PageAndSectionRanges_FormatsBothRanges()
    {
        var chunk = new Chunk
        {
            StartPageNumber = 4,
            EndPageNumber = 6,
            StartSectionTitle = "Normalization",
            EndSectionTitle = "Functional Dependencies",
        };

        var result = chunk.BuildLocation();

        Assert.That(result, Is.EqualTo("Page: 4–6 • Section: Normalization → Functional Dependencies"));
    }

    [Test]
    public void BuildLocation_PageRangeWithoutSection_FormatsOnlyPages()
    {
        var chunk = new Chunk
        {
            StartPageNumber = 2,
            EndPageNumber = 3,
        };

        var result = chunk.BuildLocation();

        Assert.That(result, Is.EqualTo("Page: 2–3"));
    }

    [Test]
    public void BuildLocation_SectionRangeWithoutPage_FormatsOnlySections()
    {
        var chunk = new Chunk
        {
            StartSectionTitle = "Transactions",
            EndSectionTitle = "Concurrency Control",
        };

        var result = chunk.BuildLocation();

        Assert.That(result, Is.EqualTo("Section: Transactions → Concurrency Control"));
    }

    [Test]
    public void BuildLocation_UntitledStartAndTitledEnd_UsesKnownEndTitle()
    {
        var chunk = new Chunk { StartSectionTitle = "", EndSectionTitle = "Normalization" };

        Assert.That(chunk.BuildLocation(), Is.EqualTo("Section: Normalization"));
    }

    [Test]
    public void BuildLocation_EndEqualsStart_DoesNotRenderRedundantRange()
    {
        var chunk = new Chunk
        {
            StartPageNumber = 7,
            EndPageNumber = 7,
            StartSectionTitle = "Indexes",
            EndSectionTitle = "Indexes",
        };

        var result = chunk.BuildLocation();

        Assert.That(result, Is.EqualTo("Page: 7 • Section: Indexes"));
    }

    [Test]
    public void BuildLocation_ParsedSection_UsesSameValueForBothEnds()
    {
        var section = new ParsedSection
        {
            SectionIndex = 1,
            PageNumber = 5,
            SectionTitle = "Relational Algebra",
            Text = "Selection and projection are relational operations.",
        };

        var result = section.BuildLocation();

        Assert.That(result, Is.EqualTo("Page: 5 • Section: Relational Algebra"));
    }
}
