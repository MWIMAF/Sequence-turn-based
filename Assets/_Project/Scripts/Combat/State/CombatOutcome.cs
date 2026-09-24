namespace Game.Combat
{
    public enum CombatOutcome
    {
        Ongoing = 0,
        PlayerWon = 1,
        EnemyWon = 2,

        /// <summary>Both combatants reached 0 HP in the same slot.</summary>
        Draw = 3,
    }
}
