using System;
using System.Collections.Generic;
using UnityEngine;

namespace MalaCosecha.Inventory
{
    /// <summary>
    /// Inventario mínimo: cuántas unidades de cada recurso tiene el jugador.
    ///
    /// Es la semilla del InventorySystem del documento. En el Hito 4 (defensas) le vamos a
    /// pedir materiales con TryRemove para construir y reparar.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        readonly Dictionary<ResourceType, int> amounts = new();

        /// <summary>
        /// Se dispara al cambiar una cantidad: (recurso, nueva cantidad).
        /// La UI se suscribe a esto en vez de preguntar cada frame.
        /// </summary>
        public event Action<ResourceType, int> Changed;

        public int Get(ResourceType type) => amounts.TryGetValue(type, out int amount) ? amount : 0;

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0)
                return;

            amounts[type] = Get(type) + amount;
            Changed?.Invoke(type, amounts[type]);
        }

        /// <summary>Quita la cantidad solo si alcanza. Devuelve si pudo.</summary>
        public bool TryRemove(ResourceType type, int amount)
        {
            if (amount <= 0 || Get(type) < amount)
                return false;

            amounts[type] = Get(type) - amount;
            Changed?.Invoke(type, amounts[type]);
            return true;
        }
    }
}
