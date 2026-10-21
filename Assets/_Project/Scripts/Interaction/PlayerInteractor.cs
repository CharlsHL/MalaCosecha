using MalaCosecha.Inventory;
using MalaCosecha.Player;
using UnityEngine;

namespace MalaCosecha.Interaction
{
    /// <summary>
    /// Detecta qué objeto interactivo tiene el jugador delante y lo usa al apretar E.
    ///
    /// Cada frame lanza un rayo (Raycast) desde la cámara hacia adelante. Si choca con algo
    /// que implemente IInteractable, ese es el "objetivo" actual.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;

        [Tooltip("Distancia máxima de interacción en metros.")]
        [SerializeField] float range = 2.5f;

        [Tooltip("Capas que el rayo puede detectar.")]
        [SerializeField] LayerMask interactionMask = Physics.DefaultRaycastLayers;

        PlayerInputReader input;

        public IInteractable CurrentTarget { get; private set; }
        public string CurrentPrompt => CurrentTarget?.GetPrompt(this);

        // Los objetos interactivos acceden al inventario a través del interactor:
        // así un ResourcePickup no tiene que buscar al jugador por su cuenta.
        public PlayerInventory Inventory { get; private set; }

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            Inventory = GetComponent<PlayerInventory>();
        }

        void Update()
        {
            CurrentTarget = FindTarget();

            if (CurrentTarget != null && input.InteractPressed)
            {
                CurrentTarget.Interact(this);
                // El objetivo puede haberse destruido (un recurso recogido): lo soltamos ya,
                // el próximo frame se vuelve a buscar.
                CurrentTarget = null;
            }
        }

        IInteractable FindTarget()
        {
            if (cameraTransform == null)
                return null;

            // El rayo nace dentro de la cápsula del jugador; los Raycast ignoran el collider
            // en el que empiezan, así que no se detecta a sí mismo.
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, range, interactionMask, QueryTriggerInteraction.Collide))
                return null;

            // GetComponentInParent: el collider puede estar en un hijo (ej: la pieza "Visual")
            // y el script en el objeto raíz.
            var target = hit.collider.GetComponentInParent<IInteractable>();
            return target != null && target.CanInteract(this) ? target : null;
        }

        void OnDrawGizmosSelected()
        {
            // Con el Player seleccionado se ve el alcance de interacción en la Scene view.
            if (cameraTransform == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * range);
        }
    }
}
