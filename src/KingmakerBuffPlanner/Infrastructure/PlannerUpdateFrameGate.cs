namespace KingmakerBuffPlanner.Infrastructure
{
    internal sealed class PlannerUpdateFrameGate
    {
        private int _lastFrame = -1;

        internal bool TryEnter(int frame)
        {
            if (frame == _lastFrame) return false;
            _lastFrame = frame;
            return true;
        }
    }
}
