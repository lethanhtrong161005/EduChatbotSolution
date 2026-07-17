using Business.Services.AI.Indexing.Chunking;
using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;

namespace UnitTests;

[TestFixture]
public sealed class DocumentChunkerTests
{
    private const string SectionSeparator = "\n";

    [Test]
    public void AllChunkers_EmptyOrWhitespaceOnlySections_ReturnNoChunks()
    {
        var sections = new[]
        {
            Section(1, string.Empty, page: 1, title: "Empty"),
            Section(2, "   \r\n\t", page: 2, title: "Whitespace"),
        };

        foreach (var chunker in CreateAllChunkers())
        {
            var result = chunker.Chunk(sections, new ChunkingOptions(100, 20));

            Assert.That(result, Is.Empty, $"{chunker.StrategyName} emitted a chunk for empty input.");
        }
    }

    [Test]
    public void AllChunkers_InvalidOptions_ThrowArgumentOutOfRangeException()
    {
        var sections = new[]
        {
            Section(1, "Some content.", page: 1, title: "Content"),
        };

        var invalidOptions = new[]
        {
            new ChunkingOptions(0, 0),
            new ChunkingOptions(-1, 0),
            new ChunkingOptions(10, -1),
            new ChunkingOptions(10, 10),
            new ChunkingOptions(10, 11),
        };

        foreach (var chunker in CreateAllChunkers())
        {
            foreach (var options in invalidOptions)
            {
                Assert.That(
                    () => chunker.Chunk(sections, options),
                    Throws.InstanceOf<ArgumentOutOfRangeException>(),
                    $"{chunker.StrategyName} accepted size={options.ChunkSize}, overlap={options.ChunkOverlap}.");
            }
        }
    }

    [Test]
    public void AllChunkers_DefaultIndex_StartsAtOne()
    {
        foreach (var chunker in CreateAllChunkers())
        {
            var result = chunker.Chunk([Section(1, "Some content.")], new ChunkingOptions(100, 20));

            Assert.That(result.Single().ChunkIndex, Is.EqualTo(1), chunker.StrategyName);
        }
    }

    [Test]
    public void AllChunkers_StartIndexBelowOne_ThrowsArgumentOutOfRangeException()
    {
        foreach (var chunker in CreateAllChunkers())
            Assert.That(() => chunker.Chunk([Section(1, "Some content.")], new ChunkingOptions(100, 20), 0), Throws.TypeOf<ArgumentOutOfRangeException>(), chunker.StrategyName);
    }

    [Test]
    public void AllChunkers_NonContiguousSections_ThrowInvalidOperationException()
    {
        var sections = new[] { Section(1, "First"), Section(3, "Third") };

        foreach (var chunker in CreateAllChunkers())
            Assert.That(() => chunker.Chunk(sections, new ChunkingOptions(100, 20)), Throws.TypeOf<InvalidOperationException>(), chunker.StrategyName);
    }

