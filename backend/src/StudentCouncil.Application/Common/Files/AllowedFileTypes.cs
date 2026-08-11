namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// Whitelist of accepted upload formats (functional spec 7.2), mapping a lower-cased file
/// extension (with leading dot) to the MIME types a client may legitimately declare for it.
/// </summary>
public static class AllowedFileTypes
{
    private const string Pdf = "application/pdf";
    private const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string Doc = "application/msword";
    private const string Txt = "text/plain";
    private const string Odt = "application/vnd.oasis.opendocument.text";
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Xls = "application/vnd.ms-excel";
    private const string Pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private const string Ppt = "application/vnd.ms-powerpoint";
    private const string Jpeg = "image/jpeg";
    private const string Png = "image/png";
    private const string Gif = "image/gif";
    private const string Heic = "image/heic";
    private const string Heif = "image/heif";

    /// <summary>All formats accepted as task documents.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Documents =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = [Pdf],
            [".docx"] = [Docx],
            [".doc"] = [Doc],
            [".txt"] = [Txt],
            [".odt"] = [Odt],
            [".xlsx"] = [Xlsx],
            [".xls"] = [Xls],
            [".csv"] = ["text/csv", "application/csv", Txt],
            [".pptx"] = [Pptx],
            [".ppt"] = [Ppt],
            [".jpg"] = [Jpeg],
            [".jpeg"] = [Jpeg],
            [".png"] = [Png],
            [".gif"] = [Gif],
            [".heic"] = [Heic, Heif]
        };

    /// <summary>Subset accepted as profile photos (functional spec 5.6).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Images =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = [Jpeg],
            [".jpeg"] = [Jpeg],
            [".png"] = [Png],
            [".heic"] = [Heic, Heif]
        };
}
