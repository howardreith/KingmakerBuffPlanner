using System;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.Domain.Planning
{
    // Which metamagic rods are MEANINGFUL choices for a beneficial buff
    // casting (everyday-use v1.2 §8). This is not applicability (that is
    // CastEnhancementSnapshot.ApplicabilityFailure: white lists, spell
    // level, spellbook) and not affordability (the ledger's business): a
    // rod can be applicable and fundable and still be a meaningless
    // choice for the buff being planned.
    //
    // The policy is structural, keyed on the metamagic MASK - the
    // installed native semantics of the rod - never on display-name
    // substrings:
    //   vanilla Kingmaker:   Empower=1, Maximize=2, Quicken=4, Extend=8,
    //                        Heighten=16, Reach=32 (verified in this
    //                        install's Assembly-CSharp);
    //   Call of the Wild:    Piercing=0x80000, ThrenodicSpell=0x2000,
    //                        Selective=0x2000000, Persistent=0x10000000
    //                        (verified in Compatibility/
    //                        CallOfTheWildMetamagicNames.cs).
    //
    // For the planner's beneficial authoring:
    //   - Piercing / Persistent affect enemy spell-resistance and save
    //     persistence; a beneficial buff cast on allies never makes those
    //     checks. Irrelevant.
    //   - Selective excludes creatures from an area; the planner's area
    //     buffs apply buffs, with nothing to exclude. Irrelevant.
    //   - Quicken buys action economy; the planner's execution model
    //     applies buffs out of combat and never spends action economy, so
    //     there is no verified real use in this version. Not offered.
    //   - Threnodic is conditional on a mind-affecting spell and an undead
    //     recipient; the snapshot does not carry either fact, so the
    //     choice is not verifiable for a given recipient and is not
    //     offered (the reason is recorded, not silently dropped).
    //   - Extend is meaningful exactly when the spell has a real duration
    //     (a positive expected-duration fact on the provider).
    //   - Every other mask (Empower, Maximize, Heighten, Reach, future
    //     values) stays offered: unknown relevance is not hidden as
    //     irrelevant, and a genuinely variable-number buff may still
    //     benefit from it.
    // An already-selected irrelevant option is never stripped by this
    // policy: it stays visible, explained and removable (the session's
    // add path refuses new irrelevant selections; the remove path and the
    // compiler's own validation are unchanged).
    public static class CastEnhancementRelevance
    {
        public const int ExtendMask = 0x8;
        public const int QuickenMask = 0x4;
        public const int PiercingMask = 0x80000;
        public const int ThrenodicMask = 0x2000;
        public const int SelectiveMask = 0x2000000;
        public const int PersistentMask = 0x10000000;

        // Empty when the enhancement is a meaningful offer for this exact
        // provider; otherwise the precise reason it is not offered.
        public static string OfferFailure(CastEnhancementSnapshot enhancement, ProviderSnapshot provider)
        {
            if (enhancement == null) return "enhancement-missing";
            if (enhancement.Category != CastEnhancementCategory.MetamagicRod) return string.Empty;
            int mask = enhancement.MetamagicMask;
            if (mask == PiercingMask)
                return "Piercing helps spells beat spell resistance; a buff cast on allies never checks it.";
            if (mask == PersistentMask)
                return "Persistent rerolls failed saves; a buff on allies asks no save.";
            if (mask == SelectiveMask)
                return "Selective excludes creatures from an area; a buff area has nothing to exclude.";
            if (mask == QuickenMask)
                return "Quicken buys action economy; the planner applies buffs out of combat and never spends it.";
            if (mask == ThrenodicMask)
                return "Threnodic needs a mind-affecting spell and an undead recipient; this recipient is not verified undead.";
            if (mask == ExtendMask)
            {
                int duration = provider == null ? 0 : provider.ExpectedDurationRounds;
                if (duration <= 0)
                    return "Extend needs a spell with a real duration; this one has none.";
                return string.Empty;
            }
            return string.Empty;
        }

        public static bool IsRelevantOffer(CastEnhancementSnapshot enhancement, ProviderSnapshot provider)
        {
            return OfferFailure(enhancement, provider).Length == 0;
        }
    }
}
