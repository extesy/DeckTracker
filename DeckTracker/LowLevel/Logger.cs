using System;
using System.IO;
using DeckTracker.Domain;

namespace DeckTracker.LowLevel
{
    internal static class Logger
    {
#if DEBUG
        public static bool DebugMode = true;
#else
        public static bool DebugMode = false;
#endif
        public static readonly string GameDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UniversalDeckTracker");
        private static readonly string ErrorLogFile = Path.Combine(GameDataDirectory, "error.log");
        private static readonly string LogFile = Path.Combine(GameDataDirectory, "events.log");
        public static readonly string GamesFile = Path.Combine(GameDataDirectory, "games.log");

        public static void Initialize()
        {
            if (!Directory.Exists(GameDataDirectory))
                Directory.CreateDirectory(GameDataDirectory);
            GameMessageDispatcher.OnGameMessage += OnGameMessage;
        }

        private static void OnGameMessage(GameMessage gameMessage)
        {
            switch (gameMessage.MessageType) {
                case MessageType.Command:
                case MessageType.Replay:
                case MessageType.Decks:
                    break;
                case MessageType.Game:
                    lock (GamesFile) {
                        using (var writer = File.AppendText(GamesFile))
                            writer.WriteLine($"{gameMessage.Timestamp:o}|{gameMessage.GameType}|{gameMessage.Message}");
                    }
                    break;
                default:
                    if (DebugMode) {
                        lock (LogFile) {
                            using (var writer = File.AppendText(LogFile))
                                writer.WriteLine($"{gameMessage.Timestamp:o}|{gameMessage.GameType}|{gameMessage.MessageType}|{gameMessage.Message}");
                        }
                    }
                    break;
            }
        }

        public static void LogDebug(GameType gameType, string message)
        {
            OnGameMessage(new GameMessage {
                GameType = gameType,
                MessageType = MessageType.Debug,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }

        public static void LogError(string line)
        {
            lock (ErrorLogFile) {
                using (var sw = File.AppendText(ErrorLogFile)) {
                    sw.WriteLine(line);
                    sw.WriteLine();
                }
            }
        }
    }
}
