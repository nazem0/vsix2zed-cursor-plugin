using System.IO.Compression;

namespace Vsix2Zed.Core;

public static class Unpacker
{
    /// <summary>Extracts a .vsix (zip) and returns the directory that contains package.json.</summary>
    public static string Unpack(string vsixPath, string? workDir = null)
    {
        if (!File.Exists(vsixPath))
            throw new FileNotFoundException("VSIX not found", vsixPath);

        workDir ??= Path.Combine(Path.GetTempPath(), "vsix2zed", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        ZipFile.ExtractToDirectory(vsixPath, workDir, overwriteFiles: true);

        var package = Directory.EnumerateFiles(workDir, "package.json", SearchOption.AllDirectories)
            .OrderBy(p => p.Length)
            .FirstOrDefault();

        if (package is null)
            throw new InvalidOperationException("No package.json found inside VSIX.");

        return Path.GetDirectoryName(package)!;
    }
}
