using Domain.Enums;

namespace Application.Features.Documents
{
    /// <summary>Pure validation rules for the metadata registered for a loan document.</summary>
    public static class DocumentRules
    {
        public const int MaxFileNameLength = 200;
        public const long MaxFileBytes = 10 * 1024 * 1024;

        private static readonly Dictionary<string, MimeTypes> MimeByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = MimeTypes.Pdf,
            [".jpg"] = MimeTypes.Jpeg,
            [".jpeg"] = MimeTypes.Jpeg,
            [".png"] = MimeTypes.Png,
            [".xls"] = MimeTypes.Xls,
            [".xlsx"] = MimeTypes.Xlsx,
            [".csv"] = MimeTypes.Csv,
        };

        public static bool TryParseDocumentType(string? value, out DocumentTypes type)
        {
            type = default;
            return !string.IsNullOrWhiteSpace(value)
                && !int.TryParse(value, out _)
                && Enum.TryParse(value, ignoreCase: true, out type)
                && Enum.IsDefined(type);
        }

        /// <summary>The mime type is derived from the file extension so it can never contradict the file name.</summary>
        public static bool TryGetMimeType(string fileName, out MimeTypes mimeType)
            => MimeByExtension.TryGetValue(Path.GetExtension(fileName), out mimeType);

        public static string ContentType(MimeTypes mimeType) => mimeType switch
        {
            MimeTypes.Pdf => "application/pdf",
            MimeTypes.Jpeg => "image/jpeg",
            MimeTypes.Png => "image/png",
            MimeTypes.Xls => "application/vnd.ms-excel",
            MimeTypes.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv",
        };

        /// <summary>
        /// The extension is only a claim. This checks the first bytes of the file really are what the extension says
        /// (a CSV has no signature, so it must simply be text).
        /// </summary>
        public static bool ContentMatchesType(ReadOnlySpan<byte> header, MimeTypes mimeType) => mimeType switch
        {
            MimeTypes.Pdf => header.StartsWith("%PDF-"u8),
            MimeTypes.Jpeg => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            MimeTypes.Png => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            MimeTypes.Xlsx => header.StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            MimeTypes.Xls => header.StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            _ => header.Length > 0 && !header.Contains((byte)0),
        };

        public static List<string> ValidateUpload(string? fileName, long length)
        {
            var errors = new List<string>();
            CheckFileName(errors, fileName);

            if (length <= 0) errors.Add("The file is empty.");
            else if (length > MaxFileBytes) errors.Add($"The file cannot exceed {MaxFileBytes / (1024 * 1024)} MB.");

            return errors;
        }

        private static void CheckFileName(List<string> errors, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                errors.Add("The file name is required.");
            else if (fileName.Trim().Length > MaxFileNameLength)
                errors.Add($"The file name cannot exceed {MaxFileNameLength} characters.");
            else if (fileName.IndexOfAny(new[] { '/', '\\' }) >= 0)
                errors.Add("The file name cannot contain path separators.");
            else if (!TryGetMimeType(fileName.Trim(), out _))
                errors.Add("The file type is not allowed; use pdf, jpg, jpeg, png, xls, xlsx or csv.");
        }

        public static List<string> Validate(string? fileName, string? fileUrl, long fileSizeBytes)
        {
            var errors = new List<string>();
            CheckFileName(errors, fileName);

            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                errors.Add("The file URL must be an absolute https address.");

            if (fileSizeBytes < 0)
                errors.Add("The file size cannot be negative.");

            return errors;
        }
    }
}