    [Test]
    public void AllChunkers_ProducedChunks_RespectCommonInvariants()
    {
        var sections = new[]
        {
            Section(1, "Alpha sentence. Beta sentence.\n\nGamma paragraph.", page: 10, title: "Alpha"),
            Section(2, "Delta sentence. Epsilon sentence.", page: 11, title: "Delta"),
        };

        const int chunkSize = 32;

        foreach (var chunker in CreateAllChunkers())
        {
            var result = chunker.Chunk(sections, new ChunkingOptions(chunkSize, 8), startIndex: 5).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Empty);
                Assert.That(result.Select(x => x.ChunkIndex), Is.EqualTo(Enumerable.Range(5, result.Count)));
                Assert.That(result.All(x => !string.IsNullOrWhiteSpace(x.ChunkText)), Is.True);
                Assert.That(result.All(x => x.ChunkText.Length <= chunkSize), Is.True);
                Assert.That(result.All(x => x.StartPageNumber.HasValue == x.EndPageNumber.HasValue), Is.True);
                Assert.That(result.All(x => (x.StartSectionTitle is null) == (x.EndSectionTitle is null)), Is.True);
                Assert.That(result.All(x => !x.StartPageNumber.HasValue || x.EndPageNumber >= x.StartPageNumber), Is.True);
            }
        }
    }

    [Test]
    public void FixedLength_UsesExactLengthAndOverlap()
    {
        var chunker = new FixedLengthChunker();
        var section = Section(1, "abcdefghij", page: 7, title: "Alphabet");

        var result = chunker.Chunk([section], new ChunkingOptions(4, 1), startIndex: 5);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Select(x => x.ChunkText), Is.EqualTo(new[] { "abcd", "defg", "ghij", }));
            Assert.That(result.Select(x => x.ChunkIndex), Is.EqualTo(new[] { 5, 6, 7 }));
            Assert.That(result.All(x => x.StartPageNumber == 7 && x.EndPageNumber == 7), Is.True);
            Assert.That(result.All(x => x.StartSectionTitle == "Alphabet" && x.EndSectionTitle == "Alphabet"), Is.True);
        }
    }

    [Test]
    public void FixedLength_OrdersSectionsAndJoinsWithOneSoftNewline()
    {
        var chunker = new FixedLengthChunker();

        var result = chunker.Chunk(
            [
                Section(2, "BBBB", page: 2, title: "Second"),
                Section(1, "AAAA", page: 1, title: "First"),
            ],
            new ChunkingOptions(100, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].ChunkText, Is.EqualTo($"AAAA{SectionSeparator}BBBB"));
            Assert.That(result[0].StartPageNumber, Is.EqualTo(1));
            Assert.That(result[0].EndPageNumber, Is.EqualTo(2));
            Assert.That(result[0].StartSectionTitle, Is.EqualTo("First"));
            Assert.That(result[0].EndSectionTitle, Is.EqualTo("Second"));
        }
    }

    [Test]
    public void FixedLength_ChunkSpanningPageBoundary_RecordsBothLocations()
    {
        var chunker = new FixedLengthChunker();

        var result = chunker.Chunk(
            [
                Section(1, "AAAA", page: 1, title: "Page One"),
                Section(2, "BBBB", page: 2, title: "Page Two"),
            ],
            new ChunkingOptions(6, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Select(x => x.ChunkText), Is.EqualTo(new[] { $"AAAA{SectionSeparator}B", "BBB", }));

            Assert.That(result[0].StartPageNumber, Is.EqualTo(1));
            Assert.That(result[0].EndPageNumber, Is.EqualTo(2));
            Assert.That(result[0].StartSectionTitle, Is.EqualTo("Page One"));
            Assert.That(result[0].EndSectionTitle, Is.EqualTo("Page Two"));

            Assert.That(result[1].StartPageNumber, Is.EqualTo(2));
            Assert.That(result[1].EndPageNumber, Is.EqualTo(2));
            Assert.That(result[1].StartSectionTitle, Is.EqualTo("Page Two"));
            Assert.That(result[1].EndSectionTitle, Is.EqualTo("Page Two"));
        }
    }

    [Test]
    public void FixedLength_ChunkStartingOnSoftSeparator_UsesFirstRealSourceLocation()
    {
        var chunker = new FixedLengthChunker();

        var result = chunker.Chunk(
            [
                Section(1, "AAAA", page: 1, title: "Page One"),
                Section(2, "BBBB", page: 2, title: "Page Two"),
            ],
            new ChunkingOptions(6, 2));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(2));

            // The second window begins at the synthetic newline:
            // "AAAA\nBBBB"
            //      ^ start
            Assert.That(result[1].ChunkText, Is.EqualTo($"{SectionSeparator}BBBB"));
            Assert.That(result[1].StartPageNumber, Is.EqualTo(2));
            Assert.That(result[1].EndPageNumber, Is.EqualTo(2));
            Assert.That(result[1].StartSectionTitle, Is.EqualTo("Page Two"));
            Assert.That(result[1].EndSectionTitle, Is.EqualTo("Page Two"));
        }
    }

    [Test]
    public void FixedLength_WhitespaceOnlyIntermediateSection_IsIgnored()
    {
        var chunker = new FixedLengthChunker();

        var result = chunker.Chunk(
            [
                Section(1, "Alpha", page: 1, title: "Alpha"),
                Section(2, " \r\n\t ", page: 2, title: "Empty"),
                Section(3, "Beta", page: 3, title: "Beta"),
            ],
            new ChunkingOptions(100, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].ChunkText, Is.EqualTo($"Alpha{SectionSeparator}Beta"));
            Assert.That(result[0].StartPageNumber, Is.EqualTo(1));
            Assert.That(result[0].EndPageNumber, Is.EqualTo(3));
            Assert.That(result[0].StartSectionTitle, Is.EqualTo("Alpha"));
            Assert.That(result[0].EndSectionTitle, Is.EqualTo("Beta"));
        }
    }

    [Test]
    public void RecursiveSeparator_PrefersParagraphBoundaryOverLaterSentenceBoundary()
    {
        var chunker = new RecursiveSeparatorChunker();

        var result = chunker.Chunk(
            [
                Section(1, "Alpha paragraph.\n\nBeta sentence. Gamma sentence.", page: 1, title: "Paragraphs"),
            ],
            new ChunkingOptions(33, 0));

        Assert.That(
            result.Select(e => e.ChunkText),
            Is.EqualTo(new[]
            {
                "Alpha paragraph.",
                "Beta sentence. Gamma sentence.",
            }));
    }

    [Test]
    public void RecursiveSeparator_PrefersLineBoundaryOverHardCut()
    {
        var chunker = new RecursiveSeparatorChunker();

        var result = chunker.Chunk(
            [
                Section(1, "alpha beta gamma\ndelta epsilon zeta", page: 1, title: "Lines"),
            ],
            new ChunkingOptions(18, 0));

        Assert.That(
            result.Select(x => x.ChunkText.Trim()),
            Is.EqualTo(new[]
            {
                "alpha beta gamma",
                "delta epsilon zeta",
            }));
    }

    [Test]
    public void RecursiveSeparator_UnbrokenText_UsesFixedFallbackWithOverlap()
    {
        var chunker = new RecursiveSeparatorChunker();

        var result = chunker.Chunk(
            [
                Section(1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", page: 1, title: "Token"),
            ],
            new ChunkingOptions(10, 2));

        Assert.That(
            result.Select(x => x.ChunkText),
            Is.EqualTo(new[]
            {
                "ABCDEFGHIJ",
                "IJKLMNOPQR",
                "QRSTUVWXYZ",
            }));
    }

    [Test]
    public void RecursiveSeparator_CanCombineAdjacentSourceSections()
    {
        var chunker = new RecursiveSeparatorChunker();

        var result = chunker.Chunk(
            [
                Section(1, "Alpha.", page: 4, title: "Alpha"),
                Section(2, "Beta.", page: 5, title: "Beta"),
            ],
            new ChunkingOptions(20, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].ChunkText, Is.EqualTo($"Alpha.{SectionSeparator}Beta."));
            Assert.That(result[0].StartPageNumber, Is.EqualTo(4));
            Assert.That(result[0].EndPageNumber, Is.EqualTo(5));
            Assert.That(result[0].StartSectionTitle, Is.EqualTo("Alpha"));
            Assert.That(result[0].EndSectionTitle, Is.EqualTo("Beta"));
        }
    }

    [Test]
    public void SentenceParagraph_TextWithinChunkSize_ReturnsExactSource()
    {
        const string source = "  Một câu ngắn.  ";
        var result = new SentenceParagraphChunker().Chunk([Section(1, source, page: 1)], new ChunkingOptions(100, 20));

        Assert.That(result.Select(e => e.ChunkText), Is.EqualTo(new[] { source }));
    }

    [Test]
    public void SentenceParagraph_PacksWholeSentencesGreedily()
    {
        var chunker = new SentenceParagraphChunker();

        var result = chunker.Chunk(
            [
                Section(1, "First sentence. Second sentence. Third sentence.", page: 1, title: "Sentences"),
            ],
            new ChunkingOptions(32, 0));

        Assert.That(
            result.Select(x => x.ChunkText.Trim()),
            Is.EqualTo(new[]
            {
                "First sentence. Second sentence.",
                "Third sentence.",
            }));
    }

    [Test]
    public void SentenceParagraph_PreservesVietnameseSentenceBoundaries()
    {
        var chunker = new SentenceParagraphChunker();

        var sentences = new[]
        {
            "Một giao dịch là nguyên tử.",
            "Dữ liệu phải nhất quán!",
            "Có bị cô lập không?",
            "Có tính bền vững…",
        };

        var result = chunker.Chunk(
            [
                Section(1, string.Join(' ', sentences), page: 1, title: "ACID"),
            ],
            new ChunkingOptions(50, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.All(x => x.ChunkText.Length <= 50), Is.True);

            foreach (var sentence in sentences)
            {
                Assert.That(
                    result.Count(x => x.ChunkText.Contains(sentence, StringComparison.Ordinal)),
                    Is.EqualTo(1),
                    $"Sentence was split or duplicated: {sentence}");
            }
        }
    }

    [Test]
    public void SentenceParagraph_PageBoundaryInsideSentence_DoesNotForceSplit()
    {
        var chunker = new SentenceParagraphChunker();

        var result = chunker.Chunk(
            [
                Section(1, "Một giao dịch phải đảm bảo tính", page: 8, title: "Transactions"),
                Section(2, "nguyên tử và nhất quán.", page: 9, title: "Transactions Continued"),
            ],
            new ChunkingOptions(100, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].ChunkText, Is.EqualTo($"Một giao dịch phải đảm bảo tính{SectionSeparator}nguyên tử và nhất quán."));
            Assert.That(result[0].StartPageNumber, Is.EqualTo(8));
            Assert.That(result[0].EndPageNumber, Is.EqualTo(9));
            Assert.That(result[0].StartSectionTitle, Is.EqualTo("Transactions"));
            Assert.That(result[0].EndSectionTitle, Is.EqualTo("Transactions Continued"));
        }
    }

    [Test]
    public void SentenceParagraph_OversizedSentence_UsesBoundedFallback()
    {
        var chunker = new SentenceParagraphChunker();
        const string source = "ABCDEFGHIJKLMNOPQRSTUVWXYZ.";

        var result = chunker.Chunk(
            [
                Section(1, source, page: 1, title: "Oversized"),
            ],
            new ChunkingOptions(10, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.All(x => x.ChunkText.Length <= 10), Is.True);
            Assert.That(string.Concat(result.Select(x => x.ChunkText)), Is.EqualTo(source));
        }
    }

    [TestCase(5)]
    [TestCase(1)]
    public void SentenceParagraph_OverlapCarriesWholeSentences(int overlap)
    {
        var chunker = new SentenceParagraphChunker();
        var sentences = new[]
        {
            "One.",
            "Two.",
            "Three.",
            "Four.",
        };

        var result = chunker.Chunk(
            [
                Section(1, string.Join(' ', sentences), page: 1, title: "Numbers"),
            ],
            new ChunkingOptions(12, overlap));

        Assert.That(result, Has.Count.GreaterThan(1));

        for (var i = 1; i < result.Count; i++)
        {
            var previous = result[i - 1].ChunkText;
            var current = result[i].ChunkText;

            var sharesWholeSentence = sentences.Any(sentence =>
                previous.Contains(sentence, StringComparison.Ordinal) &&
                current.Contains(sentence, StringComparison.Ordinal));

            Assert.That(sharesWholeSentence, Is.True, $"Chunks {i - 1} and {i} do not share a whole sentence.");
        }
    }

    [Test]
    public void ChunkerSelector_SelectsExactRegisteredStrategy()
    {
        var fixedLength = new FixedLengthChunker();
        var recursive = new RecursiveSeparatorChunker();
        var sentenceParagraph = new SentenceParagraphChunker();

        var selector = new DocumentChunkerSelector(
            [
                fixedLength,
                recursive,
                sentenceParagraph,
            ]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(selector.Select(ChunkingStrategy.FixedLength), Is.SameAs(fixedLength));
            Assert.That(selector.Select(ChunkingStrategy.RecursiveSeparator), Is.SameAs(recursive));
            Assert.That(selector.Select(ChunkingStrategy.SentenceParagraph), Is.SameAs(sentenceParagraph));
        }
    }

    [Test]
    public void ChunkerSelector_StrategyMatching_IsCaseSensitive()
    {
        var selector = new DocumentChunkerSelector(
            [
                new FixedLengthChunker(),
                new RecursiveSeparatorChunker(),
                new SentenceParagraphChunker(),
            ]);

        Assert.That(
            () => selector.Select("fixedlength"),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void ChunkerSelector_UnknownStrategy_ThrowsInvalidOperationException()
    {
        var selector = new DocumentChunkerSelector(
            [
                new FixedLengthChunker(),
                new RecursiveSeparatorChunker(),
                new SentenceParagraphChunker(),
            ]);

        Assert.That(
            () => selector.Select("UnknownStrategy"),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void ChunkerSelector_DuplicateRegistration_FailsFast()
    {
        Assert.That(
            () => new DocumentChunkerSelector(
            [
                new FixedLengthChunker(),
                new FixedLengthChunker(),
            ]),
            Throws.InstanceOf<InvalidOperationException>());
    }

    private static IDocumentChunker[] CreateAllChunkers() =>
    [
        new FixedLengthChunker(),
        new RecursiveSeparatorChunker(),
        new SentenceParagraphChunker(),
    ];

    private static ParsedSection Section(
        int index,
        string text,
        int? page = null,
        string? title = null)
    {
        return new ParsedSection
        {
            SectionIndex = index,
            PageNumber = page,
            SectionTitle = title,
            Text = text,
        };
    }
}
