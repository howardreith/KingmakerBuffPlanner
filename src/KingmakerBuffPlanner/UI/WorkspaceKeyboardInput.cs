using System;
using UnityEngine;

namespace KingmakerBuffPlanner.UI
{
    // External input/window observations, separate from semantic dispatch and
    // native rendering. Detached tests can drive the production view directly.
    internal sealed class WorkspaceKeyboardInput
    {
        internal readonly Func<KeyCode, bool> KeyDown, KeyHeld;
        internal readonly Func<bool> DescriptionOpen, Neutral;
        internal readonly Func<int> Frame;

        internal WorkspaceKeyboardInput()
            : this(Input.GetKeyDown, Input.GetKey, () => NativeDescriptionPresenter.IsOpen,
                () => !Input.anyKey, () => Time.frameCount) { }

        internal WorkspaceKeyboardInput(Func<KeyCode, bool> keyDown, Func<KeyCode, bool> keyHeld,
            Func<bool> descriptionOpen, Func<bool> neutral, Func<int> frame)
        {
            KeyDown = keyDown;
            KeyHeld = keyHeld;
            DescriptionOpen = descriptionOpen;
            Neutral = neutral;
            Frame = frame;
        }
    }
}
