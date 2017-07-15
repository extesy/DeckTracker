using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DeckTracker.LowLevel
{
    internal static class GameMessageDispatcher
    {
        private static readonly ConcurrentQueue<GameMessage> DispatchQueue = new ConcurrentQueue<GameMessage>();
        private static readonly Thread DispatchThread = new Thread(DispatchThreadImpl) {Name = "GameMessageDispatcher"};

        private static void DispatchThreadImpl()
        {
            while (true) {
                if (!DispatchQueue.TryDequeue(out GameMessage entry)) {
                    Thread.Sleep(10);
                    continue;
                }
                try {
                    OnGameMessage?.Invoke(entry);
                } catch (Exception e) {
                    Logger.LogError(e.ToString());
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public delegate void OnGameMessageHandler(GameMessage gameMessage);
        public static event OnGameMessageHandler OnGameMessage;

        public static void Dispatch(GameMessage gameMessage)
        {
            DispatchQueue.Enqueue(gameMessage);
        }

        public static void Start()
        {
            DispatchThread.Start();
        }

        public static void Stop()
        {
            DispatchThread.Abort();
            while (DispatchThread.IsAlive) Thread.Sleep(10);
        }
    }
}
