using LEDSystem.UI.Helpers;

namespace LEDSystem.Core.Interfaces
{
    public interface IEffect
    {
        /// <summary>
        /// Esegue le istruzioni all'interno prima che l'effetto venga eseguito.
        /// </summary>
        void OnCreate();
        /// <summary>
        /// Esegue le istruzioni all'interno in un loop continuo.
        /// </summary>
        void OnUpdate();
        /// <summary>
        /// Esegue le istruzioni all'interno su chiamata da parte del gestore.
        /// </summary>
        void OnAction(Tint data);
        /// <summary>
        /// Esegue le istruzioni prima che l'effetto venga rilasciato.
        /// </summary>
        void OnClosed();
    }
}