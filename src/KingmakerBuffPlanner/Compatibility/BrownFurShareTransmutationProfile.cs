namespace KingmakerBuffPlanner.Compatibility
{
    internal static class BrownFurShareTransmutationProfile
    {
        internal const string FeatureGuid =
            "b7e929dac874cd22d173ee8f4fe0bfa4";
        internal const string ActivatableGuid =
            "8641e6c39ff133ad71f669e35e1ee688";
        internal const string MarkerBuffGuid =
            "215a03a25c8ff8b76114bf7513869d6c";
        internal const string SupremacyFeatureGuid =
            "c69cd7091219708f981272f2ac057135";
        internal const string ReservoirGuid =
            BrownFurPowerfulChangeProfile.ReservoirGuid;

        internal static string EnhancementId(string casterUnitId)
        {
            return "share-transmutation|" + casterUnitId + "|" +
                ActivatableGuid;
        }

        internal static string UsagePoolId(string casterUnitId)
        {
            return BrownFurPowerfulChangeProfile.UsagePoolId(casterUnitId);
        }
    }
}
