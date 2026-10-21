namespace MalaCosecha.Interaction
{
    /// <summary>
    /// Contrato para cualquier cosa con la que el jugador puede interactuar con E:
    /// un recurso para recoger, un farol para encender, una barricada para reparar...
    ///
    /// Una interfaz dice QUÉ se puede hacer, no CÓMO. El PlayerInteractor no conoce farolas
    /// ni recursos: solo busca "algo que implemente IInteractable" y le dice Interact().
    /// Para agregar un objeto interactivo nuevo no hay que tocar el código del jugador.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texto que se muestra al apuntarle. Ej: "Recoger Materiales (+5)".</summary>
        string GetPrompt(PlayerInteractor interactor);

        /// <summary>Si ahora mismo se puede usar (ej: un farol ya encendido devolvería false).</summary>
        bool CanInteract(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
