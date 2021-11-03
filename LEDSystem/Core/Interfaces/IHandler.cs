using System;
using LEDSystem.UI.Helpers;

namespace LEDSystem.Core.Interfaces
{
    public interface IHandler
    {
        /// <summary>
        /// Invia al dispositivo seriale una variazione dello stato dei pin.
        /// </summary>
        void OnEffectData(int type, int index, byte code, byte alpha, byte red, byte green, byte blue);
        /// <summary>
        /// Arresta l'effetto a seguito di un errore che ne ha interrotto l'esecuzione.
        /// </summary>
        void OnEffectError();
        /// <summary>
        /// Mostra la finestra dell'editor del colore su richiesta dell'effetto.
        /// </summary>
        void OnColorPicker(int type, Tint tint);
        /// <summary>
        /// Mostra la finestra dell'editor del colore su richiesta dell'effetto.
        /// </summary>
        void OnConfirmRequest(Action<int> callback);
        /// <summary>
        /// Avvia il timer dell'effetto.
        /// </summary>
        void StartTimer();
        /// <summary>
        /// Arresta il timer dell'effetto.
        /// </summary>
        void StopTimer();
        /// <summary>
        /// Imposta un intervallo per il timer dell'effetto.
        /// </summary>
        void SetInterval(int interval);
    }
}