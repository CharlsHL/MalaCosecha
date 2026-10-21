using UnityEngine;
using UnityEngine.InputSystem;

namespace MalaCosecha.Player
{
    /// <summary>
    /// Único punto del juego que habla con el Input System.
    ///
    /// Lee las acciones del mapa "Player" del asset InputSystem_Actions (configurado como
    /// "Project-wide Actions" en Project Settings > Input System Package) y las expone como
    /// propiedades simples: Move, Look, JumpPressed, etc.
    ///
    /// ¿Por qué un lector intermedio? Así el resto de los scripts no sabe si el jugador usa
    /// teclado, mouse o gamepad. Si mañana se cambian las teclas, o se agrega un menú de pausa
    /// que bloquee el input, se toca solo este archivo.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        InputActionMap playerMap;
        InputAction moveAction;
        InputAction lookAction;
        InputAction jumpAction;
        InputAction sprintAction;
        InputAction crouchAction;
        InputAction interactAction;
        InputAction attackAction;

        // --- Valores continuos (se leen cada frame) ---
        public Vector2 Move => moveAction.ReadValue<Vector2>();

        // Si el cursor está libre (el jugador apretó Esc), no giramos la cámara.
        public Vector2 Look => CursorLocked ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        // El mouse entrega "píxeles movidos este frame"; el stick, una dirección de -1 a 1.
        // PlayerLook necesita saberlo para tratarlos distinto.
        public bool LookIsFromGamepad => lookAction.activeControl?.device is Gamepad;

        // --- Botones ---
        // IsPressed()              = está apretado ahora (sirve para "mantener").
        // WasPressedThisFrame()    = se apretó justo en este frame (sirve para "tocar").
        public bool JumpPressed => jumpAction.WasPressedThisFrame();
        public bool SprintHeld => sprintAction.IsPressed();
        public bool CrouchHeld => crouchAction.IsPressed();
        public bool InteractPressed => CursorLocked && interactAction.WasPressedThisFrame();
        public bool AttackPressed => CursorLocked && attackAction.WasPressedThisFrame();

        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            if (InputSystem.actions == null)
            {
                Debug.LogError("No hay Project-wide Actions configuradas. " +
                               "Asigná InputSystem_Actions en Project Settings > Input System Package.", this);
                enabled = false;
                return;
            }

            playerMap = InputSystem.actions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
            lookAction = playerMap.FindAction("Look", throwIfNotFound: true);
            jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
            sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: true);
            crouchAction = playerMap.FindAction("Crouch", throwIfNotFound: true);
            interactAction = playerMap.FindAction("Interact", throwIfNotFound: true);
            attackAction = playerMap.FindAction("Attack", throwIfNotFound: true);
        }

        void OnEnable()
        {
            playerMap?.Enable();
            SetCursorLocked(true);
        }

        void OnDisable()
        {
            SetCursorLocked(false);
        }

        void Update()
        {
            // Esc libera el mouse (útil en el Editor); un click lo vuelve a capturar.
            if (CursorLocked && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (!CursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
