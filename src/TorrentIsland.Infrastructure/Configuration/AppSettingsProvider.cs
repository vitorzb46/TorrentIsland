using Microsoft.Extensions.Configuration;
using TorrentIsland.Application.Settings;

namespace TorrentIsland.Infrastructure.Configuration;

/// <summary>
/// Carrega o <see cref="AppSettings"/> a partir da seção "AppSettings" da configuração,
/// preservando os defaults definidos nos initializers da classe.
/// </summary>
public static class AppSettingsProvider
{
    public static AppSettings Carregar(IConfiguration configuration)
    {
        var settings = new AppSettings();
        configuration.GetSection("AppSettings").Bind(settings);
        return settings;
    }
}
