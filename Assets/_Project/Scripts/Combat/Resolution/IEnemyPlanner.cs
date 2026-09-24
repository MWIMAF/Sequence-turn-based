namespace Game.Combat
{
    /// <summary>
    /// Builds a combatant's sequence for the turn. One implementation per enemy archetype (see Game.AI).
    /// </summary>
    /// <remarks>
    /// Planning is blind: the planner only receives CombatState, which never holds the other side's
    /// sequence, and TurnController asks for the enemy plan before the player locks in.
    /// Implementations must be deterministic given state.Rng and must return a sequence that passes
    /// SequenceValidator.
    /// </remarks>
    public interface IEnemyPlanner
    {
        Sequence Plan(CombatState state, CombatantId self);
    }
}
