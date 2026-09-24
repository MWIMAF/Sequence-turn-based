namespace Game.Combat
{
    /// <summary>
    /// Which IEnemyPlanner an enemy uses. Implementations live in Game.AI (see EnemyPlannerFactory).
    /// </summary>
    public enum EnemyPlannerKind
    {
        Random = 0,
    }
}
