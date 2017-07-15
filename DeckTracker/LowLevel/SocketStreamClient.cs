using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DeckTracker.Domain;

namespace DeckTracker.LowLevel
{
    public class SocketStreamClient
    {
        private readonly GameType gameType;
        private readonly int port;

        private Thread socketThread;
        private bool connected;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private readonly ConcurrentDictionary<int, string> responses = new ConcurrentDictionary<int, string>();
        private readonly char[] separator = {'|'};
        private readonly Random random = new Random();

        public SocketStreamClient(GameType gameType, int port)
        {
            this.gameType = gameType;
            this.port = port;
        }

        private void SocketThreadImpl()
        {
            while (true) {
                try {
                    using (var client = new TcpClient {NoDelay = true}) {
                        client.Connect(IPAddress.Loopback, port);
                        using (streamReader = new StreamReader(client.GetStream()))
                        using (streamWriter = new StreamWriter(client.GetStream())) {
                            connected = true;
                            string line;
                            while ((line = ReadLine(streamReader)) != null)
                                ProcessLine(line);
                        }
                    }
                } catch (ThreadAbortException) {
                    break;
                } catch (Exception e) {
                    Logger.LogDebug(gameType, $"SocketStreamClient disconnected: {e.Message}");
                } finally {
                    connected = false;
                    streamReader = null;
                    streamWriter = null;
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void ProcessLine(string line)
        {
            var parts = line.Split(separator, 3);
            var gameMessage = new GameMessage {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(parts[0])).UtcDateTime,
                GameType = gameType,
                MessageType = (MessageType)Enum.Parse(typeof(MessageType), parts[1]),
                Message = parts[2]
            };
            if (gameMessage.MessageType == MessageType.Command) {
                parts = gameMessage.Message.Split(separator, 2);
                var id = int.Parse(parts[0]);
                responses[id] = parts[1];
            } else {
                GameMessageDispatcher.Dispatch(gameMessage);
            }
        }

        private static string ReadLine(TextReader reader)
        {
            var task = reader.ReadLineAsync();
            while (!task.Wait(100)) {}
            return task.Result;
        }

        private bool SendMessage(string message)
        {
            if (!connected)
                return false;
            try {
                streamWriter.WriteLine(message);
                streamWriter.Flush();
                return true;
            } catch {
                return false;
            }
        }

        public Task<string> SendCommand(CommandType command, string parameter, CancellationToken token)
        {
            return Task.Run(() => {
                int id = random.Next(1, int.MaxValue);
                string message = parameter != null ? $"{id}|{command}|{parameter}" : $"{id}|{command}";
                bool sent = false;
                while (socketThread != null && !token.IsCancellationRequested) {
                    if (!sent) sent = SendMessage(message);
                    if (responses.TryRemove(id, out string response))
                        return response;
                    Thread.Sleep(10);
                }
                Logger.LogDebug(gameType, $"SocketStreamClient.SendCommand({command}) timeout, sent={sent} connected={connected}");
                return null;
            }, token);
        }

        public void Start()
        {
            if (socketThread == null) {
                Logger.LogDebug(gameType, "SocketStreamClient.Start");
                socketThread = new Thread(SocketThreadImpl) {Name = "SocketStreamClient"};
                socketThread.Start();
            }
        }

        public void Stop()
        {
            if (socketThread != null) {
                Logger.LogDebug(gameType, "SocketStreamClient.Stop");
                connected = false;
                socketThread.Abort();
                while (socketThread.IsAlive) Thread.Sleep(10);
                socketThread = null;
            }
        }
    }
}
