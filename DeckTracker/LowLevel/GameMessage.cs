using System;
using DeckTracker.Domain;

namespace DeckTracker.LowLevel
{
    public struct GameMessage
    {
        public DateTime Timestamp;
        public GameType GameType;
        public MessageType MessageType;
        public string Message;
    }
}
