using System;

namespace DragonQuest.Progression
{
    public enum DragonRestState { Sleeping, Waking, Awake }

    // The resting pose belongs to this visit, rather than to the saved boss reward.
    public sealed class DragonAwakening
    {
        public const float Duration = 1.4f;
        public DragonRestState State { get; private set; }
        public float Elapsed { get; private set; }
        public int Frame => State == DragonRestState.Sleeping ? 0
            : State == DragonRestState.Awake || Elapsed >= .95f ? 2 : 1;

        public bool Wake()
        {
            if (State != DragonRestState.Sleeping) return false;
            State = DragonRestState.Waking;
            Elapsed = 0;
            return true;
        }

        public void Advance(float seconds)
        {
            if (State != DragonRestState.Waking || seconds <= 0 || float.IsNaN(seconds)) return;
            Elapsed = Math.Min(Duration, Elapsed + seconds);
            if (Elapsed >= Duration) State = DragonRestState.Awake;
        }

        public void Finish()
        {
            if (State == DragonRestState.Sleeping) return;
            Elapsed = Duration;
            State = DragonRestState.Awake;
        }

        public void Reset() { State = DragonRestState.Sleeping; Elapsed = 0; }
    }
}
