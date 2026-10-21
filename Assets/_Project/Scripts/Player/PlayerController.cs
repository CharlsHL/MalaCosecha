using UnityEngine;

namespace MalaCosecha.Player
{
    /// <summary>
    /// Movimiento del jugador: caminar, correr, saltar y agacharse.
    ///
    /// Usa un CharacterController en vez de un Rigidbody. El CharacterController no tiene
    /// física "real" (no lo empujan fuerzas, no rebota, no se cae de costado): se mueve
    /// exactamente adonde le decimos y se frena contra paredes. Eso da un control preciso,
    /// que es lo que se espera de un FPS. La contra es que la gravedad la tenemos que
    /// programar nosotros (verticalVelocity).
    ///
    /// El "pivot" del jugador está en los pies: transform.position es el punto que toca el suelo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerStamina))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Velocidades (metros/segundo)")]
        [SerializeField] float walkSpeed = 4f;
        [SerializeField] float sprintSpeed = 7f;
        [SerializeField] float crouchSpeed = 2f;

        [Tooltip("Qué tan rápido se alcanza la velocidad deseada. Más alto = más 'seco'.")]
        [SerializeField] float acceleration = 25f;

        [Tooltip("Fracción del control que se conserva en el aire (0 = ninguno, 1 = total).")]
        [SerializeField, Range(0f, 1f)] float airControl = 0.3f;

        [Header("Salto y gravedad")]
        [SerializeField] float jumpHeight = 1.1f;
        [SerializeField] float gravity = -20f;

        [Header("Resistencia")]
        [SerializeField] float sprintStaminaPerSecond = 20f;
        [SerializeField] float jumpStaminaCost = 10f;

        [Header("Agacharse")]
        [SerializeField] float standingHeight = 1.8f;
        [SerializeField] float crouchingHeight = 1.0f;
        [SerializeField] float standingEyeHeight = 1.6f;
        [SerializeField] float crouchingEyeHeight = 0.8f;
        [Tooltip("Metros por segundo a los que cambia la altura al agacharse/pararse.")]
        [SerializeField] float crouchTransitionSpeed = 5f;

        [Tooltip("Objeto que contiene la cámara. Se mueve a la altura de los ojos.")]
        [SerializeField] Transform cameraRoot;

        CharacterController controller;
        PlayerInputReader input;
        PlayerStamina stamina;

        Vector3 horizontalVelocity;
        float verticalVelocity;

        // Propiedades de solo lectura: otros scripts (HUD, sonido de pasos, la IA del enemigo)
        // pueden consultar el estado del jugador sin poder modificarlo.
        public bool IsGrounded => controller.isGrounded;
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public float CurrentSpeed => horizontalVelocity.magnitude;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            stamina = GetComponent<PlayerStamina>();

            SetControllerHeight(standingHeight);
            SetEyeHeight(standingEyeHeight);
        }

        void Update()
        {
            UpdateCrouch();
            UpdateHorizontalVelocity();
            UpdateVerticalVelocity();

            // Un único Move por frame con el movimiento total. Multiplicamos por deltaTime
            // para pasar de "metros por segundo" a "metros en este frame": así el juego se
            // mueve igual a 30 fps que a 144 fps.
            Vector3 motion = (horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            CollisionFlags flags = controller.Move(motion);

            // Si nos golpeamos la cabeza contra un techo, cortamos el salto.
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }

        void UpdateHorizontalVelocity()
        {
            // ClampMagnitude evita que en diagonal (W+D) se vaya más rápido: sin esto el
            // vector (1,1) mide 1.41.
            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);

            // Solo se corre hacia adelante y de pie.
            bool wantsToSprint = input.SprintHeld && moveInput.y > 0.1f && !IsCrouching;
            IsSprinting = wantsToSprint && stamina.HasStamina;

            if (IsSprinting)
                stamina.Drain(sprintStaminaPerSecond * Time.deltaTime);

            float targetSpeed = IsCrouching ? crouchSpeed
                              : IsSprinting ? sprintSpeed
                              : walkSpeed;

            // transform.forward/right son relativos hacia donde mira el jugador
            // (PlayerLook rota este mismo transform).
            Vector3 desiredVelocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * targetSpeed;

            // En vez de saltar de golpe a la velocidad deseada, nos acercamos de a poco.
            // En el aire el cambio es más lento: se conserva el impulso del salto.
            float accel = controller.isGrounded ? acceleration : acceleration * airControl;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, accel * Time.deltaTime);
        }

        void UpdateVerticalVelocity()
        {
            // En el suelo mantenemos una velocidad negativa chica para que el controller
            // quede "pegado" (si fuera 0, isGrounded parpadea al bajar rampas).
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (input.JumpPressed && controller.isGrounded && !IsCrouching
                && stamina.TryConsume(jumpStaminaCost))
            {
                // Física de secundaria: velocidad inicial para alcanzar una altura h
                // con gravedad g  →  v = √(2·g·h). La g es negativa, de ahí el -2.
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        void UpdateCrouch()
        {
            // Agacharse es instantáneo; pararse solo si hay lugar arriba (si no, quedaríamos
            // con la cabeza metida dentro del techo de un túnel).
            if (input.CrouchHeld)
                IsCrouching = true;
            else if (IsCrouching && CanStandUp())
                IsCrouching = false;

            float targetHeight = IsCrouching ? crouchingHeight : standingHeight;
            SetControllerHeight(Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime));

            if (cameraRoot != null)
            {
                float targetEye = IsCrouching ? crouchingEyeHeight : standingEyeHeight;
                SetEyeHeight(Mathf.MoveTowards(cameraRoot.localPosition.y, targetEye, crouchTransitionSpeed * Time.deltaTime));
            }
        }

        bool CanStandUp()
        {
            // Lanzamos una esfera hacia arriba desde la "cabeza" actual de la cápsula.
            // Los SphereCast ignoran los colliders dentro de los cuales empiezan, así que
            // no chocan con el propio jugador.
            float radius = controller.radius * 0.95f;
            Vector3 origin = transform.position + Vector3.up * (controller.height - controller.radius);
            float distance = standingHeight - controller.height;
            return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance,
                                       Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        void SetControllerHeight(float height)
        {
            controller.height = height;
            controller.center = Vector3.up * (height / 2f); // pies siempre en el pivot
        }

        void SetEyeHeight(float eyeHeight)
        {
            if (cameraRoot == null)
                return;

            Vector3 position = cameraRoot.localPosition;
            position.y = eyeHeight;
            cameraRoot.localPosition = position;
        }
    }
}
