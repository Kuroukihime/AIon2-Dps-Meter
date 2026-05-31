using System.Diagnostics;
using System.IO.Compression;

// Args: <zipPath> <targetDir> <mainExeName> <mainPid>
if (args.Length < 4)
{
    Console.Error.WriteLine("Usage: AionDpsMeter.Updater <zipPath> <targetDir> <mainExeName> <mainPid>");
    return 1;
}

var zipPath    = args[0];
var targetDir  = args[1];
var mainExe    = args[2];
var mainPid    = int.Parse(args[3]);

try
{
    var proc = Process.GetProcessById(mainPid);
    if (!proc.WaitForExit(30_000))
    {
        proc.Kill();
        proc.WaitForExit(5_000);
    }
}
catch (ArgumentException)
{
 
}

await Task.Delay(500);


try
{
    using var archive = ZipFile.OpenRead(zipPath);

   
    string stripPrefix = string.Empty;
    var topDirs = archive.Entries
        .Select(e => e.FullName.Split('/')[0])
        .Distinct()
        .ToList();
    if (topDirs.Count == 1 && !topDirs[0].Contains('.'))
        stripPrefix = topDirs[0] + "/";

    foreach (var entry in archive.Entries)
    {
        var relativePath = entry.FullName;
        if (!string.IsNullOrEmpty(stripPrefix) && relativePath.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath[stripPrefix.Length..];

        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.EndsWith('/'))
            continue;

     
        var fileName = Path.GetFileName(relativePath);
        if (fileName.Equals("AionDpsMeter.Updater.exe", StringComparison.OrdinalIgnoreCase))
            continue;

        var destPath = Path.Combine(targetDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        entry.ExtractToFile(destPath, overwrite: true);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Extraction failed: {ex.Message}");
    return 2;
}

try { File.Delete(zipPath); } catch {  }

var exePath = Path.Combine(targetDir, mainExe);
if (File.Exists(exePath))
    Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });

return 0;
