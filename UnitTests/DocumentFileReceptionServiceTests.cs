using Business.Services.Documents.File;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Moq;
using System.Text;

namespace UnitTests;

public class DocumentFileReceptionServiceTests
{
    private Mock<IDocumentFileValidator> _validator = null!;
    private Mock<IStagingFileStore> _stagingStore = null!;
    private Mock<ILocalFileBuffer> _buffer = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new Mock<IDocumentFileValidator>();
        _stagingStore = new Mock<IStagingFileStore>();
        _buffer = new Mock<ILocalFileBuffer>();
    }

    [Test]
    public async Task ReceiveAsync_ValidatesAndResetsSeekableInputBeforeStaging()
    {
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes("prefix-content"));
        input.Position = 7;
        _validator.Setup(x => x.ValidateAsync(input, "source.pdf", It.IsAny<CancellationToken>()))
            .Callback(() => input.Position = input.Length)
            .ReturnsAsync(new FileValidationResult { Success = true, FileType = DocumentType.PDF });
        _stagingStore.Setup(x => x.StageAsync(input, ".pdf", It.IsAny<CancellationToken>()))
            .Callback(() => Assert.That(input.Position, Is.EqualTo(7)))
            .ReturnsAsync(new FileLocatorResult { Success = true, Locator = "opaque/staging" });

        var result = await CreateService().ReceiveAsync(input, "source.pdf");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StagingLocator, Is.EqualTo("opaque/staging"));
            Assert.That(result.FileType, Is.EqualTo(DocumentType.PDF));
            Assert.That(input.Position, Is.EqualTo(7));
        }
        _buffer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ReceiveAsync_RejectsInvalidSeekableInputBeforeStaging()
    {
        await using var input = new MemoryStream([1, 2, 3]);
        _validator.Setup(x => x.ValidateAsync(input, "source.exe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult { Success = false, Errors = ["invalid"] });

        var result = await CreateService().ReceiveAsync(input, "source.exe");

        Assert.That(result.Success, Is.False);
        _stagingStore.Verify(x => x.StageAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ReceiveAsync_BuffersNonSeekableInputOnceAndDisposesLease()
    {
        await using var input = new NonSeekableReadStream(Encoding.UTF8.GetBytes("content"));
        var lease = new Mock<ILocalFileLease>();
        lease.SetupSequence(x => x.OpenRead())
            .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("content")))
            .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("content")));
        lease.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _buffer.Setup(x => x.CopyFromAsync(input, ".pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease.Object);
        _validator.Setup(x => x.ValidateAsync(
                It.IsAny<Stream>(), "source.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult { Success = true, FileType = DocumentType.PDF });
        _stagingStore.Setup(x => x.StageAsync(
                It.IsAny<Stream>(), ".pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileLocatorResult { Success = true, Locator = "opaque/staging" });

        var result = await CreateService().ReceiveAsync(input, "source.pdf");

        Assert.That(result.Success, Is.True);
        _buffer.Verify(x => x.CopyFromAsync(input, ".pdf", It.IsAny<CancellationToken>()), Times.Once);
        lease.Verify(x => x.OpenRead(), Times.Exactly(2));
        lease.Verify(x => x.DisposeAsync(), Times.Once);
    }

    private DocumentFileReceptionService CreateService() => new(
        _validator.Object,
        _stagingStore.Object,
        _buffer.Object);

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
