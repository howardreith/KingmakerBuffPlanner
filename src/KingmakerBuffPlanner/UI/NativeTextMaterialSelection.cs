using System;

namespace KingmakerBuffPlanner.UI
{
    // TextMeshPro can keep several materials with the same role name alive.
    // Fallback submeshes may bind one of those copies to another font atlas.
    // A role material is safe for the planner only when it belongs to the
    // selected body font's atlas.
    internal static class NativeTextMaterialSelection
    {
        internal static bool Matches(string expectedName, string candidateName,
            int bodyAtlasInstanceId, int candidateAtlasInstanceId)
        {
            return !string.IsNullOrEmpty(expectedName) &&
                string.Equals(candidateName, expectedName, StringComparison.Ordinal) &&
                bodyAtlasInstanceId != 0 &&
                candidateAtlasInstanceId == bodyAtlasInstanceId;
        }
    }
}
