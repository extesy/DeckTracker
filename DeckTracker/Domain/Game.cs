using System;
using PropertyChanged;

namespace DeckTracker.Domain
{
    public class Game
    {
        public GameType GameType;
        public string Type;
        public string Mode;
        public string Id;
        public bool Win;
        public Deck PlayerDeck, OpponentDeck;
        public DateTime Start, End;
    }

    [AddINotifyPropertyChangedInterface]
    public class GameMode
    {
        public bool IsEnabled { get; set; }
        public string Name { get; set; }
    }
}
