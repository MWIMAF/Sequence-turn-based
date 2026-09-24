namespace Game.Combat
{
    public enum TurnPhase
    {
        NotStarted = 0,
        TurnStart = 1,
        Planning = 2,
        LockIn = 3,
        Resolution = 4,
        Playback = 5,
        TurnEnd = 6,
        CombatOver = 7,
    }
}
