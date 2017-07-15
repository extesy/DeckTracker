using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace DeckTracker.Domain
{
    public class FieldComparer<TContainer, TField> : IComparer<TContainer> where TField : IComparable
    {
        private readonly Func<TContainer, TField> accessor;

        public FieldComparer(Func<TContainer, TField> accessor) => this.accessor = accessor;

        public int Compare(TContainer x, TContainer y)
        {
            var a = accessor(x);
            var b = accessor(y);
            if (a == null)
                return b == null ? 0 : 1;
            return b == null ? -1 : a.CompareTo(b);
        }
    }

    public sealed class AgainstDeck : INotifyPropertyChanged
    {
        public string Classification { get; set; }
        public readonly List<Game> Games = new List<Game>();
        public readonly Dictionary<string, int> GamesPlayedByMode = new Dictionary<string, int>();
        public readonly Dictionary<string, int> GamesWonByMode = new Dictionary<string, int>();
//        public List<AgainstDeck> Children { get; } = new List<AgainstDeck>();
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public string WinRate => GamesPlayed != 0 ? Math.Round((double)GamesWon / GamesPlayed * 100) + "% of " + GamesPlayed : null;

//        public int Depth => (Children.Count == 0 ? 0 : Children.Max(r => r.Depth)) + 1;
//        public AgainstDeck Parent;
//        public int Level => Parent?.Level - 1 ?? Depth;

        public string WinRateByMode(string mode)
        {
            if (mode == null) return WinRate;
            if (!GamesPlayedByMode.ContainsKey(mode) || GamesPlayedByMode[mode] == 0) return null;
            return Math.Round((double)GamesWonByMode[mode] / GamesPlayedByMode[mode] * 100) + "% of " + GamesPlayedByMode[mode];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly IComparer<AgainstDeck> OrderByGamesPlayed = new FieldComparer<AgainstDeck, int>(deck => -deck.GamesPlayed);
    }

    public sealed class Deck : INotifyPropertyChanged
    {
        public GameType GameType;
        public string Id;
        public string Name { get; set; }
        public string[] Tags;
        public string[] Cards;
        public string[] Colors;
        public string Classification { get; set; }
        public DateTime LastPlayed { get; set; }
        public ObservableSortedList<AgainstDeck> Stats { get; } = new ObservableSortedList<AgainstDeck>(AgainstDeck.OrderByGamesPlayed);
//        public AgainstDeck TreeStats { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly IComparer<Deck> OrderByLastPlayed = new FieldComparer<Deck, long>(deck => -deck.LastPlayed.Ticks);
    }
}
