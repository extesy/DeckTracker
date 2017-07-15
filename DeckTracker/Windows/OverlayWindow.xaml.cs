using System;
using System.Windows;
using System.Windows.Interop;
using DeckTracker.Domain;
using DeckTracker.LowLevel;

namespace DeckTracker.Windows
{
    public partial class OverlayWindow : Window
    {
        private readonly GameType gameType;
        private readonly IntPtr gameWindow;
        private readonly IntPtr handle;

        public OverlayWindow(GameType gameType, IntPtr gameWindow)
        {
            InitializeComponent();
            this.gameType = gameType;
            this.gameWindow = gameWindow;
            handle = new WindowInteropHelper(this).Handle;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            WindowsHelper.EnableClickthrough(handle);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var presentationsource = PresentationSource.FromVisual(this);
            WindowsHelper.DpiScalingX = presentationsource?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            WindowsHelper.DpiScalingY = presentationsource?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        }

        public void OnGameProcessStateChange()
        {
            var screenCoordinates = WindowsHelper.GetScreenCoordinates(gameWindow);
            ShowOverlay(WindowsHelper.IsForegroundWindow(gameWindow) && WindowsHelper.GetWindowState(gameWindow) != WindowState.Minimized && !screenCoordinates.IsEmpty);
            if (!screenCoordinates.IsEmpty) {
                Top = screenCoordinates.Top;
                Left = screenCoordinates.Left;
                Height = Canvas.Height = screenCoordinates.Height;
                Width = Canvas.Width = screenCoordinates.Width;
            }
        }

        private void ShowOverlay(bool enable)
        {
            if (enable) {
                Show();
                WindowsHelper.EnsureWindowOrder(gameWindow, handle);
            } else {
                Hide();
            }
        }
    }
}
