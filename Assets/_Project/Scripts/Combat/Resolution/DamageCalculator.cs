using System;

namespace Game.Combat
{
    /// <summary>
    /// The GDD damage formula, in integers.
    /// </summary>
    /// <remarks>
    /// raw   = attacker.Attack + move.SkillPower
    /// raw  ×= crit multiplier (on a crit), × (100 + attack buff)%, × (100 + stagger bonus)% if the target is Staggered
    /// taken = raw × C / (target.Defense + C), C from the enemy tier (BalanceConfig)
    /// All multipliers are applied in one integer expression and rounded down once, so stacking
    /// percentages doesn't lose damage to repeated rounding. A landed hit deals at least MinimumDamage.
    /// The same C is used in both directions (GDD only specifies damage the player takes).
    /// </remarks>
    public static class DamageCalculator
    {
        private const long Percent = 100;

        public static DamageResult Compute(
            CombatantState attacker, CombatantState target, MoveDefinition move,
            bool isCrit, bool targetStaggered, int defenseConstant, BalanceConfig balance)
        {
            long raw = attacker.Attack + move.SkillPower;
            if (raw <= 0)
            {
                return DamageResult.None;
            }

            int total = Mitigate(raw, attacker, target, isCrit, targetStaggered, defenseConstant, balance);
            int withoutStagger = targetStaggered
                ? Mitigate(raw, attacker, target, isCrit, false, defenseConstant, balance)
                : total;

            return new DamageResult(total, isCrit, total - withoutStagger);
        }

        private static int Mitigate(long raw, CombatantState attacker, CombatantState target,
            bool isCrit, bool staggered, int defenseConstant, BalanceConfig balance)
        {
            long crit = isCrit ? attacker.CritMultiplierPercent : Percent;
            long buff = Percent + attacker.AttackBuffPercent;
            long stagger = staggered ? Percent + balance.StaggerDamageBonusPercent : Percent;
            long c = Math.Max(1, defenseConstant);
            long defense = Math.Max(0, target.Defense);

            long numerator = raw * crit * buff * stagger * c;
            long denominator = Percent * Percent * Percent * (defense + c);
            long damage = numerator / denominator;

            return (int)Math.Max(balance.MinimumDamage, damage);
        }
    }
}
