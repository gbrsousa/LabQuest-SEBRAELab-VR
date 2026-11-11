using System;
using UnityEngine;

namespace LabQuest
{
    /// <summary>
    /// Uma pergunta de múltipla escolha do quiz final.
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 4)]
        public string prompt;

        public string[] options = new string[4];

        [Tooltip("Índice (começando em 0) da alternativa correta em Options.")]
        public int correctOptionIndex;
    }
}
