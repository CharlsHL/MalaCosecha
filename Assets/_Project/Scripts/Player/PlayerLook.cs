using UnityEngine;

namespace MalaCosecha.Player
{
    /// <summary>
    /// Cámara en primera persona.
    ///
    /// El giro se reparte en dos objetos:
    ///  - Horizontal (yaw): rota el cuerpo entero del jugador. Así "adelante" para
    ///    PlayerController siempre es hacia donde mirás.
    ///  - Vertical (pitch): rota solo el CameraRoot. Si rotáramos el cuerpo hacia arriba,
    ///    al mirar al cielo el jugador intentaría caminar hacia arriba.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] Transform cameraRoot;

        [Tooltip("Grados por píxel de movimiento del mouse.")]
        [SerializeField] float mouseSensitivity = 0.1f;

        [Tooltip("Grados por segundo con el stick al máximo.")]
        [SerializeField] float gamepadSensitivity = 180f;

        [SerializeField] bool invertY;
        [SerializeField, Range(-90f, 0f)] float minPitch = -85f;
        [SerializeField, Range(0f, 90f)] float maxPitch = 85f;

        PlayerInputReader input;
        float pitch;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
        }

        void Update()
        {
            Vector2 look = input.Look;
            if (look == Vector2.zero)
                return;

            // El mouse ya entrega "cuánto se movió en este frame", así que NO se multiplica
            // por deltaTime (si lo hiciéramos, a más fps la cámara giraría más lento).
            // El stick entrega una posición (-1 a 1) que se mantiene: eso sí es una velocidad
            // y necesita deltaTime.
            look *= input.LookIsFromGamepad
                ? gamepadSensitivity * Time.deltaTime
                : mouseSensitivity;

            transform.Rotate(0f, look.x, 0f);

            // Mover el mouse hacia arriba da Y positivo, pero en Unity un pitch positivo
            // mira hacia abajo: por eso restamos.
            pitch -= invertY ? -look.y : look.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            if (cameraRoot != null)
                cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
