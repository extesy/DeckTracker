using System;

namespace DeckTracker.Domain.TheElderScrollsLegends
{
    public enum GameMode : byte
    {
        Campaign = 1, // gameOptions.gameHandler == HydraCampaign
        Practice = 2, // gameOptions.gameHandler == HydraSolitaire
        CasualBattle = 3, // gameOptions.gameHandler == HydraCasualVersus
        RankedBattle = 4, // gameOptions.gameHandler == HydraVersus
        SoloArena = 5, // gameOptions.gameHandler == HydraDraft
        VersusArena = 6, // gameOptions.gameHandler == HydraConquest
        ChaosArena = 7, // gameOptions.gameHandler == HydraChaosHandler
        Story = 8, // gameOptions.gameHandler == HydraStory
        Gauntlet = 9 // gameOptions.tournamentDefID != null
    }

    public static partial class Helpers
    {
        public static GameMode GameModeFromType(string type)
        {
            switch (type) {
                case "HydraCampaign": return GameMode.Campaign;
                case "HydraSolitaire": return GameMode.Practice;
                case "HydraCasualVersus": return GameMode.CasualBattle;
                case "HydraVersus": return GameMode.RankedBattle;
                case "HydraDraft": return GameMode.SoloArena;
                case "HydraConquest": return GameMode.VersusArena;
                case "HydraChaosHandler": return GameMode.ChaosArena;
                case "HydraStory": return GameMode.Story;
                case "HydraTournament": return GameMode.Gauntlet;
                default: throw new ArgumentException($@"Unable to convert {type} into GameMode", nameof(type));
            }
        }
    }
}
