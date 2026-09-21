using FluentAssertions;
using LoanApi.Storage;

namespace LoanApi.Test
{
    [TestFixture]
    public class StorageTests
    {
        private string _root = null!;
        private LocalFileStorage _storage = null!;

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "loanapi-storage-tests", Guid.NewGuid().ToString("N"));
            _storage = new LocalFileStorage(new LocalStorageOptions { RootPath = _root });
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        private static MemoryStream Content(string text) => new(System.Text.Encoding.UTF8.GetBytes(text));

        [Test]
        public async Task Save_ThenRead_ReturnsTheSameBytes()
        {
            byte[] bytes = [0x25, 0x50, 0x44, 0x46, 0x00, 0xFF, 0x10];

            string key = await _storage.SaveAsync(new MemoryStream(bytes), "pdf");
            await using Stream read = await _storage.OpenReadAsync(key);
            using MemoryStream copy = new();
            await read.CopyToAsync(copy);

            copy.ToArray().Should().Equal(bytes);
        }

        [Test]
        public async Task Save_GeneratesAKeyThatDoesNotDependOnTheFileName()
        {
            string first = await _storage.SaveAsync(Content("a"), "pdf");
            string second = await _storage.SaveAsync(Content("a"), "pdf");

            StorageKeys.IsValid(first).Should().BeTrue();
            first.Should().NotBe(second);
            first.Should().EndWith(".pdf");
        }

        [Test]
        public async Task Delete_RemovesTheFile_AndIsIdempotent()
        {
            string key = await _storage.SaveAsync(Content("x"), "png");

            await _storage.DeleteAsync(key);
            await _storage.DeleteAsync(key);

            Func<Task> act = () => _storage.OpenReadAsync(key);
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        [TestCase("../secret.pdf")]
        [TestCase("..\\..\\windows\\win.ini")]
        [TestCase("2026/09/../../../etc/passwd")]
        [TestCase("/etc/passwd")]
        [TestCase("C:\\windows\\win.ini")]
        [TestCase("2026/09/notaguid.pdf")]
        [TestCase("")]
        public async Task ForgedKeys_AreRejectedBeforeTouchingTheDisk(string key)
        {
            Func<Task> read = () => _storage.OpenReadAsync(key);
            Func<Task> delete = () => _storage.DeleteAsync(key);

            await read.Should().ThrowAsync<ArgumentException>();
            await delete.Should().ThrowAsync<ArgumentException>();
        }

        [TestCase("../pdf")]
        [TestCase("p/df")]
        [TestCase("toolongext")]
        [TestCase("")]
        public async Task Save_RejectsUnsafeExtensions(string extension)
        {
            Func<Task> act = () => _storage.SaveAsync(Content("x"), extension);

            await act.Should().ThrowAsync<ArgumentException>();
            Directory.Exists(_root).Should().BeFalse("nothing should have been written");
        }

        [Test]
        public async Task Save_NormalizesTheExtension()
        {
            string key = await _storage.SaveAsync(Content("x"), ".PDF");

            key.Should().EndWith(".pdf");
        }

        [Test]
        public async Task Check_LeavesNothingBehind()
        {
            await _storage.CheckAsync();

            Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }

        [Test]
        public async Task Check_OnAnUnwritableRoot_ExplainsWhy()
        {
            string file = Path.Combine(Path.GetTempPath(), $"loanapi-not-a-folder-{Guid.NewGuid():N}");
            await File.WriteAllTextAsync(file, "x");
            try
            {
                LocalFileStorage broken = new(new LocalStorageOptions { RootPath = file });

                Func<Task> act = () => broken.CheckAsync();

                await act.Should().ThrowAsync<StorageException>();
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void MissingRoot_IsAConfigurationError()
        {
            Action act = () => new LocalFileStorage(new LocalStorageOptions());

            act.Should().Throw<StorageException>();
        }

        [Test]
        public async Task Save_WhenTheSourceFailsHalfWay_DoesNotLeaveAPartialFile()
        {
            Func<Task> act = () => _storage.SaveAsync(new FailingStream(), "pdf");

            await act.Should().ThrowAsync<IOException>();
            Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }

        [TestCase("mystorage", true)]
        [TestCase("ab", false)]
        [TestCase("MyStorage", false)]
        [TestCase("my-storage", false)]
        [TestCase("abcdefghijklmnopqrstuvwxy", false)]
        [TestCase(null, false)]
        public void AccountNames_FollowAzureRules(string? name, bool valid)
        {
            StorageSelection.IsValidAccountName(name).Should().Be(valid);
        }

        [TestCase("documents", true)]
        [TestCase("loan-documents", true)]
        [TestCase("ab", false)]
        [TestCase("-documents", false)]
        [TestCase("documents-", false)]
        [TestCase("loan--docs", false)]
        [TestCase("Documents", false)]
        [TestCase(null, false)]
        public void ContainerNames_FollowAzureRules(string? name, bool valid)
        {
            StorageSelection.IsValidContainerName(name).Should().Be(valid);
        }

        [Test]
        public void ForNewFiles_UsesAzureOnlyWhenEnabledAndFullyConfigured()
        {
            StorageSelection.ForNewFiles(true, "mystorage", "documents").Should().Be(StorageProviderKind.Azure);
            StorageSelection.ForNewFiles(false, "mystorage", "documents").Should().Be(StorageProviderKind.Local);
            StorageSelection.ForNewFiles(true, null, "documents").Should().Be(StorageProviderKind.Local);
            StorageSelection.ForNewFiles(true, "mystorage", "Bad Name").Should().Be(StorageProviderKind.Local);
        }

        private sealed class FailingStream : Stream
        {
            private int _reads;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_reads++ > 0) throw new IOException("boom");
                buffer[offset] = 1;
                return 1;
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_reads++ > 0) throw new IOException("boom");
                buffer.Span[0] = 1;
                return ValueTask.FromResult(1);
            }
        }
    }
}
