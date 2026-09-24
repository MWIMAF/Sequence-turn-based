using System.Collections.Generic;
using System.Text;
using Game.Combat;

namespace Game.Editor
{
    /// <summary>
    /// Turns CombatEvents into a readable text log, for the console simulator and debugging.
    /// </summary>
    public static class CombatLogFormatter
    {
        public static string Format(CombatEvent e)
        {
            string slot = e.Slot == CombatEvent.NoSlot ? "     " : $"S{e.Slot + 1}  ";
            switch (e)
            {
                case ClashResolvedEvent clash:
                    return slot + $"{Side(clash.Player)}  vs  {Side(clash.Enemy)}";
                case MoveCancelledEvent cancel:
                    return slot + $"{cancel.Who}'s {cancel.CancelledMove.DisplayName} is cancelled ({cancel.Cause})";
                case StatusAppliedEvent applied:
                    return slot + $"{applied.Who} becomes {applied.Status} (next {applied.DurationSlots} slot(s))";
                case StatusExpiredEvent expired:
                    return slot + $"{expired.Who} is no longer {expired.Status}";
                case StaminaChangedEvent stamina:
                    return slot + $"{stamina.Who} stamina {(stamina.Delta >= 0 ? "+" : "")}{stamina.Delta} → {stamina.StaminaAfter} ({stamina.Reason})";
                case CombatantDefeatedEvent defeated:
                    return slot + $"*** {defeated.Who} is defeated ***";
                default:
                    return slot + e.GetType().Name;
            }
        }

        public static string Format(IEnumerable<CombatEvent> events)
        {
            var sb = new StringBuilder();
            foreach (var e in events)
            {
                sb.AppendLine(Format(e));
            }

            return sb.ToString();
        }

        private static string Side(SlotOutcome o)
        {
            var sb = new StringBuilder();
            sb.Append($"{o.Who} {o.Move.DisplayName} [{o.Label}]");
            if (o.DamageTaken > 0)
            {
                sb.Append($" -{o.DamageTaken}HP");
                if (o.ExposedBonusTaken > 0) sb.Append($" (incl. +{o.ExposedBonusTaken} Exposed)");
            }

            if (o.StaminaGained > 0) sb.Append($" +{o.StaminaGained}STA");
            sb.Append($" → {o.HpAfter}HP/{o.StaminaAfter}STA");
            return sb.ToString();
        }
    }
}
