using System;
using UnityEngine;

namespace MalaCosecha.Player
{
    /// <summary>
    /// Resistencia del jugador. Correr y saltar la gastan; se recupera sola tras una pausa.
    ///
    /// Si llega a cero, el jugador queda "agotado": no puede volver a correr hasta recuperar
    /// un porcentaje mínimo. Eso evita el "tartamudeo" de correr-parar-correr con la barra en 0
    /// y crea tensión: quedarse sin aire lejos de la luz es peligroso.
    ///
    /// Este componente NO sabe quién lo usa (correr, saltar, en el futuro quizás esquivar).
    /// Solo administra un número. Separar responsabilidades así es lo que hace que el código
    /// crezca sin enredarse.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxStamina = 100f;

        [Tooltip("Puntos que se recuperan por segundo.")]
        [SerializeField, Min(0f)] float regenPerSecond = 20f;

        [Tooltip("Segundos sin gastar resistencia antes de empezar a recuperarla.")]
        [SerializeField, Min(0f)] float regenDelay = 1f;

        [Tooltip("Al agotarse, porcentaje (0-1) que hay que recuperar para volver a usarla.")]
        [SerializeField, Range(0f, 1f)] float exhaustionRecoveryThreshold = 0.3f;

        public float Current { get; private set; }
        public float Max => maxStamina;
        public float Normalized => Current / maxStamina;
        public bool IsExhausted { get; private set; }
        public bool HasStamina => !IsExhausted && Current > 0f;

        /// <summary>Se dispara cuando cambia el valor. Envía el valor normalizado (0-1).</summary>
        public event Action<float> Changed;

        float lastUseTime = float.NegativeInfinity;

        void Awake()
        {
            Current = maxStamina;
        }

        void Update()
        {
            if (Current >= maxStamina || Time.time - lastUseTime < regenDelay)
                return;

            SetCurrent(Current + regenPerSecond * Time.deltaTime);

            if (IsExhausted && Normalized >= exhaustionRecoveryThreshold)
                IsExhausted = false;
        }

        /// <summary>Gasto continuo (ej: correr). Pasale "por segundo * Time.deltaTime".</summary>
        public void Drain(float amount)
        {
            if (amount <= 0f)
                return;

            lastUseTime = Time.time;
            SetCurrent(Current - amount);

            if (Current <= 0f)
                IsExhausted = true;
        }

        /// <summary>Gasto puntual (ej: saltar). Solo gasta si alcanza; devuelve si pudo.</summary>
        public bool TryConsume(float amount)
        {
            if (IsExhausted || Current < amount)
                return false;

            Drain(amount);
            return true;
        }

        void SetCurrent(float value)
        {
            value = Mathf.Clamp(value, 0f, maxStamina);
            if (Mathf.Approximately(value, Current))
                return;

            Current = value;
            Changed?.Invoke(Normalized);
        }
    }
}
