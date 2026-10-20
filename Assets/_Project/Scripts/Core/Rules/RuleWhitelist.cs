using System;
using System.Collections.Generic;

namespace AlgoRhythm.Core.Rules
{
    /// <summary>
    /// Compatibility whitelist stored as data, never as stacked ifs.
    /// Inject a restricted table in tests to exercise E003.
    /// </summary>
    public sealed class RuleWhitelist
    {
        private readonly HashSet<int> _allowed;

        public RuleWhitelist(IEnumerable<(Condition, Target, RuleAction)> allowed)
        {
            if (allowed == null)
            {
                throw new ArgumentNullException(nameof(allowed));
            }

            _allowed = new HashSet<int>();
            foreach ((Condition cond, Target tgt, RuleAction act) in allowed)
            {
                _allowed.Add(Encode(cond, tgt, act));
            }
        }

        /// <summary>Demo table: WHEN_BEAT takes N/E/S/W; WHEN_PROJECTILE takes N/E/S/W/NEAREST.</summary>
        public static RuleWhitelist Demo { get; } = new RuleWhitelist(BuildDemoTable());

        public bool Allows(Condition cond, Target tgt, RuleAction act)
        {
            return _allowed.Contains(Encode(cond, tgt, act));
        }

        public int Count => _allowed.Count;

        private static int Encode(Condition cond, Target tgt, RuleAction act)
        {
            return ((int)cond * 16 + (int)tgt) * 8 + (int)act;
        }

        private static IEnumerable<(Condition, Target, RuleAction)> BuildDemoTable()
        {
            var table = new List<(Condition, Target, RuleAction)>(9);
            foreach (Target tgt in new[] { Target.North, Target.East, Target.South, Target.West })
            {
                table.Add((Condition.WhenBeat, tgt, RuleAction.RaiseShield));
            }

            foreach (Target tgt in new[]
                     {
                         Target.North, Target.East, Target.South, Target.West, Target.Nearest
                     })
            {
                table.Add((Condition.WhenProjectile, tgt, RuleAction.RaiseShield));
            }

            return table;
        }
    }
}
