using Application.Exceptions;
using Application.Features.Documents.Commands;
using Application.Features.Documents.Handlers;
using Application.Features.Documents.Queries;
using Application.Features.Storage;
using Application.Models.Responses;
using Application.Services;
using Dapper;
using Domain.Enums;
using FluentAssertions;
using LoanApi.Storage;
using LoanApi.Test.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoanApi.Test
{
    [TestFixture]
    public class DocumentStorageHandlerTests
    {
        private static readonly byte[] Pdf = [.. "%PDF-1.7\n1 0 obj\n"u8];

        private SqliteConnection _connection = null!;
        private string _root = null!;
        private StubbedFactory _factory = null!;
        private UploadDocumentCommandHandler _upload = null!;
        private GetDocumentFileQueryHandler _download = null!;
        private UpdateStorageSettingsCommandHandler _settings = null!;
        private Guid _loanId;

        [OneTimeSetUp]
        public void RegisterHandlers() => SqlMapper.AddTypeHandler(new SqliteGuidHandler());

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _connection.Execute(@"
                ATTACH DATABASE ':memory:' AS Loans;
                ATTACH DATABASE ':memory:' AS Admin;

                CREATE TABLE Loans.Loan (Id TEXT, IsDeleted INTEGER);
                CREATE TABLE Loans.LoanDocument (
                    Id TEXT, LoanId TEXT, DocumentType INTEGER, FileName TEXT, FileUrl TEXT, UploadedDate TEXT,
                    FileSizeBytes INTEGER, MimeType INTEGER, StorageProvider INTEGER, StorageKey TEXT,
                    IsActive INTEGER, IsDeleted INTEGER, CreatedOn TEXT);
                CREATE TABLE Admin.StorageSetting (
                    Id TEXT, AzureEnabled INTEGER, AzureAccountName TEXT, AzureContainer TEXT,
                    CreatedOn TEXT, UpdatedOn TEXT, IsActive INTEGER, IsDeleted INTEGER);");

            _loanId = Guid.NewGuid();
            _connection.Execute("INSERT INTO Loans.Loan VALUES (@Id, 0)", new { Id = _loanId });

            _root = Path.Combine(Path.GetTempPath(), "loanapi-doc-tests", Guid.NewGuid().ToString("N"));
            _factory = new StubbedFactory(new FileStorageFactory(
                _connection,
                new StorageConfiguration { Local = new LocalStorageOptions { RootPath = _root } }));

            _upload = new UploadDocumentCommandHandler(_connection, _factory, NullLogger<UploadDocumentCommandHandler>.Instance);
            _download = new GetDocumentFileQueryHandler(_connection, _factory);
            _settings = new UpdateStorageSettingsCommandHandler(_connection, _factory);
        }

        [TearDown]
        public void Cleanup()
        {
            _connection.Dispose();
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        private UploadDocumentCommand Upload(byte[] content, string fileName = "contrato.pdf", long? length = null) => new()
        {
            LoanId = _loanId,
            DocumentType = DocumentTypes.Contract,
            FileName = fileName,
            Length = length ?? content.Length,
            Content = new MemoryStream(content),
        };

        private int DocumentCount() => _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Loans.LoanDocument");

        private int StoredFileCount() =>
            Directory.Exists(_root) ? Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Count() : 0;

        // ---------------------------------------------------------------- upload

        [Test]
        public async Task Upload_StoresTheFileLocally_AndRecordsWhereItLives()
        {
            LoanDocumentResponse result = await _upload.Handle(Upload(Pdf), CancellationToken.None);

            result.StorageProvider.Should().Be(DocumentStorage.Local);
            result.MimeType.Should().Be(MimeTypes.Pdf);
            result.FileSizeBytes.Should().Be(Pdf.Length);
            result.FileUrl.Should().Be($"documents/Download/{result.Id}");

            string key = _connection.ExecuteScalar<string>("SELECT StorageKey FROM Loans.LoanDocument")!;
            StorageKeys.IsValid(key).Should().BeTrue();
            key.Should().EndWith(".pdf");
            StoredFileCount().Should().Be(1);
        }

        [Test]
        public async Task Upload_NeverUsesTheUsersFileNameOnDisk()
        {
            await _upload.Handle(Upload(Pdf, fileName: "  ../../evil.pdf ".Replace("../", "")), CancellationToken.None);

            Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Single().Should().NotContain("evil");
        }

        [Test]
        public async Task Upload_WhenAzureIsEnabledButIncomplete_FallsBackToLocal()
        {
            _connection.Execute("INSERT INTO Admin.StorageSetting VALUES (@Id, 1, NULL, NULL, '2026-01-01', NULL, 1, 0)", new { Id = Guid.NewGuid() });

            LoanDocumentResponse result = await _upload.Handle(Upload(Pdf), CancellationToken.None);

            result.StorageProvider.Should().Be(DocumentStorage.Local);
        }

        [Test]
        public async Task Upload_WhenAzureIsOn_UsesAzure()
        {
            _connection.Execute("INSERT INTO Admin.StorageSetting VALUES (@Id, 1, 'mystorage', 'documents', '2026-01-01', NULL, 1, 0)", new { Id = Guid.NewGuid() });
            FakeStorage azure = new(StorageProviderKind.Azure);
            _factory.AzureFactory = (_, _) => azure;

            LoanDocumentResponse result = await _upload.Handle(Upload(Pdf), CancellationToken.None);

            result.StorageProvider.Should().Be(DocumentStorage.Azure);
            azure.Saved.Should().HaveCount(1);
            StoredFileCount().Should().Be(0, "nothing goes to the local folder while Azure is active");
        }

        [Test]
        public void Upload_WithContentThatIsNotWhatTheExtensionSays_IsRejected()
        {
            byte[] exe = [0x4D, 0x5A, 0x90, 0x00, 0x03];

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _upload.Handle(Upload(exe), CancellationToken.None))!;

            exception.ErrorMessages.Should().Contain(m => m.Contains("does not match"));
            DocumentCount().Should().Be(0);
            StoredFileCount().Should().Be(0);
        }

        [TestCase("virus.exe")]
        [TestCase("noextension")]
        [TestCase("page.html")]
        public void Upload_WithADisallowedType_IsRejected(string fileName)
        {
            Assert.ThrowsAsync<ValidationException>(() => _upload.Handle(Upload(Pdf, fileName), CancellationToken.None));

            StoredFileCount().Should().Be(0);
        }

        [Test]
        public void Upload_OfAnEmptyFile_IsRejected()
        {
            Assert.ThrowsAsync<ValidationException>(() => _upload.Handle(Upload([], length: 0), CancellationToken.None));
        }

        [Test]
        public void Upload_LargerThanTheLimit_IsRejectedEvenWhenTheDeclaredSizeLies()
        {
            byte[] big = new byte[(int)Application.Features.Documents.DocumentRules.MaxFileBytes + 1];
            "%PDF-"u8.CopyTo(big);

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _upload.Handle(Upload(big, length: 10), CancellationToken.None))!;

            exception.ErrorMessages.Should().Contain(m => m.Contains("cannot exceed"));
            StoredFileCount().Should().Be(0);
        }

        [Test]
        public void Upload_ForAnUnknownLoan_IsNotFoundAndStoresNothing()
        {
            UploadDocumentCommand command = Upload(Pdf);
            command.LoanId = Guid.NewGuid();

            Assert.ThrowsAsync<NotFoundException>(() => _upload.Handle(command, CancellationToken.None));

            StoredFileCount().Should().Be(0);
        }

        [Test]
        public void Upload_WhenTheRowCannotBeSaved_RemovesTheStoredFile()
        {
            _connection.Execute("DROP TABLE Loans.LoanDocument");

            Assert.ThrowsAsync<SqliteException>(() => _upload.Handle(Upload(Pdf), CancellationToken.None));

            StoredFileCount().Should().Be(0, "an orphaned file could never be reached again");
        }

        [Test]
        public void Upload_WhenTheStorageFails_ReportsABadGateway()
        {
            _factory.NewFilesOverride = new FakeStorage(StorageProviderKind.Local) { FailOnSave = true };

            CustomException exception = Assert.ThrowsAsync<CustomException>(() => _upload.Handle(Upload(Pdf), CancellationToken.None))!;

            exception.StatusCode.Should().Be(System.Net.HttpStatusCode.BadGateway);
            DocumentCount().Should().Be(0);
        }

        // -------------------------------------------------------------- download

        [Test]
        public async Task Download_StreamsTheStoredBytesWithTheRightType()
        {
            LoanDocumentResponse saved = await _upload.Handle(Upload(Pdf, "Contrato Final.pdf"), CancellationToken.None);

            await using DocumentFileResult file = await _download.Handle(new GetDocumentFileQuery(saved.Id), CancellationToken.None);
            using MemoryStream copy = new();
            await file.Content.CopyToAsync(copy);

            copy.ToArray().Should().Equal(Pdf);
            file.ContentType.Should().Be("application/pdf");
            file.FileName.Should().Be("Contrato Final.pdf");
        }

        [Test]
        public async Task Download_OfAFileThatWasStoredInAzure_StillWorksAfterAzureIsSwitchedOff()
        {
            _connection.Execute("INSERT INTO Admin.StorageSetting VALUES (@Id, 1, 'mystorage', 'documents', '2026-01-01', NULL, 1, 0)", new { Id = Guid.NewGuid() });
            FakeStorage azure = new(StorageProviderKind.Azure);
            _factory.AzureFactory = (_, _) => azure;
            LoanDocumentResponse saved = await _upload.Handle(Upload(Pdf), CancellationToken.None);

            _connection.Execute("UPDATE Admin.StorageSetting SET AzureEnabled = 0");
            await using DocumentFileResult file = await _download.Handle(new GetDocumentFileQuery(saved.Id), CancellationToken.None);

            file.Should().NotBeNull();
            azure.Opened.Should().HaveCount(1);
        }

        [Test]
        public void Download_OfAnExternalReference_IsRefusedWithAnExplanation()
        {
            Guid id = Guid.NewGuid();
            _connection.Execute(
                @"INSERT INTO Loans.LoanDocument (Id, LoanId, FileName, MimeType, StorageProvider, StorageKey, IsDeleted)
                  VALUES (@Id, @LoanId, 'a.pdf', 1, 0, NULL, 0)",
                new { Id = id, LoanId = _loanId });

            ConflictException exception = Assert.ThrowsAsync<ConflictException>(() => _download.Handle(new GetDocumentFileQuery(id), CancellationToken.None))!;

            exception.Message.Should().Contain("external");
        }

        [Test]
        public async Task Download_WhenTheFileIsGoneFromTheStorage_IsNotFound()
        {
            LoanDocumentResponse saved = await _upload.Handle(Upload(Pdf), CancellationToken.None);
            foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)) File.Delete(file);

            Assert.ThrowsAsync<NotFoundException>(() => _download.Handle(new GetDocumentFileQuery(saved.Id), CancellationToken.None));
        }

        [Test]
        public async Task Download_OfADeletedDocument_IsNotFound()
        {
            LoanDocumentResponse saved = await _upload.Handle(Upload(Pdf), CancellationToken.None);
            _connection.Execute("UPDATE Loans.LoanDocument SET IsDeleted = 1");

            Assert.ThrowsAsync<NotFoundException>(() => _download.Handle(new GetDocumentFileQuery(saved.Id), CancellationToken.None));
        }

        [Test]
        public void Download_OfAnUnknownDocument_IsNotFound()
        {
            Assert.ThrowsAsync<NotFoundException>(() => _download.Handle(new GetDocumentFileQuery(Guid.NewGuid()), CancellationToken.None));
        }

        // -------------------------------------------------------------- settings

        [Test]
        public async Task Settings_StartOffAndLocal()
        {
            StorageSettingsResponse result = await new GetStorageSettingsQueryHandler(_factory)
                .Handle(new GetStorageSettingsQuery(), CancellationToken.None);

            result.AzureEnabled.Should().BeFalse();
            result.ActiveProvider.Should().Be("Local");
            result.AzureCredentialSource.Should().Be("ManagedIdentity");
        }

        [Test]
        public async Task Settings_EnablingAzure_ChecksItFirstAndThenSwitchesTheActiveProvider()
        {
            FakeStorage azure = new(StorageProviderKind.Azure);
            _factory.AzureFactory = (_, _) => azure;

            StorageSettingsResponse result = await _settings.Handle(
                new UpdateStorageSettingsCommand { AzureEnabled = true, AzureAccountName = " mystorage ", AzureContainer = "documents" },
                CancellationToken.None);

            azure.Checks.Should().Be(1);
            result.AzureEnabled.Should().BeTrue();
            result.AzureAccountName.Should().Be("mystorage");
            result.ActiveProvider.Should().Be("Azure");
        }

        [Test]
        public async Task Settings_WhenAzureFailsTheCheck_IsNotEnabled()
        {
            _factory.AzureFactory = (_, _) => new FakeStorage(StorageProviderKind.Azure) { FailOnCheck = true };

            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _settings.Handle(
                new UpdateStorageSettingsCommand { AzureEnabled = true, AzureAccountName = "mystorage", AzureContainer = "documents" },
                CancellationToken.None))!;

            exception.ErrorMessages.Should().ContainSingle();
            (await new GetStorageSettingsQueryHandler(_factory).Handle(new GetStorageSettingsQuery(), CancellationToken.None))
                .AzureEnabled.Should().BeFalse();
        }

        [Test]
        public async Task Settings_DisablingAzure_DoesNotNeedToReachIt()
        {
            _factory.AzureFactory = (_, _) => new FakeStorage(StorageProviderKind.Azure);
            await _settings.Handle(new UpdateStorageSettingsCommand { AzureEnabled = true, AzureAccountName = "mystorage", AzureContainer = "documents" }, CancellationToken.None);
            _factory.AzureFactory = (_, _) => throw new InvalidOperationException("must not be called");

            StorageSettingsResponse result = await _settings.Handle(
                new UpdateStorageSettingsCommand { AzureEnabled = false, AzureAccountName = "mystorage", AzureContainer = "documents" },
                CancellationToken.None);

            result.AzureEnabled.Should().BeFalse();
            result.ActiveProvider.Should().Be("Local");
            result.AzureAccountName.Should().Be("mystorage", "the account stays so existing Azure files remain readable");
        }

        [Test]
        public void Settings_WithInvalidNames_ListsEveryProblem()
        {
            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _settings.Handle(
                new UpdateStorageSettingsCommand { AzureEnabled = false, AzureAccountName = "My Storage!", AzureContainer = "X" },
                CancellationToken.None))!;

            exception.ErrorMessages.Should().HaveCount(2);
        }

        [Test]
        public void Settings_EnablingWithoutAccountOrContainer_IsRejected()
        {
            ValidationException exception = Assert.ThrowsAsync<ValidationException>(() => _settings.Handle(
                new UpdateStorageSettingsCommand { AzureEnabled = true },
                CancellationToken.None))!;

            exception.ErrorMessages.Should().Contain(m => m.Contains("required"));
        }

        // ---------------------------------------------------------------- doubles

        private sealed class StubbedFactory : IFileStorageFactory
        {
            private readonly FileStorageFactory _inner;

            public StubbedFactory(FileStorageFactory inner) => _inner = inner;

            public Func<string, string, IFileStorage>? AzureFactory { get; set; }
            public IFileStorage? NewFilesOverride { get; set; }

            public string AzureCredentialSource => _inner.AzureCredentialSource;
            public Task<StorageSettings> GetSettingsAsync(CancellationToken ct = default) => _inner.GetSettingsAsync(ct);
            public Task<StorageProviderKind> GetActiveKindAsync(CancellationToken ct = default) => _inner.GetActiveKindAsync(ct);
            public IFileStorage CreateAzure(string account, string container) =>
                AzureFactory?.Invoke(account, container) ?? _inner.CreateAzure(account, container);

            public async Task<IFileStorage> GetForNewFilesAsync(CancellationToken ct = default)
            {
                if (NewFilesOverride is not null) return NewFilesOverride;

                StorageSettings settings = await GetSettingsAsync(ct);
                return await GetActiveKindAsync(ct) == StorageProviderKind.Azure
                    ? CreateAzure(settings.AzureAccountName!, settings.AzureContainer!)
                    : await _inner.GetForNewFilesAsync(ct);
            }

            public async Task<IFileStorage> GetForExistingAsync(DocumentStorage provider, CancellationToken ct = default)
            {
                if (provider != DocumentStorage.Azure) return await _inner.GetForExistingAsync(provider, ct);

                StorageSettings settings = await GetSettingsAsync(ct);
                return CreateAzure(settings.AzureAccountName!, settings.AzureContainer!);
            }
        }

        private sealed class FakeStorage : IFileStorage
        {
            private readonly Dictionary<string, byte[]> _files = [];

            public FakeStorage(StorageProviderKind kind) => Kind = kind;

            public StorageProviderKind Kind { get; }
            public bool FailOnSave { get; set; }
            public bool FailOnCheck { get; set; }
            public int Checks { get; private set; }
            public List<string> Saved { get; } = [];
            public List<string> Opened { get; } = [];

            public Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
            {
                if (FailOnSave) throw new StorageException("The file could not be saved.");

                using MemoryStream copy = new();
                content.CopyTo(copy);
                string key = StorageKeys.Create(extension, DateTime.UtcNow);
                _files[key] = copy.ToArray();
                Saved.Add(key);
                return Task.FromResult(key);
            }

            public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
            {
                Opened.Add(key);
                return Task.FromResult<Stream>(new MemoryStream(_files[key]));
            }

            public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            {
                _files.Remove(key);
                return Task.CompletedTask;
            }

            public Task CheckAsync(CancellationToken cancellationToken = default)
            {
                Checks++;
                if (FailOnCheck) throw new StorageException("No valid Azure credentials are available on the server.");
                return Task.CompletedTask;
            }
        }
    }
}
