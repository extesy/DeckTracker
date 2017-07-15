namespace DeckTracker.Domain.Eternal.Extensions
{
    public static class Extensions
    {
        public static string FriendlyName(this GameMode gameMode)
        {
            switch (gameMode) {
                case GameMode.Campaign: return "Campaign";
                case GameMode.Casual: return "Casual";
                case GameMode.Draft: return "Draft";
                case GameMode.Forge: return "Forge";
                case GameMode.Gauntlet: return "Gauntlet";
                case GameMode.Ranked: return "Ranked";
                case GameMode.Demo: return "Demo";
                case GameMode.Challenge: return "Challenge";
                case GameMode.Story: return "Story";
                case GameMode.Event: return "Event";
                default: return null;
            }
        }
    }
}
