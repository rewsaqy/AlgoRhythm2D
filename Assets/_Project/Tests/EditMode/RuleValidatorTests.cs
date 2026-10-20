using System.Collections.Generic;
using AlgoRhythm.Core.Rules;
using NUnit.Framework;

namespace AlgoRhythm.Tests
{
    public sealed class RuleValidatorTests
    {
        private static RuleValidator Validator(RuleWhitelist whitelist = null, int cost = 1, int cooldown = 0)
        {
            return new RuleValidator(whitelist ?? RuleWhitelist.Demo, cost, cooldown);
        }

        private static RuleValidationContext Context(int energy = 6, int cooldownRemainingMs = 0)
        {
            return new RuleValidationContext(energy, cooldownRemainingMs);
        }

        [Test]
        public void DemoWhitelist_HasNineCombinations()
        {
            Assert.AreEqual(9, RuleWhitelist.Demo.Count);
        }

        [Test]
        public void AllDemoCombinations_AreValid_WithDistinctPreviewKeys()
        {
            var keys = new HashSet<string>();
            int valid = 0;

            foreach (Condition cond in new[] { Condition.WhenBeat, Condition.WhenProjectile })
            {
                foreach (Target tgt in new[]
                         {
                             Target.North, Target.East, Target.South, Target.West, Target.Nearest
                         })
                {
                    var draft = new RuleDraft(cond, tgt, RuleAction.RaiseShield);
                    RuleValidationResult result = Validator().Validate(draft, Context());

                    bool whitelisted = cond == Condition.WhenProjectile || tgt != Target.Nearest;
                    if (!whitelisted)
                    {
                        Assert.AreEqual(RuleError.E002_WrongCategory, result.Error);
                        continue;
                    }

                    Assert.IsTrue(result.IsValid, $"{cond}+{tgt}");
                    Assert.IsTrue(keys.Add(result.PreviewKey), $"duplicate key {result.PreviewKey}");
                    valid++;
                }
            }

            Assert.AreEqual(9, valid);
        }

        [Test]
        public void PreviewKey_UsesUpperSnakeFormat()
        {
            Assert.AreEqual(
                "WHEN_PROJECTILE_EAST_RAISE_SHIELD",
                RuleValidator.PreviewKeyFor(
                    Condition.WhenProjectile, Target.East, RuleAction.RaiseShield));
        }

        [Test]
        public void EmptyCondition_ReturnsE001()
        {
            var draft = new RuleDraft(null, Target.East, RuleAction.RaiseShield);

            RuleValidationResult result = Validator().Validate(draft, Context());

            Assert.AreEqual(RuleError.E001_EmptySlot, result.Error);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void EmptyTarget_ReturnsE001()
        {
            var draft = new RuleDraft(Condition.WhenProjectile, null, RuleAction.RaiseShield);

            RuleValidationResult result = Validator().Validate(draft, Context());

            Assert.AreEqual(RuleError.E001_EmptySlot, result.Error);
        }

        [Test]
        public void EmptyAction_ReturnsE001()
        {
            var draft = new RuleDraft(Condition.WhenProjectile, Target.East, null);

            RuleValidationResult result = Validator().Validate(draft, Context());

            Assert.AreEqual(RuleError.E001_EmptySlot, result.Error);
        }

        [Test]
        public void BeatWithNearest_ReturnsE002()
        {
            var draft = new RuleDraft(Condition.WhenBeat, Target.Nearest, RuleAction.RaiseShield);

            RuleValidationResult result = Validator().Validate(draft, Context());

            Assert.AreEqual(RuleError.E002_WrongCategory, result.Error);
        }

        [Test]
        public void CombinationMissingFromWhitelist_ReturnsE003()
        {
            var restricted = new RuleWhitelist(new[]
            {
                (Condition.WhenProjectile, Target.North, RuleAction.RaiseShield)
            });
            var draft = new RuleDraft(
                Condition.WhenProjectile, Target.East, RuleAction.RaiseShield);

            RuleValidationResult result = Validator(restricted).Validate(draft, Context());

            Assert.AreEqual(RuleError.E003_Unsupported, result.Error);
        }

        [Test]
        public void InsufficientEnergy_ReturnsE004()
        {
            var draft = new RuleDraft(
                Condition.WhenProjectile, Target.East, RuleAction.RaiseShield);

            RuleValidationResult result = Validator(cost: 1).Validate(draft, Context(energy: 0));

            Assert.AreEqual(RuleError.E004_NotEnoughEnergy, result.Error);
        }

        [Test]
        public void CooldownRemaining_ReturnsE005()
        {
            var draft = new RuleDraft(
                Condition.WhenProjectile, Target.East, RuleAction.RaiseShield);

            RuleValidationResult result =
                Validator().Validate(draft, Context(energy: 6, cooldownRemainingMs: 100));

            Assert.AreEqual(RuleError.E005_Cooldown, result.Error);
        }

        [Test]
        public void EnergyCheckedBeforeCooldown_E004Wins()
        {
            var draft = new RuleDraft(
                Condition.WhenProjectile, Target.East, RuleAction.RaiseShield);

            RuleValidationResult result =
                Validator().Validate(draft, Context(energy: 0, cooldownRemainingMs: 100));

            Assert.AreEqual(RuleError.E004_NotEnoughEnergy, result.Error);
        }
    }
}
