namespace Game.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(int total, bool isCrit, int staggerBonus)
        {
            Total = total;
            IsCrit = isCrit;
            StaggerBonus = staggerBonus;
        }

        public static DamageResult None => new DamageResult(0, false, 0);

        public int Total { get; }
        public bool IsCrit { get; }

        /// <summary>How much of Total came from the target being Staggered.</summary>
        public int StaggerBonus { get; }
    }
}
