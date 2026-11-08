using System;

namespace KingmakerBuffPlanner.Domain.Identity
{
    public enum ItemSelectionKind
    {
        // The field is empty: nothing can be resolved from it.
        None,
        // "This caster's equipped item of this type supplying this ability."
        EquippedType,
        // The same rule narrowed to one equipment position, chosen explicitly
        // because the caster has identical copies equipped.
        EquippedSlot,
        // A pre-rule per-object token. Its object no longer exists after a
        // load, so it can never be matched and is never reinterpreted.
        LegacySession,
        // Unreadable saved data. Refused, never guessed at.
        Invalid
    }

    public sealed class ItemSourceSelection
    {
        internal ItemSourceSelection(ItemSelectionKind kind, string itemBlueprintGuid, string slotId)
        {
            Kind = kind;
            ItemBlueprintGuid = itemBlueprintGuid ?? string.Empty;
            SlotId = slotId ?? string.Empty;
        }

        public ItemSelectionKind Kind { get; private set; }
        public string ItemBlueprintGuid { get; private set; }
        public string SlotId { get; private set; }
        public bool IsDurable
        {
            get { return Kind == ItemSelectionKind.EquippedType || Kind == ItemSelectionKind.EquippedSlot; }
        }
    }

    // The persisted meaning of AbilityKey.SpecialSourceId for an Item source.
    //
    // Kingmaker 2.1.7b persists no identity for an ItemEntity, so a saved plan
    // cannot name one physical item. These selectors are explicit selection
    // RULES, not item identities: an equipped copy of the type satisfies the
    // rule whichever physical copy it is, and each run binds to the specific
    // live item through ProviderKey.SourceInstanceId, which is never saved.
    public static class ItemSourceSelector
    {
        public const string LegacySessionPrefix = "session-item-";
        private const string EquippedPrefix = "equipped-item:";
        private const char SlotSeparator = '@';

        public static string ForEquippedType(string itemBlueprintGuid)
        {
            if (string.IsNullOrWhiteSpace(itemBlueprintGuid))
                throw new ArgumentException("Item blueprint GUID is required.", "itemBlueprintGuid");
            return EquippedPrefix + itemBlueprintGuid;
        }

        public static string ForEquippedSlot(string itemBlueprintGuid, string slotId)
        {
            if (SlotLabel(slotId).Length == 0)
                throw new ArgumentException("A known equipment slot is required.", "slotId");
            return ForEquippedType(itemBlueprintGuid) + SlotSeparator + slotId;
        }

        // What discovery publishes for one equipped item: the plain rule,
        // or the slot rule when this caster has identical copies equipped.
        // An unlocated copy stays plain, so the copies remain ambiguous.
        public static string ForDiscoveredItem(string itemBlueprintGuid, string slotId, int copiesOnCaster)
        {
            return copiesOnCaster > 1 && slotId != null
                ? ForEquippedSlot(itemBlueprintGuid, slotId) : ForEquippedType(itemBlueprintGuid);
        }

        public static ItemSourceSelection Parse(string specialSourceId)
        {
            string value = specialSourceId ?? string.Empty;
            if (value.Length == 0) return new ItemSourceSelection(ItemSelectionKind.None, null, null);
            if (value.StartsWith(LegacySessionPrefix, StringComparison.Ordinal))
                return new ItemSourceSelection(ItemSelectionKind.LegacySession, null, null);
            if (!value.StartsWith(EquippedPrefix, StringComparison.Ordinal))
                return new ItemSourceSelection(ItemSelectionKind.Invalid, null, null);
            string body = value.Substring(EquippedPrefix.Length);
            int separator = body.IndexOf(SlotSeparator);
            string guid = separator < 0 ? body : body.Substring(0, separator);
            if (string.IsNullOrWhiteSpace(guid))
                return new ItemSourceSelection(ItemSelectionKind.Invalid, null, null);
            if (separator < 0) return new ItemSourceSelection(ItemSelectionKind.EquippedType, guid, null);
            string slot = body.Substring(separator + 1);
            return SlotLabel(slot).Length == 0
                ? new ItemSourceSelection(ItemSelectionKind.Invalid, null, null)
                : new ItemSourceSelection(ItemSelectionKind.EquippedSlot, guid, slot);
        }

        // The player-facing name of a slot id, or "" for anything unknown.
        public static string SlotLabel(string slotId)
        {
            string value = slotId ?? string.Empty;
            switch (value)
            {
                case "Armor": return "Armor";
                case "Belt": return "Belt";
                case "Head": return "Head";
                case "Feet": return "Feet";
                case "Gloves": return "Gloves";
                case "Neck": return "Neck";
                case "Ring1": return "Ring 1";
                case "Ring2": return "Ring 2";
                case "Wrist": return "Wrist";
                case "Shoulders": return "Shoulders";
            }
            int index;
            const string Quick = "Quick";
            if (value.StartsWith(Quick, StringComparison.Ordinal) &&
                TryIndex(value.Substring(Quick.Length), out index))
                return "Quick slot " + (index + 1);
            foreach (string hand in new[] { "Primary", "Secondary" })
            {
                const string Set = "Set";
                if (value.StartsWith(Set, StringComparison.Ordinal) &&
                    value.EndsWith(hand, StringComparison.Ordinal) &&
                    value.Length > Set.Length + hand.Length &&
                    TryIndex(value.Substring(Set.Length, value.Length - Set.Length - hand.Length), out index))
                    return "Weapon set " + (index + 1) + (hand == "Primary" ? " main hand" : " off hand");
            }
            return string.Empty;
        }

        private static bool TryIndex(string text, out int index)
        {
            index = -1;
            if (text.Length == 0) return false;
            foreach (char digit in text)
                if (digit < '0' || digit > '9') return false;
            return int.TryParse(text, out index);
        }
    }
}
