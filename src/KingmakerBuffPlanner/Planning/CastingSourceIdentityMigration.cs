using System;
using System.Collections.Generic;
using KingmakerBuffPlanner.Domain.Authoring;
using KingmakerBuffPlanner.Domain.Effects;

namespace KingmakerBuffPlanner.Planning
{
    // 0.4.2: a class/fact ability is its own catalogue entry
    // (CatalogSourceIdentity), so a saved casting of one that carried the
    // shared effect aggregate - or, unresolved, its own canonical key - now
    // carries the ability's own identity. Pure and idempotent: derived from
    // each casting's persisted exact ability alone (never from discovery or
    // a name); ids, order, caster, book, targets, enhancements and state
    // are untouched, and spells keep their aggregate.
    public static class CastingSourceIdentityMigration
    {
        public static CastingPlanDocument Normalize(
            CastingPlanDocument document, out int migrated)
        {
            if (document == null) throw new ArgumentNullException("document");
            migrated = 0;
            var castings = new List<PlannedCasting>(document.Castings.Count);
            foreach (PlannedCasting casting in document.Castings)
            {
                string own = CatalogSourceIdentity.DerivableFor(casting.Ability);
                bool stale = own != null && CatalogSourceIdentity.IsAbility(own) &&
                    !string.Equals(casting.SourceId, own, StringComparison.Ordinal) &&
                    (EffectAggregateIdentity.IsAggregate(casting.SourceId) ||
                        string.Equals(casting.SourceId, casting.Ability.Canonical,
                            StringComparison.Ordinal));
                if (stale) migrated++;
                castings.Add(stale ? casting.WithSourceId(own) : casting);
            }
            return migrated == 0 ? document : new CastingPlanDocument(document.CampaignId,
                document.Routines, castings, document.ImportNotices,
                document.AcknowledgedImportNotices);
        }
    }
}
