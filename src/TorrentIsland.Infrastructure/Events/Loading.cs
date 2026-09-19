namespace TorrentIsland.Infrastructure.Events;

public class Loading
{
    public static string Message
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                LoadingMessageChanged?.Invoke(null, value);
            }
        }
    } = string.Empty;

    public static event EventHandler<string>? LoadingMessageChanged;
}
