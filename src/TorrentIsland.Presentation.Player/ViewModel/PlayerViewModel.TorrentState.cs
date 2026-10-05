using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using TorrentIsland.Application.DTOs;
using TorrentIsland.Application.Interfaces;
using TorrentIsland.Presentation.Player.Common;

namespace TorrentIsland.Presentation.Player.ViewModel;

public partial class PlayerViewModel
{
    private readonly IFormattingHelper _fb;

    public ObservableCollection<TorrentDownloadDto> TorrentDownloads { get; } = [];

    public string TorrentName
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public double ProgressPercentage
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    [field: AllowNull, MaybeNull]
    public string? DownloadSpeed
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public string UploadSpeed
    {
        get => field ?? "N/A";
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public int Seeds
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public int Peers
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnTorrentUpdated(object? sender, TorrentDto dto)
    {
        Utils.AtualizarUI(() =>
        {
            TorrentName = dto.Nome ?? "Desconhecido";
            ProgressPercentage = _fb.FormatarPorcentagem(dto.Progresso);
            Status = dto.Estado.ToString();
            DownloadSpeed = _fb.FormatarBytes(dto.VelocidadeDownload);
            UploadSpeed = _fb.FormatarBytes(dto.VelocidadeUpload);
            Seeds = dto.Seeds;
            Peers = dto.ParesDisponiveis;
        });
    }
    private void OnTorrentQueue(object? sender, TorrentDownloadDto dto)
    {
        Utils.AtualizarUI(() =>
        {

            var torrent = TorrentDownloads.FirstOrDefault(t => t.TorrentId == dto.TorrentId);
            if (torrent == null)
            {
                TorrentDownloads.Add(dto);
            }
            else
            {
                torrent!.TorrentName = dto.TorrentName;
                torrent.Status = dto.Status;
                torrent.Progress = dto.Progress;
                torrent.DownloadSpeed = dto.DownloadSpeed;
                torrent.UploadSpeed = dto.UploadSpeed;
                torrent.Seeds = dto.Seeds;
                torrent.Peers = dto.Peers;
                torrent.TimeRemaining = dto.TimeRemaining;
            }
        });
    }
}
