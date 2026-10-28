using System.Text;
using System.Text.RegularExpressions;

namespace LinkVaultApi.Services;

public record FileValidationResult(bool IsValid, string? ErrorMessage = null);

public interface IFileValidationService
{
    FileValidationResult ValidateContent(Stream stream, string declaredMimeType, string fileName);
    string SanitizeFileName(string fileName);
}

public class FileValidationService : IFileValidationService
{
    private static readonly byte[] ExecutableDosHeader = { 0x4D, 0x5A }; // "MZ"
    private static readonly byte[] ExecutableElfHeader = { 0x7F, 0x45, 0x4C, 0x46 }; // ".ELF"
    private static readonly byte[] PdfHeader = Encoding.ASCII.GetBytes("%PDF-");
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] Gif87Header = Encoding.ASCII.GetBytes("GIF87a");
    private static readonly byte[] Gif89Header = Encoding.ASCII.GetBytes("GIF89a");
    private static readonly byte[] ZipHeader1 = { 0x50, 0x4B, 0x03, 0x04 };
    private static readonly byte[] ZipHeader2 = { 0x50, 0x4B, 0x05, 0x06 };

    private static readonly Regex SvgMaliciousPatterns = new(
        @"(<script|javascript:|on\w+\s*=|<iframe|<embed|<object)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public FileValidationResult ValidateContent(Stream stream, string declaredMimeType, string fileName)
    {
        if (stream == null || stream.Length == 0)
        {
            return new FileValidationResult(false, "File stream is empty.");
        }

        var initialPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            // Read first 512 bytes for header inspection
            var header = new byte[Math.Min(512, (int)stream.Length)];
            var bytesRead = stream.Read(header, 0, header.Length);

            if (bytesRead < 4 && declaredMimeType != "text/plain")
            {
                return new FileValidationResult(false, "File header is too short to be valid.");
            }

            // Universal check: Disallow disguised binary executables (PE, ELF)
            if (StartsWith(header, ExecutableDosHeader) || StartsWith(header, ExecutableElfHeader))
            {
                return new FileValidationResult(false, "Executable files (.exe, binary payloads) are strictly disallowed.");
            }

            // MIME-specific magic byte verification
            var normalizedMime = declaredMimeType.Trim().ToLowerInvariant();

            switch (normalizedMime)
            {
                case "application/pdf":
                    if (!StartsWith(header, PdfHeader))
                        return new FileValidationResult(false, "File content does not match declared PDF format.");
                    break;

                case "image/png":
                    if (!StartsWith(header, PngHeader))
                        return new FileValidationResult(false, "File content does not match declared PNG format.");
                    break;

                case "image/jpeg":
                    if (!StartsWith(header, JpegHeader))
                        return new FileValidationResult(false, "File content does not match declared JPEG format.");
                    break;

                case "image/gif":
                    if (!StartsWith(header, Gif87Header) && !StartsWith(header, Gif89Header))
                        return new FileValidationResult(false, "File content does not match declared GIF format.");
                    break;

                case "image/webp":
                    // RIFF at [0..3] and WEBP at [8..11]
                    if (header.Length < 12 ||
                        Encoding.ASCII.GetString(header, 0, 4) != "RIFF" ||
                        Encoding.ASCII.GetString(header, 8, 4) != "WEBP")
                    {
                        return new FileValidationResult(false, "File content does not match declared WebP format.");
                    }
                    break;

                case "application/zip":
                case "application/x-zip-compressed":
                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                case "application/vnd.openxmlformats-officedocument.presentationml.presentation":
                    if (!StartsWith(header, ZipHeader1) && !StartsWith(header, ZipHeader2))
                        return new FileValidationResult(false, "File content does not match declared archive or Office document format.");
                    break;

                case "image/svg+xml":
                    // Reset position and read up to 4KB of SVG text to check for malicious script payloads
                    if (stream.CanSeek) stream.Position = 0;
                    var svgScanBuffer = new byte[Math.Min(4096, (int)stream.Length)];
                    var svgBytesRead = stream.Read(svgScanBuffer, 0, svgScanBuffer.Length);
                    var svgContent = Encoding.UTF8.GetString(svgScanBuffer, 0, svgBytesRead);

                    if (SvgMaliciousPatterns.IsMatch(svgContent))
                    {
                        return new FileValidationResult(false, "SVG contains disallowed active scripts or embedded elements.");
                    }

                    if (!svgContent.Contains("<svg", StringComparison.OrdinalIgnoreCase))
                    {
                        return new FileValidationResult(false, "File content does not contain a valid SVG root element.");
                    }
                    break;
            }

            return new FileValidationResult(true);
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = initialPosition;
            }
        }
    }

    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unnamed_file";
        }

        // Strip path traversal and null characters
        var clean = fileName
            .Replace("\0", "")
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");

        clean = Path.GetFileName(clean);

        // Keep alphanumeric, dash, underscore, dot
        var sanitized = Regex.Replace(clean, @"[^\w\.\-]", "_");

        // Trim leading and trailing dots or underscores
        sanitized = sanitized.Trim('.', '_');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "file";
        }

        return sanitized;
    }

    private static bool StartsWith(byte[] array, byte[] prefix)
    {
        if (array.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (array[i] != prefix[i]) return false;
        }
        return true;
    }
}
