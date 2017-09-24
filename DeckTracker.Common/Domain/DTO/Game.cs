using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeckTracker.Domain.DTO
{
    public sealed class Game
    {
        public long Id { get; set; }
        public GameType GameType { get; set; }
        [Required] public string ExternalId { get; set; } // game-specific game id
        public byte Mode { get; set; }
        [Column(TypeName = "timestamptz")] public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int? Season { get; set; }
        public PlayerSide Starter { get; set; }
        public PlayerSide Winner { get; set; }
        public bool IsConcede { get; set; }
        public int TurnCount { get; set; }
        //public long Player1DeckId { get; set; }
        [Required] public PlayDeck Player1Deck { get; set; }
        //public long Player2DeckId { get; set; }
        [Required] public PlayDeck Player2Deck { get; set; }
    }
}
