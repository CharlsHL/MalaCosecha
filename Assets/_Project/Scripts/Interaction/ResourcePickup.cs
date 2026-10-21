using MalaCosecha.Inventory;
using UnityEngine;

namespace MalaCosecha.Interaction
{
    /// <summary>
    /// Un recurso tirado en el mundo que se recoge con E.
    ///
    /// Necesita un Collider (en este objeto o en un hijo) para que el rayo del
    /// PlayerInteractor lo encuentre.
    /// </summary>
    public class ResourcePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] ResourceType resourceType = ResourceType.Materials;
        [SerializeField, Min(1)] int amount = 5;

        public string GetPrompt(PlayerInteractor interactor) =>
            $"Recoger {resourceType.DisplayName()} (+{amount})";

        public bool CanInteract(PlayerInteractor interactor) => interactor.Inventory != null;

        public void Interact(PlayerInteractor interactor)
        {
            interactor.Inventory.Add(resourceType, amount);
            Destroy(gameObject);
        }
    }
}
