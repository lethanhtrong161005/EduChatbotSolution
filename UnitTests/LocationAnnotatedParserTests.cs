using Business.Services.AI.Indexing.Parsing;
using Domain.Entities;
using System.Text;

namespace UnitTests;

public class LocationAnnotatedParserTests
{
    [Test]
    public async Task ParseAsync_ReadsTxtWithoutDisposingCallerOwnedStream()
    {
        var parser = new LocationAnnotatedParser();
        await using var source = new MemoryStream(Encoding.UTF8.GetBytes("hello parser"));

        var result = await parser.ParseAsync(source, DocumentType.TXT);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Sections.Single().Text, Is.EqualTo("hello parser"));
            Assert.That(source.CanRead, Is.True);
        }
    }

    [Test]
    public void ParseAsync_RejectsStreamThatIsNotPositionedAtZero()
    {
        var parser = new LocationAnnotatedParser();
        using var source = new MemoryStream([1, 2, 3]);
        source.Position = 1;

        Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync(source, DocumentType.TXT));
    }
}
