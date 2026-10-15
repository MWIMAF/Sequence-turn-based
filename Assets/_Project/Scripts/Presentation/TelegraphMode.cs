namespace Game.Presentation
{
    /// <summary>
    /// How much of the enemy's locked plan the player sees while planning.
    /// A test toggle: CLAUDE.md lists telegraphing as an open design question.
    /// </summary>
    public enum TelegraphMode
    {
        Hidden = 0,
        FirstSlot = 1,
        OneRandomSlot = 2,
        Full = 3,
    }
}
