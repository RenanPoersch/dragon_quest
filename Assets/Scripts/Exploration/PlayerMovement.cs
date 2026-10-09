using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonQuest.Exploration
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 4f;

        private Rigidbody2D body;
        private Vector2 movement;

        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !Application.isFocused)
            {
                movement = Vector2.zero;
                return;
            }

            float horizontal = 0f;
            float vertical = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;

            // Clamp evita que a diagonal seja mais rápida que os eixos.
            movement = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
            if (movement.sqrMagnitude > 0f) FacingDirection = movement.normalized;
        }

        private void FixedUpdate()
        {
            // A física resolve as colisões, inclusive ao deslizar ao longo de paredes.
            body.linearVelocity = movement * speed;
        }

        private void OnDisable()
        {
            movement = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                movement = Vector2.zero;
                if (body != null) body.linearVelocity = Vector2.zero;
            }
        }
    }
}
