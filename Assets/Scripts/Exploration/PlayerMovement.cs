using UnityEngine;
using UnityEngine.InputSystem;

namespace DragonQuest.Exploration
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 4f;
        [SerializeField, Min(1f)] private float acceleration = MovementDynamics.DefaultAcceleration;
        [SerializeField, Min(1f)] private float deceleration = MovementDynamics.DefaultDeceleration;

        private Rigidbody2D body;
        private Vector2 movement;
        private bool movementEnabled = true;
        private System.Func<Vector2,Vector2,Vector2> movementConstraint;
        public void SetMovementConstraint(System.Func<Vector2,Vector2,Vector2> constraint)
        { movementConstraint=constraint; }
        private System.Func<float, float> lateralFloor;
        private float lateralMin, lateralMax;
        private float lateralSpeedMultiplier = 1, lateralVelocity;
        public bool IsSideView => lateralFloor != null;
        public bool IsFrontalView { get; private set; }
        public void SetFrontalView(bool enabled) => IsFrontalView=enabled;

        public void SetLateralPath(System.Func<float, float> floor, float minX = 0, float maxX = 0, float speedMultiplier = 1)
        {
            lateralFloor = floor; lateralMin = minX; lateralMax = maxX;
            lateralSpeedMultiplier = speedMultiplier; lateralVelocity = 0;
            movement = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            if (IsSideView) FacingDirection = Vector2.left;
        }

        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public bool HasMovementInput => movement.sqrMagnitude > 0;
        public int TeleportRevision { get; private set; }

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
            if (!movementEnabled || keyboard == null || !Application.isFocused)
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
            movement = IsSideView ? new Vector2(horizontal, 0)
                : IsFrontalView ? Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f)
                : ObliqueProjection.GroundDirection(Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f));
            if (movement.sqrMagnitude > 0f) FacingDirection = movement.normalized;
        }

        private void FixedUpdate()
        {
            if (IsSideView)
            {
                Vector2 velocity = movementEnabled && Application.isFocused
                    ? MovementDynamics.StepVelocity(new Vector2(lateralVelocity, 0), movement * speed * lateralSpeedMultiplier,
                        Time.fixedDeltaTime, acceleration, deceleration) : Vector2.zero;
                float x = Mathf.Clamp(body.position.x + velocity.x * Time.fixedDeltaTime, lateralMin, lateralMax);
                lateralVelocity = x == lateralMin || x == lateralMax ? 0 : velocity.x;
                body.linearVelocity = Vector2.zero;
                body.MovePosition(new Vector2(x, lateralFloor(x)));
                return;
            }
            // Evita trocar instantaneamente a velocidade e provocar um solavanco na camera.
            // Parte da velocidade resolvida pela fisica, conservando colisao e deslizamento nas paredes.
            body.linearVelocity = movementEnabled && Application.isFocused
                ? MovementDynamics.StepVelocity(body.linearVelocity, movement * speed, Time.fixedDeltaTime, acceleration, deceleration)
                : Vector2.zero;
            if (movementConstraint != null)
            {
                Vector2 next=movementConstraint(body.position,body.linearVelocity*Time.fixedDeltaTime);
                body.linearVelocity=(next-body.position)/Time.fixedDeltaTime;
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!enabled)
            {
                lateralVelocity = 0;
                movement = Vector2.zero;
                if (body != null) body.linearVelocity = Vector2.zero;
            }
        }

        public void Teleport(Vector2 position)
        {
            lateralVelocity = 0;
            unchecked { TeleportRevision++; }
            movement = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        private void OnDisable()
        {
            lateralVelocity = 0;
            movement = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                lateralVelocity = 0;
                movement = Vector2.zero;
                if (body != null) body.linearVelocity = Vector2.zero;
            }
        }
    }
}
