using System.Collections.Generic;
using System.Collections.ObjectModel;
using KingmakerBuffPlanner.Domain.Planning;

namespace KingmakerBuffPlanner.Compatibility
{
    internal sealed class BrownFurPowerfulChangeToggleContract
    {
        internal BrownFurPowerfulChangeToggleContract(
            PowerfulChangeAbilityScore score,
            string activatableGuid,
            string markerBuffGuid)
        {
            Score = score;
            ActivatableGuid = activatableGuid;
            MarkerBuffGuid = markerBuffGuid;
        }

        internal PowerfulChangeAbilityScore Score { get; private set; }
        internal string ActivatableGuid { get; private set; }
        internal string MarkerBuffGuid { get; private set; }
    }

    /// <summary>
    /// Centralized identities for the installed, independently optional
    /// Brown-Fur provider. The score intent has no semantic blueprint component:
    /// the provider itself keys its cast transaction by these stable activatable
    /// identities. Every match is additionally checked against the native marker
    /// buff and reservoir component shape before it is used.
    /// </summary>
    internal static class BrownFurPowerfulChangeProfile
    {
        internal const string FeatureGuid =
            "b3bbed7e12463e4c434cd81eda7ab2dd";
        internal const string CastingSpellbookGuid =
            "0c21cfcab6ce4395bd4df330ab3cf715";
        internal const string ReservoirGuid =
            "3b775ee982444493b3de8f7bc31bd872";

        private static readonly ReadOnlyCollection<
            BrownFurPowerfulChangeToggleContract> Values =
            new ReadOnlyCollection<BrownFurPowerfulChangeToggleContract>(
                new List<BrownFurPowerfulChangeToggleContract> {
                    new BrownFurPowerfulChangeToggleContract(
                        PowerfulChangeAbilityScore.Strength,
                        "16c06d016437be9e9e6dac6211ff30a5",
                        "958e93bc70e6ae048e2e96193423915a"),
                    new BrownFurPowerfulChangeToggleContract(
                        PowerfulChangeAbilityScore.Dexterity,
                        "d1f274d1a129eedd8ef44efdb3426d7f",
                        "aba507d99e1b4d6c6bda9233f708eb64"),
                    new BrownFurPowerfulChangeToggleContract(
                        PowerfulChangeAbilityScore.Constitution,
                        "434573bfac3915b1a611a1452917d1d9",
                        "cea64eb942b294360344824a3795a351"),
                    new BrownFurPowerfulChangeToggleContract(
                        PowerfulChangeAbilityScore.Intelligence,
                        "bbef0eaabb277fcf2cbb22a82076e4f7",
                        "5bb5dd956df4d7bc2cf03e02bbd28d5f"),
                    new BrownFurPowerfulChangeToggleContract(
                        PowerfulChangeAbilityScore.Wisdom,
                        "2e7cfb55db278e75a7bca01ac52e4100",
                        "81ce31c8f868e0db5c4aa8a8e9cf1656"),
                    new BrownFurPowerfulChangeToggleContract(
                        PowerfulChangeAbilityScore.Charisma,
                        "deac03b22537cb6f05c8323a384e9b93",
                        "9fe5998e93963fec5ae91aed6a060ef0")
                });

        internal static IReadOnlyList<BrownFurPowerfulChangeToggleContract>
            Toggles { get { return Values; } }

        internal static BrownFurPowerfulChangeToggleContract Find(
            string activatableGuid)
        {
            foreach (BrownFurPowerfulChangeToggleContract value in Values)
                if (value.ActivatableGuid == activatableGuid) return value;
            return null;
        }

        internal static string EnhancementId(string casterUnitId,
            string activatableGuid)
        {
            return "class-feature|" + casterUnitId + "|" + activatableGuid;
        }

        internal static string UsagePoolId(string casterUnitId)
        {
            return "class-feature-resource|" + casterUnitId + "|" +
                ReservoirGuid;
        }
    }
}
