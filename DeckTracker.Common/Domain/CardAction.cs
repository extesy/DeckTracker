namespace DeckTracker.Domain
{
    public enum CardAction : byte
    {
        Draw = 1,
        Mulligan = 2,
        Discard = 3,
        Play = 4,
        Die = 5,
        ReturnToHand = 6,
        ReturnToDeck = 7,
        Resurrect = 8,
        Attack = 9,
        Block = 10,
        Prophecy = 11
    }
}
