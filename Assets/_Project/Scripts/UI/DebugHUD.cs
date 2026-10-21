using MalaCosecha.Interaction;
using MalaCosecha.Inventory;
using MalaCosecha.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MalaCosecha.UI
{
    /// <summary>
    /// HUD TEMPORAL para el prototipo, dibujado con IMGUI (OnGUI).
    ///
    /// IMGUI no se usa para la UI final de un juego (es lento y feo), pero para prototipar es
    /// ideal: cero configuración, ni Canvas ni fuentes ni prefabs. Se dibuja todo desde código.
    /// En la etapa visual lo reemplazamos por un HUD real; como este script solo LEE el estado
    /// de los demás componentes, borrarlo no rompe nada.
    ///
    /// F1 muestra/oculta la info de depuración.
    /// </summary>
    public class DebugHUD : MonoBehaviour
    {
        [SerializeField] bool showDebugInfo = true;

        PlayerController player;
        PlayerStamina stamina;
        PlayerInteractor interactor;
        PlayerInventory inventory;

        GUIStyle labelStyle;
        GUIStyle centeredStyle;

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("DebugHUD: no hay ningún PlayerController en la escena.", this);
                enabled = false;
                return;
            }

            stamina = player.GetComponent<PlayerStamina>();
            interactor = player.GetComponent<PlayerInteractor>();
            inventory = player.GetComponent<PlayerInventory>();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                showDebugInfo = !showDebugInfo;
        }

        void OnGUI()
        {
            CreateStylesIfNeeded();

            DrawCrosshair();
            DrawInteractionPrompt();
            DrawStaminaBar();
            DrawInventory();

            if (showDebugInfo)
                DrawDebugInfo();

            if (Cursor.lockState != CursorLockMode.Locked)
                DrawShadowedLabel(new Rect(0, Screen.height * 0.3f, Screen.width, 30),
                                  "Click para jugar  ·  Esc libera el mouse", centeredStyle);
        }

        void DrawCrosshair()
        {
            bool hasTarget = interactor != null && interactor.CurrentTarget != null;
            float size = hasTarget ? 8f : 4f;
            var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            DrawRect(new Rect(center.x - size / 2, center.y - size / 2, size, size),
                     hasTarget ? Color.yellow : Color.white);
        }

        void DrawInteractionPrompt()
        {
            string prompt = interactor != null ? interactor.CurrentPrompt : null;
            if (string.IsNullOrEmpty(prompt))
                return;

            DrawShadowedLabel(new Rect(0, Screen.height / 2f + 20, Screen.width, 30),
                              $"[E] {prompt}", centeredStyle);
        }

        void DrawStaminaBar()
        {
            if (stamina == null)
                return;

            var background = new Rect(20, Screen.height - 40, 250, 18);
            DrawRect(background, new Color(0f, 0f, 0f, 0.6f));

            var fill = new Rect(background.x + 2, background.y + 2,
                                (background.width - 4) * stamina.Normalized, background.height - 4);
            // Rojo mientras está agotado: el jugador tiene que entender por qué no puede correr.
            DrawRect(fill, stamina.IsExhausted ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.3f, 0.8f, 0.3f));

            DrawShadowedLabel(new Rect(background.x, background.y - 22, 250, 20), "Resistencia", labelStyle);
        }

        void DrawInventory()
        {
            if (inventory == null)
                return;

            string text = $"{ResourceType.Materials.DisplayName()}: {inventory.Get(ResourceType.Materials)}\n" +
                          $"{ResourceType.Ammo.DisplayName()}: {inventory.Get(ResourceType.Ammo)}";
            DrawShadowedLabel(new Rect(20, 20, 300, 50), text, labelStyle);
        }

        void DrawDebugInfo()
        {
            string text = $"Velocidad: {player.CurrentSpeed:0.0} m/s\n" +
                          $"En el suelo: {(player.IsGrounded ? "sí" : "no")}\n" +
                          $"Corriendo: {(player.IsSprinting ? "sí" : "no")}\n" +
                          $"Agachado: {(player.IsCrouching ? "sí" : "no")}\n" +
                          $"Resistencia: {stamina.Current:0}/{stamina.Max:0}\n" +
                          $"FPS: {1f / Time.smoothDeltaTime:0}\n" +
                          "(F1 oculta)";
            DrawShadowedLabel(new Rect(Screen.width - 220, 20, 200, 160), text, labelStyle);
        }

        // --- Utilidades de dibujo ---

        void CreateStylesIfNeeded()
        {
            if (labelStyle != null)
                return;

            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            centeredStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        }

        static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void DrawShadowedLabel(Rect rect, string text, GUIStyle style)
        {
            // Sombra negra desplazada 1px: el texto se lee sobre fondos claros u oscuros.
            Color previous = GUI.color;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            GUI.color = Color.white;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }
    }
}
