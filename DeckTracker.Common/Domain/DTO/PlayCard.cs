using System.ComponentModel.DataAnnotations;

namespace DeckTracker.Domain.DTO
{
    public sealed class PlayCard
    {
        public long Id { get; set; }
        public GameType GameType { get; set; }
        [Required] public PlayDeck Deck { get; set; }
        [Required] public Card Card { get; set; }
        public int Turn { get; set; }
        public CardAction Action { get; set; }
        public int[] TargetCardIds { get; set; } // [] null if targeting face or if n/a
    }
}
