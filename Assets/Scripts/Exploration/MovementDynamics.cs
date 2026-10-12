using System;
using UnityEngine;

namespace DragonQuest.Exploration
{
    // A camera acompanha a fisica interpolada; curvas suaves comecam na velocidade do corpo.
    public static class MovementDynamics
    {
        public const float DefaultAcceleration = 80f;
        public const float DefaultDeceleration = 100f;

        public static Vector2 StepVelocity(Vector2 current, Vector2 target, float deltaTime,
            float acceleration = DefaultAcceleration, float deceleration = DefaultDeceleration)
        {
            float rate = target.sqrMagnitude < 0.0001f ? deceleration : acceleration;
            return Vector2.MoveTowards(current, target, Math.Max(0, rate) * Math.Max(0, deltaTime));
        }
    }
}
