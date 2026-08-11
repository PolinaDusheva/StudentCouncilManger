namespace StudentCouncil.Application.Common.Files;

/// <summary>
/// Magic-byte verification (spec 8 / 13): the file's leading bytes must match the format its
/// extension claims, so a renamed executable cannot pass as a <c>.pdf</c>. Pure text formats
/// (<c>.txt</c>, <c>.csv</c>) have no stable signature and are validated by extension + MIME only.
/// </summary>
public static class FileSignatures
{
    /// <summary>Number of leading bytes a caller should buffer for signature checks.</summary>
    public const int HeaderLength = 16;

    // Extensions whose binary content shares a signature with several formats.
    private static readonly byte[] Pdf = "%PDF"u8.ToArray();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Gif = "GIF8"u8.ToArray();
    private static readonly byte[] Zip = [0x50, 0x4B]; // PK — ZIP-based OOXML / ODT
    private static readonly byte[] Ole2 = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]; // legacy Office

    private static readonly IReadOnlyDictionary<string, byte[][]> Prefixes =
        new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = [Pdf],
            [".png"] = [Png],
            [".jpg"] = [Jpeg],
            [".jpeg"] = [Jpeg],
            [".gif"] = [Gif],
            [".docx"] = [Zip],
            [".xlsx"] = [Zip],
            [".pptx"] = [Zip],
            [".odt"] = [Zip],
            [".doc"] = [Ole2],
            [".xls"] = [Ole2],
            [".ppt"] = [Ole2]
        };

    /// <summary>
    /// Returns <c>true</c> when <paramref name="header"/> matches the signature expected for
    /// <paramref name="extension"/>, or when the extension carries no signature requirement.
    /// </summary>
    public static bool Matches(string extension, ReadOnlySpan<byte> header)
    {
        // ISO base-media (HEIC): an 'ftyp' box at offset 4. The brand varies (heic/heix/mif1/...).
        if (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase))
        {
            return header.Length >= 8 && header.Slice(4, 4).SequenceEqual("ftyp"u8);
        }

        if (!Prefixes.TryGetValue(extension, out var candidates))
        {
            return true; // No signature on record (e.g. .txt, .csv) — extension + MIME govern.
        }

        foreach (var prefix in candidates)
        {
            if (header.Length >= prefix.Length && header[..prefix.Length].SequenceEqual(prefix))
            {
                return true;
            }
        }

        return false;
    }
}
