using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using KingmakerBuffPlanner.Compatibility;
using KingmakerBuffPlanner.Domain.Providers;

namespace KingmakerBuffPlanner.GameAdapters
{
    // 0.4.2 (B): reads what every party spellbook holds, straight from the
    // native book (Kingmaker 2.1.7b): a prepared book's memorized slots
    // (GetAllMemorizedSpells - Spellbook.Memorize fills a slot at once and
    // marks it unavailable until rest, Spend marks it unavailable, and
    // ForgetMemorized clears it, so a held spell is any slot whose Spell is
    // set whatever its availability), a spontaneous book's known, special
    // and custom (metamagic) spells. Classification and resources play no
    // part. Anything that could make a read incomplete is reported as an
    // explicit unknown state instead of an empty book.
    internal sealed class KingmakerSpellbookMembershipAdapter
    {
        internal PartySpellbookMembership Capture()
        {
            try
            {
                if (Game.Instance == null || Game.Instance.Player == null)
                    return PartySpellbookMembership.Unstable("player-state-unavailable");
                if (LoadingProcess.Instance != null && LoadingProcess.Instance.IsLoadingInProcess)
                    return PartySpellbookMembership.Unstable("loading-in-process");
                if (NativeServiceWindowShown())
                    return PartySpellbookMembership.Unstable("native-service-window-open");
                bool contractsComplete = KingmakerSpellbookRoleAdapter.OptionalContractsComplete;
                var books = new List<SpellbookMembershipFact>();
                foreach (UnitEntityData unit in PartyUnits())
                    books.AddRange(ReadUnit(unit, contractsComplete));
                return new PartySpellbookMembership(true, null, books, null, AbilityName);
            }
            catch (Exception exception)
            {
                return PartySpellbookMembership.Unstable("membership-read-failed:" +
                    exception.GetType().Name);
            }
        }

        // The spellbook (or any other service window, or level-up) may be
        // mid-edit while it is shown; nothing is concluded then.
        private static bool NativeServiceWindowShown()
        {
            StaticCanvas canvas = StaticCanvas.Instance;
            if (canvas == null) return false;
            return canvas.ServiceWindow != null && canvas.ServiceWindow.WindowTabs != null &&
                canvas.ServiceWindow.WindowTabs.IsShow;
        }

        // The party exactly as discovery enumerates it (members and pets).
        private static IEnumerable<UnitEntityData> PartyUnits()
        {
            var result = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (UnitEntityData unit in Game.Instance.Player.Party ?? new List<UnitEntityData>())
            {
                if (unit == null || string.IsNullOrWhiteSpace(unit.UniqueId)) continue;
                result[unit.UniqueId] = unit;
                UnitEntityData pet = unit.Descriptor == null ? null : unit.Descriptor.Pet;
                if (pet != null && !string.IsNullOrWhiteSpace(pet.UniqueId)) result[pet.UniqueId] = pet;
            }
            return result.Values.OrderBy(unit => unit.UniqueId, StringComparer.Ordinal);
        }

        private static IEnumerable<SpellbookMembershipFact> ReadUnit(UnitEntityData unit,
            bool contractsComplete)
        {
            var facts = new List<SpellbookMembershipFact>();
            if (unit.Descriptor == null) return facts;
            List<Spellbook> spellbooks = unit.Descriptor.Spellbooks
                .Where(book => book != null && book.Blueprint != null)
                .OrderBy(book => book.Blueprint.AssetGuid, StringComparer.Ordinal).ToList();
            IReadOnlyDictionary<string, SpellbookRoleResolution> roles =
                new KingmakerSpellbookRoleAdapter().Resolve(spellbooks);
            bool available = unit.Descriptor.State != null && !unit.Descriptor.State.IsDead &&
                unit.Descriptor.State.IsConscious;
            foreach (Spellbook book in spellbooks)
            {
                string guid = book.Blueprint.AssetGuid;
                SpellbookMembershipKind kind = book.Blueprint.Spontaneous
                    ? SpellbookMembershipKind.Known : SpellbookMembershipKind.Prepared;
                SpellbookRoleResolution role;
                roles.TryGetValue(guid, out role);
                if (!available)
                {
                    facts.Add(Fact(unit, guid, kind, SpellbookMembershipStatus.CasterUnavailable,
                        "caster-dead-or-unconscious"));
                    continue;
                }
                if (!contractsComplete || role == null || role.Role != SpellbookRole.CastingCapable ||
                    role.RelationshipTargetGuid.Length != 0)
                {
                    facts.Add(Fact(unit, guid, kind, SpellbookMembershipStatus.RoleNotProven,
                        !contractsComplete ? "optional-spellbook-contracts-incomplete"
                            : role == null ? "role-unresolved" : role.Role + ":" + role.Reason));
                    continue;
                }
                List<KeyValuePair<string, int>> members;
                try { members = kind == SpellbookMembershipKind.Prepared ? Prepared(book) : Known(book); }
                catch (Exception exception)
                {
                    facts.Add(Fact(unit, guid, kind, SpellbookMembershipStatus.Unreadable,
                        "read-failed:" + exception.GetType().Name));
                    continue;
                }
                facts.Add(new SpellbookMembershipFact(unit.UniqueId, guid, kind,
                    SpellbookMembershipStatus.Complete, string.Empty, members));
            }
            return facts;
        }

        private static SpellbookMembershipFact Fact(UnitEntityData unit, string guid,
            SpellbookMembershipKind kind, SpellbookMembershipStatus status, string reason)
        {
            return new SpellbookMembershipFact(unit.UniqueId, guid, kind, status, reason, null);
        }

        private static List<KeyValuePair<string, int>> Prepared(Spellbook book)
        {
            return book.GetAllMemorizedSpells()
                .Where(slot => slot != null && slot.Spell != null && slot.Spell.Blueprint != null)
                .Select(slot => new KeyValuePair<string, int>(Member(slot.Spell), 1)).ToList();
        }

        private static List<KeyValuePair<string, int>> Known(Spellbook book)
        {
            var members = new List<KeyValuePair<string, int>>();
            for (int level = 0; level <= book.MaxSpellLevel; level++)
                foreach (AbilityData spell in book.GetKnownSpells(level)
                    .Concat(book.GetSpecialSpells(level)).Concat(book.GetCustomSpells(level)))
                    if (spell != null && spell.Blueprint != null)
                        members.Add(new KeyValuePair<string, int>(Member(spell), 1));
            return members;
        }

        // The spell as the book holds it: a variant's parent (books hold the
        // parent; the cast chooses the variant) and its metamagic.
        private static string Member(AbilityData spell)
        {
            BlueprintAbility blueprint = spell.Blueprint;
            string baseGuid = blueprint.Parent == null ? blueprint.AssetGuid : blueprint.Parent.AssetGuid;
            int metamagic = spell.MetamagicData == null ? 0 : (int)spell.MetamagicData.MetamagicMask;
            return SpellbookMembershipFact.MemberKey(baseGuid, metamagic);
        }

        // The localized name of a removed spell, for the player's notice.
        private static string AbilityName(string guid)
        {
            try
            {
                BlueprintAbility blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(guid);
                return blueprint == null ? null : blueprint.Name;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
