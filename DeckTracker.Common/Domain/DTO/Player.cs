using System.ComponentModel.DataAnnotations;

namespace DeckTracker.Domain.DTO
{
    public sealed class Player
    {
        public long Id { get; set; }
        public GameType GameType { get; set; }
        [Required] public string ExternalId { get; set; } // game-specific player id
        [Required] public string Name { get; set; }
        public bool IsAI { get; set; }
    }
}
