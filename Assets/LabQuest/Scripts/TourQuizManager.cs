using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LabQuest
{
    /// <summary>
    /// Máquina de estados da visita guiada e do quiz final:
    /// Recepção -> Coworking -> AMAGAMES -> Arena -> Quiz -> Crachá -> Concluído.
    /// As interações da cena avisam o manager com <see cref="CompleteStage"/> (normalmente via <see cref="TourStepTrigger"/>),
    /// e uma etapa só é aceita quando é a etapa atual, o que garante a ordem da trilha.
    /// </summary>
    public class TourQuizManager : MonoBehaviour
    {
        [Serializable]
        class StageSettings
        {
            public TourStage stage;

            [TextArea(2, 4)]
            [Tooltip("Texto mostrado no painel de instruções ao entrar na etapa.")]
            public string instructions;

            [Tooltip("Fala do NPC guia ao entrar na etapa (opcional).")]
            public AudioClip narration;

            [Tooltip("Objetos ativados ao entrar na etapa (ex.: âncoras de teleporte, painéis do POI).")]
            public GameObject[] enableOnEnter;

            [Tooltip("Objetos desativados ao entrar na etapa.")]
            public GameObject[] disableOnEnter;

            public UnityEvent onEnter = new UnityEvent();
        }

        [Header("Trilha")]
        [SerializeField]
        List<StageSettings> m_Stages = new List<StageSettings>();

        [SerializeField]
        TMP_Text m_InstructionsText;

        [SerializeField]
        [Tooltip("AudioSource do NPC guia, usado para as narrações de cada etapa.")]
        AudioSource m_GuideAudioSource;

        [SerializeField]
        [Tooltip("AudioSource para os sons de acerto/erro. Se vazio, usa o do NPC guia.")]
        AudioSource m_SfxAudioSource;

        [Header("Quiz")]
        [SerializeField]
        GameObject m_QuizPanel;

        [SerializeField]
        TMP_Text m_QuestionText;

        [SerializeField]
        [Tooltip("Mostra \"Pergunta 1/2\" (opcional).")]
        TMP_Text m_QuestionProgressText;

        [SerializeField]
        [Tooltip("Um botão por alternativa, na mesma ordem de Options. O texto vem do TMP_Text filho do botão.")]
        Button[] m_OptionButtons;

        [SerializeField]
        TMP_Text m_FeedbackText;

        [SerializeField]
        List<QuizQuestion> m_Questions = new List<QuizQuestion>();

        [SerializeField]
        [Tooltip("Acertos necessários para ganhar o crachá. Se errar, o quiz recomeça.")]
        int m_MinCorrectAnswers = 2;

        [SerializeField]
        [Tooltip("Segundos que o feedback de cada resposta fica na tela antes da próxima pergunta.")]
        float m_FeedbackDuration = 1.5f;

        [SerializeField]
        AudioClip m_CorrectSound;

        [SerializeField]
        AudioClip m_WrongSound;

        [Header("Eventos")]
        public UnityEvent<TourStage> stageChanged = new UnityEvent<TourStage>();
        public UnityEvent quizPassed = new UnityEvent();
        public UnityEvent quizFailed = new UnityEvent();
        public UnityEvent tourCompleted = new UnityEvent();

        int m_QuestionIndex;
        int m_CorrectAnswers;
        bool m_WaitingFeedback;

        public TourStage currentStage { get; private set; }

        void Awake()
        {
            for (var i = 0; i < m_OptionButtons.Length; i++)
            {
                var optionIndex = i;
                m_OptionButtons[i].onClick.AddListener(() => AnswerQuestion(optionIndex));
            }
        }

        void Start()
        {
            EnterStage(TourStage.Welcome);
        }

        /// <summary>
        /// Conclui a etapa informada e avança para a próxima, se ela for a etapa atual.
        /// O quiz não é concluído por aqui: ele avança sozinho pelas respostas.
        /// </summary>
        /// <returns>True se a etapa foi aceita.</returns>
        public bool CompleteStage(TourStage stage)
        {
            if (stage != currentStage || stage == TourStage.Quiz || stage == TourStage.Completed)
                return false;

            EnterStage(stage + 1);
            return true;
        }

        /// <summary>
        /// Registra a resposta do visitante para a pergunta atual.
        /// </summary>
        public void AnswerQuestion(int optionIndex)
        {
            if (currentStage != TourStage.Quiz || m_WaitingFeedback)
                return;

            var correct = optionIndex == m_Questions[m_QuestionIndex].correctOptionIndex;
            if (correct)
                m_CorrectAnswers++;

            PlaySfx(correct ? m_CorrectSound : m_WrongSound);
            SetText(m_FeedbackText, correct ? "Resposta correta!" : "Resposta incorreta.");
            StartCoroutine(NextQuestionAfterFeedback());
        }

        void EnterStage(TourStage stage)
        {
            currentStage = stage;

            var settings = m_Stages.Find(s => s.stage == stage);
            if (settings != null)
            {
                SetActive(settings.enableOnEnter, true);
                SetActive(settings.disableOnEnter, false);
                SetText(m_InstructionsText, settings.instructions);
                PlayNarration(settings.narration);
                settings.onEnter.Invoke();
            }

            if (m_QuizPanel != null)
                m_QuizPanel.SetActive(stage == TourStage.Quiz);

            if (stage == TourStage.Quiz)
                StartQuiz();

            stageChanged.Invoke(stage);

            if (stage == TourStage.Completed)
                tourCompleted.Invoke();
        }

        void StartQuiz()
        {
            if (m_Questions.Count == 0)
            {
                Debug.LogWarning("TourQuizManager: nenhuma pergunta cadastrada, pulando o quiz.", this);
                quizPassed.Invoke();
                EnterStage(TourStage.Badge);
                return;
            }

            m_QuestionIndex = 0;
            m_CorrectAnswers = 0;
            ShowQuestion();
        }

        void ShowQuestion()
        {
            var question = m_Questions[m_QuestionIndex];
            SetText(m_QuestionText, question.prompt);
            SetText(m_QuestionProgressText, $"Pergunta {m_QuestionIndex + 1}/{m_Questions.Count}");
            SetText(m_FeedbackText, string.Empty);

            for (var i = 0; i < m_OptionButtons.Length; i++)
            {
                var hasOption = i < question.options.Length;
                m_OptionButtons[i].gameObject.SetActive(hasOption);
                if (hasOption)
                    SetText(m_OptionButtons[i].GetComponentInChildren<TMP_Text>(), question.options[i]);
            }
        }

        IEnumerator NextQuestionAfterFeedback()
        {
            m_WaitingFeedback = true;
            yield return new WaitForSeconds(m_FeedbackDuration);
            m_WaitingFeedback = false;

            m_QuestionIndex++;
            if (m_QuestionIndex < m_Questions.Count)
            {
                ShowQuestion();
                yield break;
            }

            if (m_CorrectAnswers >= Mathf.Min(m_MinCorrectAnswers, m_Questions.Count))
            {
                quizPassed.Invoke();
                EnterStage(TourStage.Badge);
                yield break;
            }

            quizFailed.Invoke();
            SetText(m_FeedbackText, $"Você acertou {m_CorrectAnswers} de {m_Questions.Count}. Tente novamente!");
            m_WaitingFeedback = true;
            yield return new WaitForSeconds(m_FeedbackDuration);
            m_WaitingFeedback = false;
            StartQuiz();
        }

        void PlayNarration(AudioClip clip)
        {
            if (m_GuideAudioSource == null || clip == null)
                return;

            m_GuideAudioSource.Stop();
            m_GuideAudioSource.clip = clip;
            m_GuideAudioSource.Play();
        }

        void PlaySfx(AudioClip clip)
        {
            var source = m_SfxAudioSource != null ? m_SfxAudioSource : m_GuideAudioSource;
            if (source != null && clip != null)
                source.PlayOneShot(clip);
        }

        static void SetActive(GameObject[] objects, bool active)
        {
            if (objects == null)
                return;

            foreach (var obj in objects)
            {
                if (obj != null)
                    obj.SetActive(active);
            }
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        /// <summary>
        /// Atalho para testar a trilha no Editor sem headset: conclui a etapa atual
        /// (no quiz, responde a alternativa correta).
        /// </summary>
        [ContextMenu("Avançar etapa (teste)")]
        void AdvanceForTesting()
        {
            if (currentStage == TourStage.Quiz)
                AnswerQuestion(m_Questions[m_QuestionIndex].correctOptionIndex);
            else
                CompleteStage(currentStage);
        }
    }
}
