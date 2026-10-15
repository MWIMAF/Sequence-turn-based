namespace Game.Combat
{
    /// <summary>
    /// Builds a combatant's sequence for the turn (see Game.AI: PatternEnemyPlanner, RandomEnemyPlanner).
    /// </summary>
    /// <remarks>
    /// Planning is blind: the planner only receives CombatState, which never holds the other side's
    /// sequence, and TurnController asks for the enemy plan before the player locks in.
    /// Implementations must be deterministic (use state.Rng for any randomness) and must return a
    /// sequence that passes SequenceValidator.
    /// </remarks>
    public interface IEnemyPlanner
    {
        Sequence Plan(CombatState state, CombatantId self);
    }
}
