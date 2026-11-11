namespace LabQuest
{
    /// <summary>
    /// Etapas da visita guiada, na ordem em que o visitante passa por elas.
    /// Cada etapa representa o objetivo atual do visitante.
    /// </summary>
    public enum TourStage
    {
        /// <summary>Recepção: pegar o tablet de boas-vindas.</summary>
        Welcome,

        /// <summary>Teleportar até a área de Coworking.</summary>
        Coworking,

        /// <summary>Pegar e examinar o troféu na bancada da AMAGAMES.</summary>
        Amagames,

        /// <summary>Apertar o botão do púlpito para acionar o telão da Arena.</summary>
        Arena,

        /// <summary>Responder o quiz final.</summary>
        Quiz,

        /// <summary>Validar o crachá no terminal de saída.</summary>
        Badge,

        /// <summary>Tour encerrado.</summary>
        Completed,
    }
}
