using System;
using UnityEngine;

namespace DragonQuest.Prototype
{
    public sealed class CastleDestroyedEndingPresentation : IDisposable
    {
        public CastleImpactPresentation Impact { get; }
        public CastleDestroyedPresentation Aftermath { get; }
        private bool hasDrawn;
        public bool IsComplete => Impact.Sequence.IsComplete && Aftermath.Sequence.IsComplete;
        public float Elapsed => Impact.Sequence.Elapsed + Aftermath.Sequence.Elapsed;

        public CastleDestroyedEndingPresentation()
        {
            Impact = new CastleImpactPresentation();
            // Preload behind the opening black so the white transition never waits for disk/GPU work.
            try { Aftermath = new CastleDestroyedPresentation(enterFromWhite: true); }
            catch { Impact.Dispose(); throw; }
        }
        public void Update(float unscaledDeltaTime)
        {
            if (!hasDrawn) return;
            Advance(unscaledDeltaTime);
        }
        // The preview scrubber and runtime share the same cut/overflow behavior.
        public void Advance(float unscaledDeltaTime)
        {
            float remaining = CastleImpactSequence.CompleteAt - Impact.Sequence.Elapsed;
            Impact.Sequence.Advance(unscaledDeltaTime);
            // Carry a long frame across the cut exactly once, without shortening the epilogue.
            if (unscaledDeltaTime > remaining) Aftermath.Sequence.Advance(unscaledDeltaTime - remaining);
        }
        public void Draw() => Draw(new Rect(0, 0, Screen.width, Screen.height));
        public void Draw(Rect viewport)
        {
            if (Event.current.type == EventType.Repaint) hasDrawn = true;
            if (!Impact.Sequence.IsComplete) Impact.Draw(viewport);
            else Aftermath.Draw(viewport);
        }
        public void Dispose() { Impact.Dispose(); Aftermath.Dispose(); }
    }
}
