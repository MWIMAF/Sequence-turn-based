namespace Game.Combat
{
    /// <summary>
    /// Physical or magical, per the GDD's hand actions. Data only for now: both are reduced by Defense
    /// until the GDD defines magic defense.
    /// </summary>
    public enum DamageKind
    {
        Physical = 0,
        Magical = 1,
    }
}
