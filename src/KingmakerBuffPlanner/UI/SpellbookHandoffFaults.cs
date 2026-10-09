namespace KingmakerBuffPlanner.UI
{
    // WP2B runtime evidence: a guarded physical scenario proves that a
    // handoff failing after the native spellbook closed still leaves a usable
    // interface. It arms exactly one simulated opener refusal, and only inside
    // a locked runtime-test session; ordinary play can never arm it.
    internal static class SpellbookHandoffFaults
    {
        private static bool _refuseNextOpen;

        internal static bool ArmOpenRefusalForRuntime()
        {
            if (!NativeCastingSessionPolicy.Locked) return false;
            _refuseNextOpen = true;
            return true;
        }

        internal static bool ConsumeOpenRefusal()
        {
            if (!_refuseNextOpen) return false;
            _refuseNextOpen = false;
            return NativeCastingSessionPolicy.Locked;
        }
    }
}
