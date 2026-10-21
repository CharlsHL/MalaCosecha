namespace MalaCosecha.Inventory
{
    /// <summary>
    /// Tipos de recurso del MVP. El documento de diseño pide uno principal (materiales para
    /// reparar y construir) más la munición como recurso aparte.
    /// </summary>
    public enum ResourceType
    {
        Materials,
        Ammo
    }

    public static class ResourceTypeExtensions
    {
        /// <summary>Nombre para mostrar en pantalla.</summary>
        public static string DisplayName(this ResourceType type) => type switch
        {
            ResourceType.Materials => "Materiales",
            ResourceType.Ammo => "Munición",
            _ => type.ToString()
        };
    }
}
