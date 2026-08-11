using FluentAssertions;
using StudentCouncil.Application.Common.Exceptions;
using StudentCouncil.Application.Common.Files;

namespace StudentCouncil.UnitTests.Files;

public class FileValidationTests
{
    private const long MaxBytes = 25L * 1024 * 1024;

    private static readonly byte[] PdfHeader = "%PDF-1.7\n%abc"u8.ToArray();
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static readonly byte[] ZipHeader = [0x50, 0x4B, 0x03, 0x04, 0, 0, 0, 0];
    private static readonly byte[] ExeHeader = [0x4D, 0x5A, 0x90, 0x00, 0, 0, 0, 0]; // "MZ" — Windows PE

    private static string ValidateDocument(string name, string? contentType, long length, byte[] header) =>
        FileValidation.Validate(name, contentType, length, header, MaxBytes, AllowedFileTypes.Documents);

    [Fact]
    public void Accepts_a_real_pdf()
    {
        ValidateDocument("report.pdf", "application/pdf", 2048, PdfHeader).Should().Be(".pdf");
    }

    [Fact]
    public void Accepts_a_real_png()
    {
        ValidateDocument("photo.PNG", "image/png", 4096, PngHeader).Should().Be(".png");
    }

    [Fact]
    public void Accepts_a_docx_by_zip_signature()
    {
        ValidateDocument(
            "minutes.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            8192,
            ZipHeader).Should().Be(".docx");
    }

    [Fact]
    public void Accepts_a_generic_octet_stream_when_magic_bytes_match()
    {
        // Clients (e.g. curl) often send application/octet-stream; the signature carries the weight.
        ValidateDocument("report.pdf", "application/octet-stream", 2048, PdfHeader).Should().Be(".pdf");
    }

    [Fact]
    public void Accepts_csv_without_a_signature()
    {
        ValidateDocument("data.csv", "text/csv", 64, "id,name\n1,a"u8.ToArray()).Should().Be(".csv");
    }

    [Fact]
    public void Rejects_an_executable_renamed_to_pdf()
    {
        var act = () => ValidateDocument("malware.pdf", "application/pdf", 1024, ExeHeader);

        act.Should().Throw<BadRequestException>().Which.Code.Should().Be("unsupported_file_type");
    }

    [Fact]
    public void Rejects_a_disallowed_extension()
    {
        var act = () => ValidateDocument("tool.exe", "application/octet-stream", 1024, ExeHeader);

        act.Should().Throw<BadRequestException>().Which.Code.Should().Be("unsupported_file_type");
    }

    [Fact]
    public void Rejects_a_mismatched_specific_content_type()
    {
        var act = () => ValidateDocument("report.pdf", "image/png", 1024, PdfHeader);

        act.Should().Throw<BadRequestException>().Which.Code.Should().Be("unsupported_file_type");
    }

    [Fact]
    public void Rejects_an_oversized_file_with_413()
    {
        var act = () => ValidateDocument("big.pdf", "application/pdf", MaxBytes + 1, PdfHeader);

        act.Should().Throw<FileTooLargeException>().Which.StatusCode.Should().Be(413);
    }

    [Fact]
    public void Rejects_an_empty_file()
    {
        var act = () => ValidateDocument("empty.pdf", "application/pdf", 0, PdfHeader);

        act.Should().Throw<BadRequestException>().Which.Code.Should().Be("unsupported_file_type");
    }

    [Fact]
    public void Image_whitelist_excludes_documents()
    {
        var act = () => FileValidation.Validate("report.pdf", "application/pdf", 1024, PdfHeader, MaxBytes, AllowedFileTypes.Images);

        act.Should().Throw<BadRequestException>().Which.Code.Should().Be("unsupported_file_type");
    }
}
