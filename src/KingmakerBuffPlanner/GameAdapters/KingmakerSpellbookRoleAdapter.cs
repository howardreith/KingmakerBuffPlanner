using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic;
using KingmakerBuffPlanner.Compatibility;

namespace KingmakerBuffPlanner.GameAdapters
{
    // Optional Call of the Wild integration. Every reflected member is pinned to the local
    // component contract and absence leaves the existing native spellbook behavior intact.
    internal sealed class KingmakerSpellbookRoleAdapter
    {
        private const string OptionalAssembly = "CallOfTheWild";
        private const string CannotUseSpellsType =
            "CallOfTheWild.SpellbookMechanics.CanNotUseSpells";
        private const string CompanionSpellbookType =
            "CallOfTheWild.SpellbookMechanics.CompanionSpellbook";
        private const string KnownFromMemorizationType =
            "CallOfTheWild.SpellbookMechanics.GetKnownSpellsFromMemorizationSpellbook";

        private static readonly OptionalSpellbookContracts Contracts =
            OptionalSpellbookContracts.Create();

        internal IReadOnlyDictionary<string, SpellbookRoleResolution> Resolve(
            IEnumerable<Spellbook> spellbooks)
        {
            List<Spellbook> owned = (spellbooks ?? new Spellbook[0])
                .Where(book => book != null && book.Blueprint != null)
                .OrderBy(book => book.Blueprint.AssetGuid, StringComparer.Ordinal).ToList();
            return SpellbookRoleResolver.Resolve(owned.Select(book =>
                new SpellbookRoleInput(book.Blueprint.AssetGuid, book.Blueprint.Spontaneous,
                    HasComponent(book.Blueprint, Contracts.CannotUseSpells),
                    Relationship(book.Blueprint, Contracts.CompanionSpellbook),
                    Relationship(book.Blueprint, Contracts.KnownFromMemorization))).ToArray());
        }

        internal bool IsIncluded(Spellbook spellbook, IEnumerable<Spellbook> ownedSpellbooks)
        {
            if (spellbook == null || spellbook.Blueprint == null) return false;
            SpellbookRoleResolution result;
            return Resolve(ownedSpellbooks).TryGetValue(spellbook.Blueprint.AssetGuid,
                out result) && result.Included;
        }

        private static bool HasComponent(BlueprintSpellbook book, Type type)
        {
            return type != null && (book.ComponentsArray ?? new BlueprintComponent[0])
                .Any(component => component != null && component.GetType() == type);
        }

        private static string Relationship(BlueprintSpellbook book, OptionalComponentContract contract)
        {
            if (contract == null || contract.Type == null || contract.SpellbookField == null)
                return string.Empty;
            BlueprintComponent component = (book.ComponentsArray ?? new BlueprintComponent[0])
                .FirstOrDefault(value => value != null && value.GetType() == contract.Type);
            if (component == null) return string.Empty;
            try
            {
                var relationship = contract.SpellbookField.GetValue(component) as BlueprintSpellbook;
                return relationship == null ? string.Empty : relationship.AssetGuid ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class OptionalSpellbookContracts
        {
            internal Type CannotUseSpells;
            internal OptionalComponentContract CompanionSpellbook;
            internal OptionalComponentContract KnownFromMemorization;

            internal static OptionalSpellbookContracts Create()
            {
                return new OptionalSpellbookContracts
                {
                    CannotUseSpells = FindOptionalType(CannotUseSpellsType),
                    CompanionSpellbook = OptionalComponentContract.Create(
                        FindOptionalType(CompanionSpellbookType)),
                    KnownFromMemorization = OptionalComponentContract.Create(
                        FindOptionalType(KnownFromMemorizationType))
                };
            }

            private static Type FindOptionalType(string fullName)
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => string.Equals(assembly.GetName().Name, OptionalAssembly,
                        StringComparison.Ordinal))
                    .Select(assembly => assembly.GetType(fullName, false))
                    .FirstOrDefault(type => type != null);
            }
        }

        private sealed class OptionalComponentContract
        {
            internal Type Type;
            internal FieldInfo SpellbookField;

            internal static OptionalComponentContract Create(Type type)
            {
                if (type == null) return null;
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic;
                FieldInfo field = type.GetField("spellbook", Flags);
                if (field == null || field.FieldType != typeof(BlueprintSpellbook)) return null;
                return new OptionalComponentContract { Type = type, SpellbookField = field };
            }
        }
    }
}
