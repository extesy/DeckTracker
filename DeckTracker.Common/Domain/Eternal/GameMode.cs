using System;

namespace DeckTracker.Domain.Eternal
{
    public enum GameMode : byte
    {
        Campaign = 1,
        Casual = 2,
        Draft = 3,
        Forge = 4,
        Gauntlet = 5,
        Ranked = 6,
        Demo = 7,
        Challenge = 8,
        Story = 9,
        Event = 10
    }

    public static partial class Helpers
    {
        public static GameMode GameModeFromType(string type)
        {
            switch (type) {
                case "Campaign": return GameMode.Campaign;
                case "Casual": return GameMode.Casual;
                case "Draft": return GameMode.Draft;
                case "Forge": return GameMode.Forge;
                case "Survival": return GameMode.Gauntlet;
                case "Versus": return GameMode.Ranked;
                case "FriendChallenge": return GameMode.Challenge;
                case "MicroCampaign": return GameMode.Story;
                case "Event": return GameMode.Event;
                default: throw new ArgumentException($@"Unable to convert {type} into GameMode", nameof(type));
            }
        }
    }
}
