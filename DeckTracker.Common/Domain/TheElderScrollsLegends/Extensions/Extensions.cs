namespace DeckTracker.Domain.TheElderScrollsLegends.Extensions
{
    public static class Extensions
    {
        public static string FriendlyName(this GameMode gameMode)
        {
            switch (gameMode) {
                case GameMode.Campaign: return "Campaign";
                case GameMode.Practice: return "Practice";
                case GameMode.CasualBattle: return "Casual";
                case GameMode.RankedBattle: return "Ranked";
                case GameMode.SoloArena: return "Solo Arena";
                case GameMode.VersusArena: return "Versus Arena";
                case GameMode.ChaosArena: return "Chaos Arena";
                case GameMode.Story: return "Story";
                case GameMode.Gauntlet: return "Gauntlet";
                default: return null;
            }
        }
    }
}
