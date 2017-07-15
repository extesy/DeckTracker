using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DeckTracker.Domain;
using DeckTracker.Windows;
using Rectangle = System.Drawing.Rectangle;

namespace DeckTracker.LowLevel
{
    internal static class ProcessMonitor
    {
        private class GameProcessState
        {
            public readonly GameType GameType;
            public readonly string ProcessName;
            public readonly SocketStreamClient SocketStreamClient;
            public InjectionState InjectionState;
            public Process Process;
//            public IntPtr Window;
//            public WindowState WindowState;
//            public bool IsForeground;
//            public Rectangle ScreenCoordinates;
//            public OverlayWindow OverlayWindow;

            public GameProcessState(GameType gameType, string processName, int port)
            {
                GameType = gameType;
                ProcessName = processName;
                SocketStreamClient = new SocketStreamClient(gameType, port);
                Reset();
            }

            public void Reset()
            {
                SocketStreamClient.Stop();
                InjectionState = InjectionState.Idle;
                Process = null;
//                Window = IntPtr.Zero;
//                WindowState = WindowState.Normal;
//                IsForeground = false;
//                ScreenCoordinates = Rectangle.Empty;
//                OverlayWindow?.Close();
//                OverlayWindow = null;
            }
        }

        private static readonly GameProcessState[] GameProcessStates = {
            new GameProcessState(GameType.TheElderScrollsLegends, "The Elder Scrolls Legends", 50001),
            new GameProcessState(GameType.Eternal, "Eternal", 50002)
        };

        private static DispatcherTimer timer;
        private static int pingCounter = 0;

        public delegate void OnGameInjectionStateChangeHandler(GameType gameType, InjectionState injectionState);
        public static event OnGameInjectionStateChangeHandler OnGameInjectionStateChange;

        public static void Start()
        {
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (sender, args) => OnTimer(), Dispatcher.CurrentDispatcher);
            timer.Start();
            foreach (var state in GameProcessStates)
                OnGameInjectionStateChange?.Invoke(state.GameType, state.InjectionState);
            GameMessageDispatcher.OnGameMessage += OnGameMessage;
        }

        public static void Stop()
        {
            timer.Stop();
            foreach (var state in GameProcessStates)
                state.Reset();
        }

        public static GameType? RunningGameType => GameProcessStates.FirstOrDefault(state => state.InjectionState == InjectionState.Injected)?.GameType;

        public static string SendCommand(GameType gameType, CommandType command, int timeout = 100) => SendCommand(gameType, command, null, timeout);

        public static string SendCommand(GameType gameType, CommandType command, string parameter, int timeout = 100)
        {
            var client = GameProcessStates.FirstOrDefault(state => state.GameType == gameType)?.SocketStreamClient;
            if (client == null) return null;
            var task = client.SendCommand(command, parameter, new CancellationTokenSource(timeout).Token);
            try {
                task.Wait();
            } catch (AggregateException) {
                return null;
            }
            return task.Result;
        }

        private static void OnGameMessage(GameMessage gameMessage)
        {
            if (gameMessage.MessageType != MessageType.Quit) return;
            var currentState = GameProcessStates.FirstOrDefault(state => state.GameType == gameMessage.GameType);
            if (currentState == null) return;
            currentState.SocketStreamClient.Stop();
            currentState.InjectionState = InjectionState.Failed;
        }

        private static void OnTimer()
        {
            foreach (var state in GameProcessStates) {
                var previousInjectionState = state.InjectionState;
//                var previousWindowState = state.WindowState;
//                var previousIsForeground = state.IsForeground;
//                var previousScreenCoordinates = state.ScreenCoordinates;

                if (state.Process?.HasExited == true)
                    state.Reset();

                switch (state.InjectionState) {
                    case InjectionState.Idle:
                        var processes = Process.GetProcessesByName(state.ProcessName);
//                        foreach (var process in processes)
//                            Logger.LogDebug(state.GameType, $"Game process: {process.ProcessName} {process.MainModule.FileName}");
                        state.Process = processes.FirstOrDefault();
                        if (state.Process == null)
                            break;
                        state.InjectionState = InjectionState.Injecting;
                        break;
                    case InjectionState.Injecting:
                        state.SocketStreamClient.Start();
                        if (SendCommand(state.GameType, CommandType.Ping) == "Pong") {
                            state.InjectionState = InjectionState.Injected;
                            break;
                        }
                        Logger.LogDebug(state.GameType, "Injecting DeckTracker.InGame.Helper.dll");
                        string injectionLibrary = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DeckTracker.InGame.Helper.dll");
                        state.InjectionState = DllInjector.InjectDll(state.GameType, (uint)state.Process.Id, injectionLibrary, out int _) ? InjectionState.Injected : InjectionState.Failed;
                        if (state.InjectionState == InjectionState.Injected) {
                            Logger.LogDebug(state.GameType, "Injected DeckTracker.InGame.Helper.dll, waiting for response");
                            if (SendCommand(state.GameType, CommandType.Ping, 10000) != "Pong")
                                state.InjectionState = InjectionState.Failed;
                        }
                        break;
                    case InjectionState.Injected:
                        pingCounter = (pingCounter + 1) % 200;
                        if (pingCounter == 0 && SendCommand(state.GameType, CommandType.Ping) != "Pong") {
                            state.InjectionState = InjectionState.Disconnected;
                            break;
                        }
//                        if (state.Window == IntPtr.Zero)
//                            state.Window = WindowsHelper.FindUnityWindow(state.Process.Id);
//                        if (state.Window != IntPtr.Zero) {
//                            state.WindowState = WindowsHelper.GetWindowState(state.Window);
//                            state.IsForeground = WindowsHelper.IsForegroundWindow(state.Window);
//                            state.ScreenCoordinates = WindowsHelper.GetScreenCoordinates(state.Window);
//                        }
//                        if (state.OverlayWindow == null)
//                            state.OverlayWindow = new OverlayWindow(state.GameType, state.Window);
                        break;
                    case InjectionState.Disconnected:
//                        state.OverlayWindow?.Close();
//                        state.OverlayWindow = null;
                        pingCounter = (pingCounter + 1) % 200;
                        if (pingCounter == 0 && SendCommand(state.GameType, CommandType.Ping) == "Pong")
                            state.InjectionState = InjectionState.Injected;
                        break;
                    case InjectionState.Failed:
                        state.SocketStreamClient.Stop();
                        break;
                }

//                if (state.InjectionState != previousInjectionState || state.WindowState != previousWindowState || state.IsForeground != previousIsForeground || state.ScreenCoordinates != previousScreenCoordinates)
//                    state.OverlayWindow?.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => state.OverlayWindow.OnGameProcessStateChange()));

                if (state.InjectionState != previousInjectionState) {
                    Logger.LogDebug(state.GameType, $"Injection state changed: {state.InjectionState}");
                    OnGameInjectionStateChange?.Invoke(state.GameType, state.InjectionState);
                }
            }
        }
    }
}
