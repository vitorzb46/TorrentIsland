using System.Diagnostics;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using TorrentIsland.Application.Settings;
using TorrentIsland.Infrastructure.Logging;

namespace TorrentIsland.Infrastructure.Services;

public class MkvExtract
{
    public static async Task<bool> DownloadBinary()
    {
        try
        {
            using HttpClient client = new();
            var url = "https://mkvtoolnix.download/windows/releases/101.0/mkvtoolnix-64-bit-101.0.7z";            
            var zipBytes = await client.GetByteArrayAsync(url);
            using var zipMem = new MemoryStream(zipBytes);
            using var zip = SevenZipArchive.OpenArchive(zipMem);
            var list = zip.Entries.Where(f => f.Key!.Contains(".exe"))
                                  .Select(s => s)
                                  .ToList();

            foreach (var entry in list)
            {
                if (entry.Key == "mkvtoolnix/mkvextract.exe")
                {
                    await entry.WriteToFileAsync(AppSettings.MkvExtract);
                    break;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Salvar($"Falha ao baixar mkvextract.exe: {ex.Message}");
            return false;
        }
    }

    public static async Task<Process?> WaitForProcess(List<string> argsList)
    {
        var args = string.Join(' ', argsList);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = AppSettings.MkvExtract,
            Arguments = args,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(processStartInfo);
        if (process != null)
        {
            string erros = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(erros))
                Log.Salvar($"[mkvextract ERRO] {erros}");
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        else
        {
            Log.Salvar("Falha ao iniciar o processo do mkvextract.");
        }
        await process!.WaitForExitAsync().ConfigureAwait(false);
        return process;
    }
}