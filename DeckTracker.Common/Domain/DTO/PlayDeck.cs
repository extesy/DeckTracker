using System.ComponentModel.DataAnnotations;

namespace DeckTracker.Domain.DTO
{
    public class PlayDeckBase
    {
        [Required] public string ShortId { get; set; }
        public int[] CardIds { get; set; } // [] full deck (if known)
        public int Class { get; set; } // bit-mask of card colors
        public string[] Archetypes { get; set; } // []
    }

    public sealed class PlayDeck : PlayDeckBase
    {
        public long Id { get; set; }
        public GameType GameType { get; set; }
        public string ExternalId { get; set; } // game-specific deck id
        [Required] public Player Player { get; set; }
        public int? PlayerRank { get; set; } // negative - below legend, positive - legends
        public int? PlayerScore { get; set; }
        [Required] public int[] PlayedCardIds { get; set; } // []
        public bool IsComplete { get; set; }
        public string Name { get; set; }
    }

    public sealed class PlayDeckGroup : PlayDeckBase
    {
        public int Count { get; set; }
    }
}
