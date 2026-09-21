using Application.Features.Documents;
using Domain.Enums;
using FluentAssertions;

namespace LoanApi.Test
{
    [TestFixture]
    public class DocumentRulesTests
    {
        [TestCase("Contract", DocumentTypes.Contract)]
        [TestCase("contract", DocumentTypes.Contract)]
        [TestCase("IDCopy", DocumentTypes.IDCopy)]
        public void TryParseDocumentType_AcceptsKnownNames(string value, DocumentTypes expected)
        {
            DocumentRules.TryParseDocumentType(value, out DocumentTypes type).Should().BeTrue();
            type.Should().Be(expected);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Nope")]
        [TestCase("99")]
        [TestCase("1")]
        public void TryParseDocumentType_RejectsUnknownOrNumericValues(string? value)
        {
            DocumentRules.TryParseDocumentType(value, out _).Should().BeFalse();
        }

        [TestCase("contrato.PDF", MimeTypes.Pdf)]
        [TestCase("foto.jpeg", MimeTypes.Jpeg)]
        [TestCase("foto.jpg", MimeTypes.Jpeg)]
        [TestCase("hoja.xlsx", MimeTypes.Xlsx)]
        public void TryGetMimeType_FollowsTheExtension(string fileName, MimeTypes expected)
        {
            DocumentRules.TryGetMimeType(fileName, out MimeTypes mime).Should().BeTrue();
            mime.Should().Be(expected);
        }

        [Test]
        public void Validate_AcceptsAWellFormedDocument()
        {
            DocumentRules.Validate("contrato.pdf", "https://storage.example.com/a/contrato.pdf", 1200).Should().BeEmpty();
        }

        [TestCase("", "https://x.com/a.pdf")]
        [TestCase("virus.exe", "https://x.com/a.pdf")]
        [TestCase("../etc/passwd.pdf", "https://x.com/a.pdf")]
        [TestCase("a.pdf", "http://x.com/a.pdf")]
        [TestCase("a.pdf", "javascript:alert(1)")]
        [TestCase("a.pdf", "/relative/a.pdf")]
        public void Validate_RejectsBadNamesAndUrls(string fileName, string url)
        {
            DocumentRules.Validate(fileName, url, 10).Should().NotBeEmpty();
        }

        [Test]
        public void Validate_RejectsANegativeSize()
        {
            DocumentRules.Validate("a.pdf", "https://x.com/a.pdf", -1).Should().NotBeEmpty();
        }

        [Test]
        public void ContentMatchesType_RecognizesEachSupportedSignature()
        {
            DocumentRules.ContentMatchesType("%PDF-1.7"u8, MimeTypes.Pdf).Should().BeTrue();
            DocumentRules.ContentMatchesType([0xFF, 0xD8, 0xFF, 0xE0], MimeTypes.Jpeg).Should().BeTrue();
            DocumentRules.ContentMatchesType([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], MimeTypes.Png).Should().BeTrue();
            DocumentRules.ContentMatchesType([0x50, 0x4B, 0x03, 0x04, 0x14], MimeTypes.Xlsx).Should().BeTrue();
            DocumentRules.ContentMatchesType([0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], MimeTypes.Xls).Should().BeTrue();
            DocumentRules.ContentMatchesType("a,b,c\n1,2,3"u8, MimeTypes.Csv).Should().BeTrue();
        }

        [Test]
        public void ContentMatchesType_RejectsAFileThatIsNotWhatItClaims()
        {
            byte[] windowsExecutable = [0x4D, 0x5A, 0x90, 0x00];

            foreach (MimeTypes type in Enum.GetValues<MimeTypes>())
            {
                DocumentRules.ContentMatchesType(windowsExecutable, type).Should().BeFalse(type.ToString());
            }

            DocumentRules.ContentMatchesType("%PDF-1.7"u8, MimeTypes.Png).Should().BeFalse();
            DocumentRules.ContentMatchesType([], MimeTypes.Csv).Should().BeFalse();
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(10 * 1024 * 1024, true)]
        [TestCase(10 * 1024 * 1024 + 1, false)]
        public void ValidateUpload_EnforcesTheSizeLimits(long length, bool valid)
        {
            (DocumentRules.ValidateUpload("a.pdf", length).Count == 0).Should().Be(valid);
        }

        [Test]
        public void ContentType_HasAValueForEveryMimeType()
        {
            foreach (MimeTypes type in Enum.GetValues<MimeTypes>())
            {
                DocumentRules.ContentType(type).Should().NotBeNullOrEmpty();
            }
        }
    }
}
