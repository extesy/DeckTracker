using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using DeckTracker.Domain;

namespace DeckTracker.LowLevel
{
    public class SocketStreamClientRx
    {
        private readonly GameType gameType;
        private readonly int port;

        private IObservable<ConnectionContext> connection;
        private IObservable<GameMessage> gameMessages;
        private IDisposable gameMessagesDispatcher;
        private readonly char[] separator = {'|'};
        private readonly Random random = new Random();

        public SocketStreamClientRx(GameType gameType, int port)
        {
            this.gameType = gameType;
            this.port = port;
        }

        private class ConnectionContext : IDisposable
        {
            public TcpClient TcpClient;
            public StreamReader StreamReader;
            public StreamWriter StreamWriter;

            public void Dispose()
            {
                if (TcpClient == null) return;
                TcpClient.Dispose();
                TcpClient = null;
                if (StreamReader == null && StreamWriter == null) return;
                StreamReader?.Dispose();
                StreamReader = null;
                StreamWriter?.Dispose();
                StreamWriter = null;
            }
        }

        public void Start()
        {
            connection = Observable.Using(
                () => new ConnectionContext {TcpClient = new TcpClient {NoDelay = true}},
                conn => conn.TcpClient.ConnectAsync(IPAddress.Loopback, port).ToObservable().Select(_ => {
                    conn.StreamReader = new StreamReader(conn.TcpClient.GetStream());
                    conn.StreamWriter = new StreamWriter(conn.TcpClient.GetStream());
                    return conn;
                }))
                .Repeat()
                .Publish()
                .RefCount();

            gameMessages = connection
                .SelectMany(c => c.StreamReader.ReadLineAsync().ToObservable().Repeat().TakeWhile(x => x != null))
                .Select(line => {
                    var parts = line.Split(separator, 3);
                    return new GameMessage {
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(parts[0])).UtcDateTime,
                        GameType = gameType,
                        MessageType = (MessageType)Enum.Parse(typeof(MessageType), parts[1]),
                        Message = parts[2]
                    };
                });

            gameMessagesDispatcher = gameMessages.Retry().Subscribe(GameMessageDispatcher.Dispatch);
        }

        public void Stop()
        {
            gameMessagesDispatcher?.Dispose();
            gameMessagesDispatcher = null;
            gameMessages = null;
            connection = null;
        }

        public Task<string> SendCommand(string command, CancellationToken token)
        {
            int id = random.Next(1, int.MaxValue);
            string message = $"{id}|{command}";
            return connection.SelectMany(c => c.StreamWriter.WriteLineAsync(message).ToObservable().Select(_ => c))
                .SelectMany(c => c.StreamWriter.FlushAsync().ToObservable().Select(_ => c))
                .SelectMany(c => gameMessages.FirstAsync(s => s.MessageType == MessageType.Command && s.Message.StartsWith($"{id}|")).Select(gm => gm.Message))
                .Retry()
                .ToTask(token);
        }
    }
}
