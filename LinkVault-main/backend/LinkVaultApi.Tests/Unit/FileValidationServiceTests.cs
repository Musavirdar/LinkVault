using System.Text;
using FluentAssertions;
using LinkVaultApi.Services;
using Xunit;

namespace LinkVaultApi.Tests.Unit;

public class FileValidationServiceTests
{
    private readonly FileValidationService _service = new();

    [Fact]
    public void ValidateFileContent_ValidPdf_ShouldReturnTrue()
    {
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%âãÏÓ\n1 0 obj...");
        using var stream = new MemoryStream(pdfBytes);

        var result = _service.ValidateContent(stream, "application/pdf", "document.pdf");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFileContent_DisguisedExeAsPdf_ShouldReturnFalse()
    {
        // MZ header disguised as PDF
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(exeBytes);

        var result = _service.ValidateContent(stream, "application/pdf", "malicious.pdf");
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Executable");
    }

    [Fact]
    public void ValidateFileContent_ValidPng_ShouldReturnTrue()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        using var stream = new MemoryStream(pngBytes);

        var result = _service.ValidateContent(stream, "image/png", "image.png");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFileContent_ValidJpeg_ShouldReturnTrue()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var stream = new MemoryStream(jpegBytes);

        var result = _service.ValidateContent(stream, "image/jpeg", "photo.jpg");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFileContent_SafeSvg_ShouldReturnTrue()
    {
        var svgText = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><circle cx=\"50\" cy=\"50\" r=\"40\" fill=\"red\" /></svg>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgText));

        var result = _service.ValidateContent(stream, "image/svg+xml", "icon.svg");
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateFileContent_MaliciousSvgWithScript_ShouldReturnFalse()
    {
        var svgWithScript = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert('XSS')</script></svg>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgWithScript));

        var result = _service.ValidateContent(stream, "image/svg+xml", "xss.svg");
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("script");
    }

    [Fact]
    public void SanitizeFileName_ShouldStripDirectoryTraversalAndNullBytes()
    {
        var maliciousName = "../../../etc/passwd\0.png";
        var safeName = _service.SanitizeFileName(maliciousName);

        safeName.Should().NotContain("..");
        safeName.Should().NotContain("/");
        safeName.Should().NotContain("\\");
        safeName.Should().NotContain("\0");
        safeName.Should().EndWith(".png");
    }
}
