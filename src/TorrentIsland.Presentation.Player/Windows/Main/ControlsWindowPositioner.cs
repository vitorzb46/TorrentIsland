using System.Windows;
using System.Windows.Threading;
using TorrentIsland.Presentation.Player.Abstractions;
using TorrentIsland.Presentation.Player.ViewModel;

namespace TorrentIsland.Presentation.Player.Windows.Main;

public sealed class ControlsWindowPositioner(IPlayerUiContext context,
                        IControlsWindow controls,
                        PlayerViewModel viewModel,
                        IUiDispatcher dispatcher,
                        IScreenMetrics metrics)
{

    public void RefreshLayout()
    {
        Reposition();
        dispatcher.BeginInvoke(new Action(() =>
        {
            ResetVideoView();
        }), DispatcherPriority.Render);
    }

    private void Reposition()
    {
        bool fullscreen = viewModel.IsFullscreen;
        bool maximized = context.Property.WindowState == WindowState.Maximized;

        if (fullscreen) controls.Apply(Fullscreen());

        else if (maximized) controls.Apply(Maximized());

        else controls.Apply(Normal());
    }

    private ControlsGeometry Fullscreen() =>
        new(Left: 0,
            Top: metrics.PrimaryScreenHeight - controls.Height,
            Width: metrics.PrimaryScreenWidth);

    private ControlsGeometry Maximized() =>
        new(Left: metrics.WorkArea.Left,
            Top: metrics.WorkArea.Bottom - controls.Height,
            Width: metrics.WorkArea.Width);

    private ControlsGeometry Normal() =>
        new(Left: context.Property.Left,
            Top: context.Property.Top + context.Property.ActualHeight - controls.Height,
            Width: context.Property.ActualWidth);

    private void ResetVideoView()
    {
        context.PwVideoView.Width = 0;
        context.PwVideoView.Height = 0;
        context.PwVideoView.Width = double.NaN;
        context.PwVideoView.Height = double.NaN;
        context.PwVideoView.InvalidateVisual();
    }
}