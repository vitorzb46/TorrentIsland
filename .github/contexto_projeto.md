# Arquitetura do Projeto 
 
## Estrutura de Pastas 
``` 
- Core 
- Infrastructure 
- Presentation 
  - Application 
  - Domain 
  - Contracts 
  - DTOs 
  - Medias 
  - Commands 
  - Queries 
  - Entities 
  - Exceptions 
  - Interfaces 
  - Infrastructure.Media 
  - Infrastructure.Player 
  - Logging 
  - Services 
  - Presentation.Console 
  - Presentation.Wpf 
``` 
 
## Código Fonte (Apenas Assinaturas) 
### Arquivo: Class1.cs 
```csharp 
public class Class1
``` 
 
### Arquivo: IIniciarDownload.cs 
```csharp 
namespace Application.Contracts
    public interface IIniciarDownload
``` 
 
### Arquivo: IIniciarStreamTorrent.cs 
```csharp 
public interface IIniciarStreamTorrent
``` 
 
### Arquivo: StreamResult.cs 
```csharp 
namespace Application.DTOs;
public record StreamResult(string FullUri);
``` 
 
### Arquivo: IniciarDownload.cs 
```csharp 
namespace Application.Medias.Commands;
public class IniciarDownload(IDownloadService downloadService) : IIniciarDownload
    private readonly IDownloadService _downloadService = downloadService;
    public async Task<Guid> ExecutarPorPastaAsync(string filePath)
    public async Task<Guid> ExecutarAsync(string magnetLink)
``` 
 
### Arquivo: IniciarStreamTorrent.cs 
```csharp 
namespace Application.Medias.Commands;
public class IniciarStreamTorrent(IDownloadService downloadService) : IIniciarStreamTorrent
    public async Task<Stream> StreamAsync(string magnetLink)
``` 
 
### Arquivo: PararDownload.cs 
```csharp 
namespace Application.Medias.Commands;
public class PararDownload
``` 
 
### Arquivo: PausarDownload.cs 
```csharp 
namespace Application.Medias.Commands;
public class PausarDownload
``` 
 
### Arquivo: ObterHistoricoDownload.cs 
```csharp 
``` 
 
### Arquivo: ObterProgresso.cs 
```csharp 
``` 
 
### Arquivo: Class1.cs 
```csharp 
public class Class1
``` 
 
### Arquivo: DownloadItem.cs 
```csharp 
namespace Core.Domain.Entities
    public class DownloadItem
        public Guid Id { get; set; }
        public string? Titulo { get; set; }
        public double Progresso { get; set; } // 0 a 100
        public string? Status { get; set; }
``` 
 
### Arquivo: Exceptions.cs 
```csharp 
namespace Core.Domain.Exceptions
    public class InvalidMagnetLinkException : Exception
        public InvalidMagnetLinkException() : base("A URL fornecida não é um Link Magnetico válido.") {}
    public class ParamaterException : Exception
        public ParamaterException() : base("Parâmetro fornecido não pode ser vazio.") {}
    public class FolderException : Exception
        public FolderException() : base("Pasta fornecida está vazia ou inexistente!") {}
``` 
 
### Arquivo: IDownloadService.cs 
```csharp 
namespace Domain.Interfaces;
public interface IDownloadService
``` 
 
### Arquivo: IMediaPlayer.cs 
```csharp 
namespace Core.Domain.Interfaces
    public interface IMediaPlayer
``` 
 
### Arquivo: Class1.cs 
```csharp 
public class Class1
``` 
 
### Arquivo: Log.cs 
```csharp 
namespace Infrastructure.Media.Logging
    internal class Log
``` 
 
### Arquivo: TorrentService.cs 
```csharp 
namespace Infrastructure.Infrastructure.Media.Services;
public class Torrent
    public TorrentManager? Manager { get; set; }
    public TorrentSettings? Settings { get; }
    public Dictionary<Guid, TorrentManager>? List { get; set; }
    internal Torrent()
    private TorrentSettings TorrentSettings()
    public static Guid Id => Guid.NewGuid();
public class TorrentService(ClientEngine engine)
    private ClientEngine Engine { get; } = engine;
    private static Torrent? Torrent { get; set; }
    private static readonly Dictionary<Guid, TorrentManager> _torrents = [];
    public async Task<StreamResult> StreamAsync(string magnetUrl, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    private static async Task GetTrackers(MagnetLink magnet, CancellationToken cancellationToken)
    private static async Task StreamBuffer(TorrentManager manager, CancellationToken cancellationToken)
``` 
 
### Arquivo: Class1.cs 
```csharp 
public class Class1
``` 
 
### Arquivo: Program.cs 
```csharp 
``` 
 
### Arquivo: App.xaml.cs 
```csharp 
namespace Presentation.Wpf;
public partial class App : Application
``` 
 
### Arquivo: AssemblyInfo.cs 
```csharp 
``` 
 
