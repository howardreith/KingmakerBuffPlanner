using System;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    internal static class ControllerPresentationHost
    {
        internal static Canvas Resolve(Component origin)
        {
            Canvas nearest = origin == null ? null : origin.GetComponentInParent<Canvas>();
            Canvas root = nearest == null ? null : nearest.rootCanvas;
            if (root == null || !root.gameObject.activeInHierarchy)
                throw new InvalidOperationException("The controller campaign root canvas is unavailable.");
            Debug.Log("[KBP-CONTROLLER-HOST] origin=" + origin.GetType().Name + ";nearest=" + nearest.GetInstanceID() +
                ";root=" + root.GetInstanceID() + ";scale=" + root.scaleFactor + ";mode=" + root.renderMode +
                ";nativePixelsNotQualified=true.");
            // Never retain a spellbook's nested/masked canvas as the editor's
            // theme/scale/lifetime owner after the source book has closed.
            return root;
        }
    }
}
