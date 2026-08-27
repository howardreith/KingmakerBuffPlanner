using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace KingmakerBuffPlanner.Domain.Planning
{
    public enum PowerfulChangeAbilityScore
    {
        None = 0,
        Strength = 1,
        Dexterity = 2,
        Constitution = 3,
        Intelligence = 4,
        Wisdom = 5,
        Charisma = 6
    }

    public enum PowerfulChangeEligibilityStatus
    {
        Eligible,
        Ineligible,
        Blocked
    }

    public sealed class PowerfulChangeEligibility
    {
        internal PowerfulChangeEligibility(
            PowerfulChangeEligibilityStatus status,
            string reason,
            IEnumerable<PowerfulChangeAbilityScore> scores,
            IEnumerable<string> carrierFamilies)
        {
            Status = status;
            Reason = reason ?? string.Empty;
            AbilityScores = new ReadOnlyCollection<PowerfulChangeAbilityScore>(
                (scores ?? new PowerfulChangeAbilityScore[0]).Where(value =>
                        value != PowerfulChangeAbilityScore.None)
                    .Distinct().OrderBy(value => (int)value).ToList());
            CarrierFamilies = new ReadOnlyCollection<string>((carrierFamilies ??
                    new string[0]).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToList());
        }

        public PowerfulChangeEligibilityStatus Status { get; private set; }
        public string Reason { get; private set; }
        public IReadOnlyList<PowerfulChangeAbilityScore> AbilityScores
        { get; private set; }
        public IReadOnlyList<string> CarrierFamilies { get; private set; }
        public bool Eligible { get { return Status ==
            PowerfulChangeEligibilityStatus.Eligible; } }
        public bool Supports(PowerfulChangeAbilityScore score)
        { return Eligible && AbilityScores.Contains(score); }
    }

    public static class PowerfulChangeEligibilityClassifier
    {
        private static readonly HashSet<string> DirectFamilies =
            new HashSet<string>(new[] {
                "AddStatBonus", "AddContextStatBonus", "AddGenericStatBonus",
                "AddStatBonusAbilityValue"
            }, StringComparer.Ordinal);

        public static PowerfulChangeEligibility Classify(
            bool isGenuineSpell,
            bool isTransmutation,
            string sourceSpellbookGuid,
            string requiredSpellbookGuid,
            IEnumerable<string> abilityBonusCarriers,
            IEnumerable<string> appliedBuffGuids)
        {
            if (!isGenuineSpell) return Ineligible("ability-not-genuine-spell");
            if (!isTransmutation) return Ineligible("school-not-transmutation");
            if (string.IsNullOrWhiteSpace(requiredSpellbookGuid) ||
                !string.Equals(sourceSpellbookGuid, requiredSpellbookGuid,
                    StringComparison.Ordinal))
                return Ineligible("spellbook-not-qualified");

            string[] carriers = (abilityBonusCarriers ?? new string[0])
                .Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (carriers.Length == 0)
                return Ineligible("no-positive-ability-score-bonus");

            var scores = new HashSet<PowerfulChangeAbilityScore>();
            var families = new HashSet<string>(StringComparer.Ordinal);
            foreach (string carrier in carriers)
            {
                string family;
                Dictionary<string, string> fields;
                if (!TryParseCarrier(carrier, out family, out fields))
                    return Blocked("bonus-carrier-malformed", scores, families);
                families.Add(family);
                if (family == "ChangeUnitSize") continue;
                if (DirectFamilies.Contains(family))
                {
                    PowerfulChangeAbilityScore score;
                    int value;
                    if (!fields.ContainsKey("Stat") ||
                        !Enum.TryParse(fields["Stat"], false, out score) ||
                        score == PowerfulChangeAbilityScore.None ||
                        !fields.ContainsKey("Value") ||
                        !int.TryParse(fields["Value"], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out value))
                        return Blocked("bonus-carrier-fields-invalid", scores,
                            families);
                    if (value > 0) scores.Add(score);
                    continue;
                }
                if (family == "Polymorph")
                {
                    if (!TryAddPositive(fields, "StrengthBonus",
                            PowerfulChangeAbilityScore.Strength, scores) ||
                        !TryAddPositive(fields, "DexterityBonus",
                            PowerfulChangeAbilityScore.Dexterity, scores) ||
                        !TryAddPositive(fields, "ConstitutionBonus",
                            PowerfulChangeAbilityScore.Constitution, scores))
                        return Blocked("bonus-carrier-fields-invalid", scores,
                            families);
                    continue;
                }
                return Blocked("bonus-carrier-unsupported", scores, families);
            }
            if (scores.Count == 0)
                return Ineligible("no-positive-ability-score-bonus", scores,
                    families);

            string[] buffs = (appliedBuffGuids ?? new string[0]).Where(value =>
                    !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .ToArray();
            if (buffs.Length == 0)
                return Blocked("bonus-applied-buff-missing", scores, families);
            if (buffs.Any(value => !ValidGuid(value)))
                return Blocked("bonus-applied-buff-malformed", scores, families);
            return new PowerfulChangeEligibility(
                PowerfulChangeEligibilityStatus.Eligible,
                "supported-positive-ability-score-bonus", scores, families);
        }

        private static bool TryAddPositive(
            IDictionary<string, string> fields,
            string fieldName,
            PowerfulChangeAbilityScore score,
            ISet<PowerfulChangeAbilityScore> scores)
        {
            string text;
            int value;
            if (!fields.TryGetValue(fieldName, out text)) return true;
            if (!int.TryParse(text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value)) return false;
            if (value > 0) scores.Add(score);
            return true;
        }

        private static bool TryParseCarrier(string value, out string family,
            out Dictionary<string, string> fields)
        {
            family = string.Empty;
            fields = new Dictionary<string, string>(StringComparer.Ordinal);
            int equals = value.IndexOf('=');
            int open = value.IndexOf('{', equals + 1);
            int close = value.LastIndexOf('}');
            if (equals <= 0 || open <= equals + 1 || close <= open ||
                close != value.Length - 1) return false;
            string type = value.Substring(equals + 1, open - equals - 1);
            int dot = type.LastIndexOf('.');
            family = dot < 0 ? type : type.Substring(dot + 1);
            if (string.IsNullOrWhiteSpace(family)) return false;
            string body = value.Substring(open + 1, close - open - 1);
            if (body.Length == 0) return family == "ChangeUnitSize";
            foreach (string pair in body.Split(','))
            {
                int separator = pair.IndexOf('=');
                if (separator <= 0 || separator == pair.Length - 1)
                    return false;
                string name = pair.Substring(0, separator);
                string text = pair.Substring(separator + 1);
                if (fields.ContainsKey(name)) return false;
                fields.Add(name, text);
            }
            return true;
        }

        private static bool ValidGuid(string value)
        {
            if (value == null || value.Length != 32) return false;
            return value.All(character => (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f'));
        }

        private static PowerfulChangeEligibility Ineligible(string reason,
            IEnumerable<PowerfulChangeAbilityScore> scores = null,
            IEnumerable<string> families = null)
        {
            return new PowerfulChangeEligibility(
                PowerfulChangeEligibilityStatus.Ineligible, reason, scores,
                families);
        }

        private static PowerfulChangeEligibility Blocked(string reason,
            IEnumerable<PowerfulChangeAbilityScore> scores,
            IEnumerable<string> families)
        {
            return new PowerfulChangeEligibility(
                PowerfulChangeEligibilityStatus.Blocked, reason, scores,
                families);
        }
    }
}
