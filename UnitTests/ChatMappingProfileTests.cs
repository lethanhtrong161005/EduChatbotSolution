using AutoMapper;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Presentation.Mappings;

namespace UnitTests;

public class ChatMappingProfileTests
{
    [Test]
    public void Citation_MapsToResolvedCitationForVariantPreview()
    {
        var configuration = new MapperConfiguration(
            config => config.AddProfile<ChatMappingProfile>(),
            NullLoggerFactory.Instance);
        var mapper = configuration.CreateMapper();
        var documentId = Guid.NewGuid();
        var citation = new Citation
        {
            ChunkId = Guid.NewGuid(),
            CitationIndex = 2,
            SimilarityScore = 0.75,
            LocationInDocument = "page 3",
            Chunk = new Chunk
            {
                ChunkIndex = 4,
                ChunkText = "Grounded text",
                DocumentId = documentId,
                Document = new Document { Id = documentId, Title = "Database Systems" },
            },
        };

        var result = mapper.Map<ResolvedCitation>(citation);

        Assert.Multiple(() =>
        {
            Assert.That(result.ChunkIndex, Is.EqualTo(4));
            Assert.That(result.ChunkText, Is.EqualTo("Grounded text"));
            Assert.That(result.DocumentId, Is.EqualTo(documentId));
            Assert.That(result.DocumentTitle, Is.EqualTo("Database Systems"));
        });
    }
}
