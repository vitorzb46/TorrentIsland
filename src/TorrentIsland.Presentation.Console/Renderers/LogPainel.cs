using System.Text;

namespace TorrentIsland.Presentation.Console.Renderers;

/// <summary>
/// Painel estático montado pelo MainLoop do aplicativo (ex-SB/Adicionar do antigo Log estático).
/// </summary>
public sealed class LogPainel
{
    private readonly object _sync = new();
    private readonly StringBuilder _sb = new();

    public void Adicionar(string mensagem)
    {
        lock (_sync)
        {
            _sb.AppendLine(mensagem);
        }
    }

    public void Limpar()
    {
        lock (_sync)
        {
            _sb.Clear();
        }
    }

    public string Conteudo()
    {
        lock (_sync)
        {
            return _sb.ToString();
        }
    }
}
