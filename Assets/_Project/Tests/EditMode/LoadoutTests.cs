using System.Linq;
using NUnit.Framework;

namespace Game.Combat.Tests
{
    /// <summary>Equipment on five body parts gives the player's five actions.</summary>
    public sealed class LoadoutTests
    {
        [Test]
        public void GetActions_ReturnsOneActionPerBodyPart_InOrder()
        {
            using (var w = new TestWorld())
            {
                var player = Player(w, out _);

                var actions = player.GetActions();

                Assert.That(actions.Select(a => a.Move.DisplayName), Is.EqualTo(new[] { "Slash", "Parry", "Rest", "Kick", "Evade" }));
                Assert.That(actions.Select(a => a.BodyPart), Is.EqualTo(BodyParts.All));
            }
        }

        [Test]
        public void ActionNotOfferedByTheEquipment_FallsBackToItsFirstAction()
        {
            using (var w = new TestWorld())
            {
                var sword = w.Own(EquipmentDefinition.CreateRuntime("Sword", EquipSlot.Hand, w.Slash, w.Parry));
                var slot = new LoadoutSlot(BodyPart.RightHand, sword, w.Rest);

                Assert.That(slot.Action, Is.SameAs(w.Slash));
                Assert.That(slot.Problem, Does.Contain("doesn't offer"));
            }
        }

        [Test]
        public void EmptyBodyPart_GivesNoAction()
        {
            using (var w = new TestWorld())
            {
                var player = Player(w, out var loadout);
                loadout[2] = new LoadoutSlot(BodyPart.Body);
                player.Configure("You", CombatStats.Create(), loadout);

                Assert.That(player.GetActions().Count, Is.EqualTo(4));
            }
        }

        [Test]
        public void WrongGearForTheBodyPart_IsReported()
        {
            using (var w = new TestWorld())
            {
                var boots = w.Own(EquipmentDefinition.CreateRuntime("Boots", EquipSlot.Leg, w.Kick));
                Assert.That(new LoadoutSlot(BodyPart.LeftHand, boots).Problem, Does.Contain("Leg gear"));
            }
        }

        [Test]
        public void CombatStateCreate_LimitsThePlayerToTheLoadout_AndFreesTheEnemy()
        {
            using (var w = new TestWorld())
            {
                var player = Player(w, out _);
                var state = CombatState.Create(player, "Grunt", CombatStats.Create(15), EnemyTier.Alpha, 7);

                Assert.That(state.Player.AvailableActions.Count, Is.EqualTo(5));
                Assert.That(state.Player.UsesStamina, Is.True);
                Assert.That(state.Enemy.AvailableActions, Is.Null);
                Assert.That(state.Enemy.UsesStamina, Is.False);
                Assert.That(state.EnemyTier, Is.EqualTo(EnemyTier.Alpha));
            }
        }

        private static PlayerDefinition Player(TestWorld w, out LoadoutSlot[] loadout)
        {
            var sword = w.Own(EquipmentDefinition.CreateRuntime("Sword", EquipSlot.Hand, w.Slash, w.Parry));
            var shield = w.Own(EquipmentDefinition.CreateRuntime("Shield", EquipSlot.Hand, w.Bash, w.Parry));
            var armor = w.Own(EquipmentDefinition.CreateRuntime("Armor", EquipSlot.Body, w.Rest));
            var boots = w.Own(EquipmentDefinition.CreateRuntime("Boots", EquipSlot.Leg, w.Kick, w.Evade));

            loadout = new[]
            {
                new LoadoutSlot(BodyPart.RightHand, sword, w.Slash),
                new LoadoutSlot(BodyPart.LeftHand, shield, w.Parry),
                new LoadoutSlot(BodyPart.Body, armor, w.Rest),
                new LoadoutSlot(BodyPart.RightLeg, boots, w.Kick),
                new LoadoutSlot(BodyPart.LeftLeg, boots, w.Evade),
            };
            return w.Own(PlayerDefinition.CreateRuntime("You", CombatStats.Create(), loadout));
        }
    }
}
