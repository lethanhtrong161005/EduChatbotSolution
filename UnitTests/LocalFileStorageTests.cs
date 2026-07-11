using Business.Services.Documents.File;
using Domain.Contracts;
using Microsoft.Extensions.Options;
using System.Text;

namespace UnitTests;

public class LocalFileStorageTests
{
    private string _appDirectory = null!;
    private FileStorageOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _appDirectory = $"EduChatAI-Tests-{Guid.NewGuid()}";
        _options = new FileStorageOptions
        {
            AppDirectory = _appDirectory,
            FileDirectoryBuffer = "buffer",
            FileDirectoryStaging = "staging",
            FileDirectoryReceived = "uploaded",
            FileDirectoryProcessing = "processing",
            FileDirectoryIndexed = "indexed",
            FileDirectoryFailed = "failed",
        };
    }

    [TearDown]
    public void TearDown()
    {
        TryDeleteDirectory(Path.Combine(Path.GetTempPath(), _appDirectory));
        TryDeleteDirectory(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            _appDirectory));
    }

    [Test]
    public async Task BufferLease_SyncAndAsyncDisposalAreIdempotent()
    {
        var buffer = new LocalFileBuffer(Options.Create(_options));
        var lease = await buffer.CopyFromAsync(new MemoryStream([1, 2, 3]), ".pdf");
        var path = lease.FilePath;

        lease.Dispose();
        await lease.DisposeAsync();
        lease.Dispose();

        Assert.That(System.IO.File.Exists(path), Is.False);
    }

    [Test]
    public async Task BufferLease_TransferredStreamOwnsCleanup()
    {
        var buffer = new LocalFileBuffer(Options.Create(_options));
        var lease = await buffer.CopyFromAsync(new MemoryStream([1, 2, 3]), ".pdf");
        var path = lease.FilePath;

        var stream = lease.OpenReadAndTransferOwnership();
        await lease.DisposeAsync();
        Assert.That(stream.ReadByte(), Is.EqualTo(1));

        await stream.DisposeAsync();
        Assert.That(System.IO.File.Exists(path), Is.False);
    }

    [Test]
    public async Task StagingStore_StagesReadsChecksAndDeletesOpaqueLocator()
    {
        var store = new LocalStagingFileStore(Options.Create(_options));
        var stageResult = await store.StageAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("staged")), ".txt");

        Assert.That(stageResult.Success, Is.True);
        Assert.That(await store.ExistsAsync(stageResult.Locator!), Is.True);
        var readResult = await store.OpenReadAsync(stageResult.Locator!);
        await using (readResult.FileStream)
        using (var reader = new StreamReader(readResult.FileStream!, leaveOpen: true))
            Assert.That(await reader.ReadToEndAsync(), Is.EqualTo("staged"));

        var deleteResult = await store.DeleteAsync(stageResult.Locator!);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleteResult.Success, Is.True);
            Assert.That(await store.ExistsAsync(stageResult.Locator!), Is.False);
        }
    }

    [Test]
    public async Task StagingStore_RejectsLocatorOutsideItsRoot()
    {
        var store = new LocalStagingFileStore(Options.Create(_options));
        var outside = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await store.ExistsAsync(outside), Is.False);
            Assert.That((await store.OpenReadAsync(outside)).Success, Is.False);
            Assert.That((await store.DeleteAsync(outside)).Success, Is.False);
        }
    }

    [Test]
    public async Task DurableLocalStore_OverwritesMovesReadsAndReturnsTypedDeletion()
    {
        var strategy = new LocalHardDriveDurableStorageStrategy(Options.Create(_options));
        var first = await strategy.StoreAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("first")), "fixed.pdf", DocumentFileDirectory.Received);
        var second = await strategy.StoreAsync(
            new MemoryStream(Encoding.UTF8.GetBytes("second")), "fixed.pdf", DocumentFileDirectory.Received);

        Assert.That(second.Locator, Is.EqualTo(first.Locator));
        var moved = await strategy.MoveAsync(second.Locator!, DocumentFileDirectory.Indexed);
        Assert.That(Path.GetFileName(moved.Locator), Is.EqualTo("fixed.pdf"));

        var read = await strategy.OpenReadAsync(moved.Locator!);
        await using (read.FileStream)
        using (var reader = new StreamReader(read.FileStream!, leaveOpen: true))
            Assert.That(await reader.ReadToEndAsync(), Is.EqualTo("second"));

        var deleted = await strategy.DeleteAsync(moved.Locator!);
        Assert.That(deleted.Success, Is.True);
        Assert.That(await strategy.ExistsAsync(moved.Locator!), Is.False);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