### Arquivo: MainWindow.xaml.cs 
```csharp 
namespace Presentation.Wpf;
public partial class MainWindow : Window
    public MainWindow()
``` 
 
## Análise de Dependências (Graphify) 
# Graph Report - C:\Users\Vitor\Documents\Repos\Projects\TorrentIsland  (2026-08-16)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 115 nodes · 139 edges · 17 communities (12 shown, 5 thin omitted)
- Extraction: 99% EXTRACTED · 1% INFERRED · 0% AMBIGUOUS · INFERRED: 2 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `4151feaf`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Torrent
- IniciarDownload.cs
- .BaixarAsync
- Application
- App
- IIniciarStreamTorrent
- IMediaPlayer
- DownloadItem.cs
- Application/Class1.cs
- Domain/Class1.cs
- Infrastructure.Media/Class1.cs
- Log.cs
- Infrastructure.Player/Class1.cs

## God Nodes (most connected - your core abstractions)
1. `Application` - 7 edges
2. `Torrent` - 7 edges
3. `TorrentService` - 7 edges
4. `Microsoft.NET.Sdk` - 6 edges
5. `Domain` - 5 edges
6. `IniciarDownload` - 5 edges
7. `net10.0` - 5 edges
8. `IDownloadService` - 5 edges
9. `IMediaPlayer` - 5 edges
10. `Application.Medias.Commands` - 4 edges

## Surprising Connections (you probably didn't know these)
- `Infrastructure.Media` --references--> `net10.0`  [EXTRACTED]
  Infrastructure/Infrastructure.Media/Infrastructure.Media.csproj → Core/Domain/Domain.csproj
- `Infrastructure.Player` --references--> `net10.0`  [EXTRACTED]
  Infrastructure/Infrastructure.Player/Infrastructure.Player.csproj → Core/Domain/Domain.csproj
- `Presentation.Console` --references--> `net10.0`  [EXTRACTED]
  Presentation/Presentation.Console/Presentation.Console.csproj → Core/Domain/Domain.csproj
- `Infrastructure.Media` --references--> `Microsoft.NET.Sdk`  [EXTRACTED]
  Infrastructure/Infrastructure.Media/Infrastructure.Media.csproj → Core/Domain/Domain.csproj
- `Infrastructure.Player` --references--> `Microsoft.NET.Sdk`  [EXTRACTED]
  Infrastructure/Infrastructure.Player/Infrastructure.Player.csproj → Core/Domain/Domain.csproj

## Import Cycles
- None detected.

## Communities (17 total, 5 thin omitted)

### Community 0 - "Torrent"
Cohesion: 0.16
Nodes (14): CancellationToken, ClientEngine, StreamResult, Infrastructure.Infrastructure.Media.Services, Application.DTOs, Dictionary, Guid, IProgress (+6 more)

### Community 1 - "IniciarDownload.cs"
Cohesion: 0.15
Nodes (10): PararDownload, PausarDownload, FolderException, InvalidMagnetLinkException, ParamaterException, Application.Medias.Commands, Application.Contracts, Core.Domain.Exceptions (+2 more)

### Community 2 - ".BaixarAsync"
Cohesion: 0.18
Nodes (11): IIniciarDownload, Guid, Task, IniciarDownload, Guid, Task, Guid, IProgress (+3 more)

### Community 3 - "Application"
Cohesion: 0.42
Nodes (10): Application, Domain, net10.0, Microsoft.NET.Sdk, net10.0-windows, Infrastructure.Media, Infrastructure.Player, MonoTorrent (3.9.0-alpha.unstable.rev0000) (+2 more)

### Community 4 - "App"
Cohesion: 0.20
Nodes (6): Application, Presentation.Wpf, Application, App, Window, MainWindow

### Community 5 - "IIniciarStreamTorrent"
Cohesion: 0.22
Nodes (6): Stream, Task, IIniciarStreamTorrent, IniciarStreamTorrent, Stream, Task

### Community 6 - "IMediaPlayer"
Cohesion: 0.25
Nodes (3): IMediaPlayer, Stream, Core.Domain.Interfaces

### Community 7 - "DownloadItem.cs"
Cohesion: 0.50
Nodes (3): DownloadItem, Guid, Core.Domain.Entities

## Knowledge Gaps
- **17 isolated node(s):** `Application`, `Class1`, `PararDownload`, `PausarDownload`, `Domain` (+12 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IniciarDownload` connect `.BaixarAsync` to `IniciarDownload.cs`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Why does `IniciarStreamTorrent` connect `IIniciarStreamTorrent` to `IniciarDownload.cs`?**
  _High betweenness centrality (0.045) - this node is a cross-community bridge._
- **What connects `Application`, `Class1`, `PararDownload` to the rest of the system?**
  _17 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `IniciarDownload.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.14705882352941177 - nodes in this community are weakly interconnected._ 
