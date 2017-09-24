using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeckTracker.Domain.DTO
{
    public sealed class Card
    {
        public int Id { get; set; }
        public GameType GameType { get; set; }
        [Required] public string ExternalId { get; set; } // game-specific card id
        [Required] public int ExternalSetId { get; set; } // game-specific set id
        public int? Number { get; set; }
        [Required] public string Name { get; set; }
        public byte Type { get; set; } // unit, power, spell, weapon
        public string[] SubTypes { get; set; } // [] race, faction
        public byte Rarity { get; set; }
        public byte Class { get; set; } // bit-mask of card colors
        public byte? Strength { get; set; }
        public byte? Health { get; set; }
        public byte ManaCost { get; set; }
        public int[] ColorCost { get; set; } // [] color requirements
        public string[] Keywords { get; set; } // []
        public string[] Abilities { get; set; } // []
        public bool IsPremium { get; set; }
        public Card ParentCard { get; set; }
        [Column(TypeName = "jsonb")] public string Attributes { get; set; } // all custom attributes
    }
}
