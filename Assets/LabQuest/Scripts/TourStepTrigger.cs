using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace LabQuest
{
    /// <summary>
    /// Conclui uma etapa do <see cref="TourQuizManager"/> quando o visitante interage com este objeto.
    /// Coloque no tablet, na âncora de teleporte do POI, no troféu, no botão do púlpito ou no terminal do crachá.
    /// </summary>
    public class TourStepTrigger : MonoBehaviour
    {
        enum TriggerMode
        {
            /// <summary>Só pelo método <see cref="Trigger"/> (ligue em Button.onClick, eventos de um XR Socket, etc.).</summary>
            Manual,

            /// <summary>Quando o interactable deste objeto é pego/selecionado (XR Grab, XR Simple Interactable).</summary>
            OnSelectEntered,

            /// <summary>Quando o visitante se teleporta para a Teleportation Anchor/Area deste objeto.</summary>
            OnTeleported,
        }

        [SerializeField]
        [Tooltip("Se vazio, procura o TourQuizManager da cena.")]
        TourQuizManager m_Manager;

        [SerializeField]
        [Tooltip("Etapa concluída por esta interação.")]
        TourStage m_Stage;

        [SerializeField]
        TriggerMode m_Mode = TriggerMode.OnSelectEntered;

        [SerializeField]
        [Tooltip("Áudio tocado na interação (ex.: narração espacial do troféu). Opcional.")]
        AudioSource m_AudioOnTrigger;

        [SerializeField]
        [Tooltip("Espera o áudio acima terminar antes de avançar a trilha.")]
        bool m_WaitForAudio = true;

        [SerializeField]
        [Tooltip("Segundos extras antes de avançar a trilha (ex.: duração do vídeo do telão).")]
        float m_ExtraDelay;

        [Tooltip("Chamado quando a interação é aceita (na etapa certa), antes da espera. Ex.: ligar o telão.")]
        public UnityEvent onTriggered = new UnityEvent();

        XRBaseInteractable m_Interactable;
        bool m_Triggered;

        void Awake()
        {
            if (m_Manager == null)
                m_Manager = FindAnyObjectByType<TourQuizManager>();

            m_Interactable = GetComponent<XRBaseInteractable>();
            if (m_Mode != TriggerMode.Manual && m_Interactable == null)
                Debug.LogWarning($"TourStepTrigger: modo {m_Mode} precisa de um XR Interactable no mesmo objeto.", this);
        }

        void OnEnable()
        {
            if (m_Interactable == null)
                return;

            if (m_Mode == TriggerMode.OnSelectEntered)
                m_Interactable.selectEntered.AddListener(OnSelectEntered);
            else if (m_Mode == TriggerMode.OnTeleported && m_Interactable is BaseTeleportationInteractable teleport)
                teleport.teleporting.AddListener(OnTeleporting);
        }

        void OnDisable()
        {
            if (m_Interactable == null)
                return;

            m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
            if (m_Interactable is BaseTeleportationInteractable teleport)
                teleport.teleporting.RemoveListener(OnTeleporting);
        }

        void OnSelectEntered(SelectEnterEventArgs args) => Trigger();

        void OnTeleporting(TeleportingEventArgs args) => Trigger();

        /// <summary>
        /// Conclui a etapa configurada, se ela for a etapa atual da trilha. Interações fora de ordem são ignoradas.
        /// </summary>
        public void Trigger()
        {
            if (m_Triggered || m_Manager == null || m_Manager.currentStage != m_Stage)
                return;

            m_Triggered = true;
            onTriggered.Invoke();
            StartCoroutine(CompleteAfterDelay());
        }

        IEnumerator CompleteAfterDelay()
        {
            var delay = m_ExtraDelay;
            if (m_AudioOnTrigger != null)
            {
                m_AudioOnTrigger.Play();
                if (m_WaitForAudio && m_AudioOnTrigger.clip != null)
                    delay += m_AudioOnTrigger.clip.length;
            }

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            m_Manager.CompleteStage(m_Stage);
        }
    }
}
