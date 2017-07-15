using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using DeckTracker.Domain;
using DeckTracker.LowLevel.Zstd;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckTracker.LowLevel
{
    internal static class ReplayUploader
    {
        private const string ApiRoute = "https://decktrackerapi.azurewebsites.net/api/replays/{0}/{1}?deckTrackerVersion={2}&gameVersion={3}&compressionFormat=zstandard&serializationFormat=json";
        private static readonly string ReplaysFile = Path.Combine(Logger.GameDataDirectory, "replays.log");
        private static readonly string DeckTrackerVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        private static readonly HashSet<Thread> Threads = new HashSet<Thread>();

        public static void Initialize()
        {
            GameMessageDispatcher.OnGameMessage += OnGameMessage;
        }

        private static void OnGameMessage(GameMessage gameMessage)
        {
            if (gameMessage.MessageType != MessageType.Replay) return;

            if (Logger.DebugMode) {
                using (var writer = File.AppendText(ReplaysFile))
                    writer.WriteLine($"{gameMessage.Timestamp:o}|{gameMessage.GameType}|{gameMessage.Message}");
            }

            var thread = new Thread(UploadReplay) {Name = "ReplayUploader"};
            thread.Start(gameMessage);
            Threads.Add(thread);
        }

        public static void Stop()
        {
            foreach (var thread in Threads.ToArray()) {
                thread.Abort();
                while (thread.IsAlive) Thread.Sleep(10);
            }
        }

        private static void UploadReplay(object context)
        {
            try {
                var gameMessage = (GameMessage)context;
                var replay = JsonConvert.DeserializeObject<IEnumerable<JObject>>(gameMessage.Message);
                var game = replay.FirstOrDefault(record => record["type"].Value<string>() == MessageType.Game.ToString())?["message"];
                var gameId = game?["id"]?.Value<string>();
                if (gameId == null) {
                    Logger.LogError($"Unable to find game id in a replay: {gameMessage.Message}");
                    return;
                }
                var gameVersion = game["version"]?.Value<string>();

                byte[] compressedReplay;
                using (var compressor = new Compressor(Compressor.MaxCompressionLevel))
                    compressedReplay = compressor.Compress(Encoding.UTF8.GetBytes(gameMessage.Message));

                var uri = string.Format(ApiRoute, gameMessage.GameType, gameId, DeckTrackerVersion, gameVersion);
                int attempt = 0;
                while (attempt++ < 3) {
                    try {
                        using (var webClient = new WebClient())
                            webClient.UploadData(uri, "PUT", compressedReplay);
                        break;
                    } catch (ThreadInterruptedException) {
                        attempt = int.MaxValue;
                    } catch (WebException) {
                        Thread.Sleep(5000 * attempt * attempt);
                    }
                }
            } catch (ThreadAbortException) {
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            } finally {
                Threads.Remove(Thread.CurrentThread);
            }
        }
    }
}
