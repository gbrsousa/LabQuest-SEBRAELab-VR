using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LabQuest.EditorTools
{
    /// <summary>
    /// Monta a cena MainScene_SebraeLab com o modelo 3D do SEBRAE Lab, o rig de VR do template, os 4 POIs,
    /// as âncoras de teleporte, os painéis World Space e o TourQuizManager já ligado.
    /// Menu: LabQuest > Construir MainScene_SebraeLab. Rodar de novo recria a cena do zero.
    /// As posições abaixo estão em metros, no sistema de coordenadas do modelo (piso em y = 0).
    /// </summary>
    static class MainSceneBuilder
    {
        const string k_ScenePath = "Assets/Scenes/MainScene_SebraeLab.unity";
        const string k_MaterialsFolder = "Assets/LabQuest/Materials";
        const string k_ModelPath = "Assets/Sebraelab3D/Sebrae Lab.fbx";
        const string k_BenchPrefab = "Assets/Sebraelab3D/SM_MesaCompensado.fbx";
        const string k_RigPrefab = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
        const string k_TeleportAnchorPrefab = "Assets/VRTemplateAssets/Prefabs/Teleport/Teleport Anchor.prefab";
        const string k_ConfettiPrefab = "Assets/VRTemplateAssets/Prefabs/Blaster/Confetti.prefab";
        const string k_CorrectSound = "Assets/VRTemplateAssets/Audio/Button_22_click.wav";
        const string k_WrongSound = "Assets/VRTemplateAssets/Audio/Button_14_hover.wav";

        // Ordem dos valores do enum privado TourStepTrigger.TriggerMode.
        const int k_ModeManual = 0;
        const int k_ModeOnSelectEntered = 1;
        const int k_ModeOnTeleported = 2;

        static readonly Color k_PanelColor = new Color(0.06f, 0.09f, 0.16f, 0.92f);
        static readonly Color k_ButtonColor = new Color(0.15f, 0.39f, 0.92f, 1f);

        [MenuItem("LabQuest/Construir MainScene_SebraeLab")]
        static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (File.Exists(k_ScenePath) && !EditorUtility.DisplayDialog("LabQuest",
                    $"{k_ScenePath} já existe e será recriada do zero. Continuar?", "Recriar", "Cancelar"))
                return;

            Build();
        }

        /// <summary>
        /// Ponto de entrada para linha de comando: -executeMethod LabQuest.EditorTools.MainSceneBuilder.Build
        /// </summary>
        public static void Build()
        {
            ConfigureModelImport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mats = new Materials();

            // Ponto de partida: no saguão de entrada, logo depois da porta, de frente para a recepção.
            var spawn = new Vector3(18.4f, 0f, 7.4f);
            const float spawnYaw = 165f;

            // Sistema
            var manager = new GameObject("TourQuizManager").AddComponent<TourQuizManager>();
            CreateLight();
            CreateEventSystem();
            CreateRig(spawn, spawnYaw);

            // Ambiente: modelo do SEBRAE Lab
            CreateModel();

            // POI 1 - Recepção (saguão de entrada, mesa da recepção do modelo com tampo em y = 0.73)
            var reception = new GameObject("POI 1 - Recepcao").transform;
            var tablet = CreateTablet(reception, mats, new Vector3(18.85f, 0.74f, 5.95f), spawnYaw);
            AddTrigger(tablet, manager, TourStage.Welcome, k_ModeOnSelectEntered);
            CreateNpc(reception, mats, new Vector3(17.55f, 0.9f, 5.9f), spawn);
            InfoPanel("Painel Boas-vindas", reception, new Vector3(18.6f, 1.95f, 4.9f), YawTo(spawn, new Vector3(18.6f, 0f, 4.9f)), new Vector2(900, 300),
                "Bem-vindo ao SEBRAE Lab",
                "Esta visita guiada apresenta os principais espaços do laboratório. Siga as instruções do painel do guia.");
            var anchorReception = Anchor("Ancora Recepcao", reception, spawn, spawnYaw);

            // POI 2 - Coworking (ao lado do lounge, de frente para as estações de trabalho de vidro)
            var coworking = new GameObject("POI 2 - Coworking").transform;
            InfoPanel("Painel Coworking", coworking, new Vector3(13.4f, 1.65f, 1.5f), -90f, new Vector2(1000, 420),
                "Coworking",
                "Espaço compartilhado onde empreendedores, estudantes e startups trabalham lado a lado, fazem networking e participam de mentorias e eventos.");
            var anchorCoworking = Anchor("Ancora Coworking", coworking, new Vector3(15.3f, 0f, 1.8f), -90f);
            AddTrigger(anchorCoworking.GetComponentInChildren<BaseTeleportationInteractable>(true).gameObject,
                manager, TourStage.Coworking, k_ModeOnTeleported);

            // POI 3 - AMAGAMES (bancada nova na área livre a oeste do salão)
            var amagames = new GameObject("POI 3 - AMAGAMES").transform;
            var bench = Prefab(k_BenchPrefab, amagames, new Vector3(9.6f, 0f, 6.3f));
            bench.name = "Bancada AMAGAMES";
            bench.AddComponent<BoxCollider>();
            MakeStatic(bench);
            Box("Suporte Monitor", amagames, new Vector3(9.25f, 0.84f, 6.45f), new Vector3(0.08f, 0.06f, 0.08f), mats.dark);
            Box("Monitor", amagames, new Vector3(9.25f, 1.06f, 6.45f), new Vector3(0.6f, 0.38f, 0.04f), mats.dark);
            var trophy = CreateTrophy(amagames, mats, new Vector3(9.95f, 0.81f, 6.2f));
            var trophyAudio = trophy.GetComponent<AudioSource>();
            AddTrigger(trophy, manager, TourStage.Amagames, k_ModeOnSelectEntered, trophyAudio, 1f);
            InfoPanel("Painel AMAGAMES", amagames, new Vector3(9.6f, 1.75f, 6.75f), 0f, new Vector2(1000, 420),
                "Bancada AMAGAMES",
                "Espaço da comunidade local de desenvolvimento de jogos. Pegue o troféu sobre a bancada para ouvir sobre os eventos e projetos de games.");
            var anchorAmagames = Anchor("Ancora AMAGAMES", amagames, new Vector3(9.6f, 0f, 4.7f), 0f);

            // POI 4 - Arena (área de apresentações: TV, mesa de madeira e arquibancada do modelo)
            var arena = new GameObject("POI 4 - Arena").transform;
            var (slide, video) = CreateScreen(arena, mats);
            var pulpitButton = CreatePulpitButton(arena, mats, new Vector3(11.2f, 0.827f, 11.85f));
            var arenaTrigger = AddTrigger(pulpitButton, manager, TourStage.Arena, k_ModeOnSelectEntered, null, 5f);
            UnityEventTools.AddBoolPersistentListener(arenaTrigger.onTriggered, slide.SetActive, true);
            UnityEventTools.AddPersistentListener(arenaTrigger.onTriggered, video.Play);
            var arenaSpot = new Vector3(10.9f, 0f, 10f);
            var anchorArena = Anchor("Ancora Arena", arena, arenaSpot, 0f);
            var quizPosition = new Vector3(12.9f, 1.5f, 10.5f);
            var quiz = CreateQuizPanel(arena, quizPosition, YawTo(arenaSpot, quizPosition));

            // Saída - Terminal do crachá (passagem entre o salão e o saguão de entrada)
            var exit = new GameObject("Saida - Terminal Cracha").transform;
            var kiosk = Box("Terminal", exit, new Vector3(16.4f, 0.6f, 7.4f), new Vector3(0.8f, 1.2f, 0.4f), mats.dark);
            kiosk.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var terminalPanel = InfoPanel("Painel Terminal", exit, new Vector3(16.18f, 1.55f, 7.4f), 90f, new Vector2(700, 420),
                "Terminal de Crachá", "Conclua o quiz e valide aqui o seu crachá de visitante.", true);
            var validateButton = MakeButton("Botao Validar", terminalPanel, "Validar crachá",
                new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.3f));
            var terminalTrigger = AddTrigger(terminalPanel.gameObject, manager, TourStage.Badge, k_ModeManual);
            UnityEventTools.AddVoidPersistentListener(validateButton.onClick, terminalTrigger.Trigger);
            var badge = CreateBadge(exit, mats, new Vector3(16.0f, 1.25f, 7.4f));
            badge.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var confetti = Prefab(k_ConfettiPrefab, exit, new Vector3(16.2f, 2.1f, 7.4f));
            var anchorExit = Anchor("Ancora Saida", exit, new Vector3(14.8f, 0f, 7.4f), 90f);

            // UI do guia (segue o olhar do visitante)
            var guide = CreateGuidePanel(spawn, spawnYaw);

            // Liga tudo no TourQuizManager
            var so = new SerializedObject(manager);
            so.FindProperty("m_InstructionsText").objectReferenceValue = guide.text;
            so.FindProperty("m_GuideAudioSource").objectReferenceValue = guide.audio;
            so.FindProperty("m_QuizPanel").objectReferenceValue = quiz.panel;
            so.FindProperty("m_QuestionText").objectReferenceValue = quiz.question;
            so.FindProperty("m_QuestionProgressText").objectReferenceValue = quiz.progress;
            so.FindProperty("m_FeedbackText").objectReferenceValue = quiz.feedback;
            so.FindProperty("m_CorrectSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(k_CorrectSound);
            so.FindProperty("m_WrongSound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(k_WrongSound);
            SetObjectArray(so.FindProperty("m_OptionButtons"), quiz.options);

            SetQuestions(so.FindProperty("m_Questions"), new[]
            {
                ("Qual espaço do SEBRAE Lab é voltado ao trabalho compartilhado e ao networking entre empreendedores?",
                    new[] { "Arena", "Coworking", "Recepção", "Bancada AMAGAMES" }, 1),
                ("Em qual espaço acontecem as apresentações de pitches das startups?",
                    new[] { "Bancada AMAGAMES", "Recepção", "Arena", "Coworking" }, 2),
            });

            SetStages(so.FindProperty("m_Stages"), new[]
            {
                (TourStage.Welcome,
                    "Bem-vindo! Pegue o tablet sobre a mesa da recepção (botão Grip) para começar a visita.",
                    new GameObject[0], new GameObject[0]),
                (TourStage.Coworking,
                    "Teleporte liberado! Mire o marcador na área de Coworking com o analógico e solte para se teleportar.",
                    new[] { anchorReception, anchorCoworking }, new GameObject[0]),
                (TourStage.Amagames,
                    "Agora vá até a bancada da AMAGAMES e pegue o troféu para ouvir sobre a comunidade de jogos.",
                    new[] { anchorAmagames }, new GameObject[0]),
                (TourStage.Arena,
                    "Siga para a Arena e aperte o botão vermelho sobre a mesa de madeira para ligar o telão.",
                    new[] { anchorArena }, new GameObject[0]),
                (TourStage.Quiz,
                    "Hora do quiz! Responda às perguntas no painel usando o raio do controle.",
                    new GameObject[0], new GameObject[0]),
                (TourStage.Badge,
                    "Parabéns, você acertou o quiz! Vá até o terminal perto da entrada e valide o seu crachá.",
                    new[] { anchorExit }, new GameObject[0]),
                (TourStage.Completed,
                    "Tour concluído! Você agora é um Inovador do SEBRAE Lab.",
                    new[] { badge, confetti }, new GameObject[0]),
            });
            so.ApplyModifiedPropertiesWithoutUndo();

            // Estado inicial: âncoras e recompensas só aparecem quando a etapa libera.
            foreach (var go in new[] { anchorReception, anchorCoworking, anchorAmagames, anchorArena, anchorExit, badge, confetti, slide, quiz.panel })
            {
                if (go != null)
                    go.SetActive(false);
            }

            EditorSceneManager.SaveScene(scene, k_ScenePath);
            AddSceneToBuildSettings();
            Debug.Log($"LabQuest: cena criada em {k_ScenePath}");
        }

        class Materials
        {
            public readonly Material accent = Mat("Destaque Azul", new Color(0.1f, 0.35f, 0.75f));
            public readonly Material dark = Mat("Escuro", new Color(0.12f, 0.12f, 0.14f));
            public readonly Material gold = Mat("Dourado", new Color(0.95f, 0.75f, 0.2f), 0.8f);
            public readonly Material red = Mat("Vermelho", new Color(0.85f, 0.15f, 0.15f));
            public readonly Material npc = Mat("NPC", new Color(0.1f, 0.7f, 0.6f));

            static Material Mat(string name, Color color, float metallic = 0f)
            {
                if (!AssetDatabase.IsValidFolder(k_MaterialsFolder))
                    AssetDatabase.CreateFolder("Assets/LabQuest", "Materials");

                var path = $"{k_MaterialsFolder}/{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, path);
                }

                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Metallic", metallic);
                EditorUtility.SetDirty(mat);
                return mat;
            }
        }

        static void CreateLight()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            // Sem sombras a luz atravessa o teto e ilumina o interior antes de gerar a iluminação baked.
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            light.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        }

        static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
        }

        static void CreateRig(Vector3 position, float yaw)
        {
            var rig = Prefab(k_RigPrefab, null, position);
            if (rig == null)
                return;

            rig.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // A proposta usa só teleporte (evita enjoo): desliga a locomoção contínua dos analógicos.
            foreach (var move in rig.GetComponentsInChildren<ContinuousMoveProvider>(true))
                move.enabled = false;

            foreach (var behaviour in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour.GetType().Name != "ControllerInputActionManager")
                    continue;

                var so = new SerializedObject(behaviour);
                var smoothMotion = so.FindProperty("m_SmoothMotionEnabled");
                if (smoothMotion == null)
                    continue;

                smoothMotion.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Colisores (para objetos pegáveis não atravessarem o piso), UVs de lightmap (para o bake)
        /// e sem a câmera que vem dentro do FBX.
        /// </summary>
        static void ConfigureModelImport()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(k_ModelPath);
            if (importer == null || importer.addCollider && importer.generateSecondaryUV && !importer.importCameras && !importer.importLights)
                return;

            importer.addCollider = true;
            importer.generateSecondaryUV = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
        }

        static void CreateModel()
        {
            var model = Prefab(k_ModelPath, null, Vector3.zero);
            if (model == null)
                return;

            model.name = "SEBRAE Lab (modelo)";

            // O FBX também traz uma "biblioteca" de peças soltas (SM_*) longe do prédio e a câmera do Blender.
            foreach (Transform child in model.transform)
            {
                if (child.name.StartsWith("SM_") || child.name == "Camera")
                    child.gameObject.SetActive(false);
            }

            MakeStatic(model);
        }

        static void MakeStatic(GameObject root)
        {
            const StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        }

        /// <summary>Yaw (graus) para um objeto em <paramref name="to"/> ficar de frente para quem está em <paramref name="from"/>.</summary>
        static float YawTo(Vector3 from, Vector3 to)
        {
            return Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
        }

        static GameObject CreateTablet(Transform parent, Materials mats, Vector3 position, float yaw)
        {
            var tablet = Box("Tablet Boas-vindas", parent, position + new Vector3(0f, 0.0075f, 0f), new Vector3(0.26f, 0.015f, 0.18f), mats.dark, false);
            tablet.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            tablet.AddComponent<XRGrabInteractable>();

            var screen = Panel("Tela", tablet.transform, Vector3.zero, 0f, new Vector2(240, 160), new Color(0.1f, 0.35f, 0.75f, 1f), false);
            screen.localPosition = new Vector3(0f, 0.6f, 0f);
            screen.localRotation = Quaternion.Euler(90f, 0f, 0f);
            screen.localScale = new Vector3(1f / 260f, 1f / 180f, 1f);
            Text("Texto", screen, "Pegue para\ncomeçar", 30, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            return tablet;
        }

        static void CreateNpc(Transform parent, Materials mats, Vector3 position, Vector3 lookFrom)
        {
            Primitive(PrimitiveType.Capsule, "NPC Guia", parent, position, new Vector3(0.5f, 0.9f, 0.5f), mats.npc);
            var label = Panel("Nome", parent, position + new Vector3(0f, 1.15f, 0f), YawTo(lookFrom, position), new Vector2(360, 90), k_PanelColor, false);
            Text("Texto", label, "Guia do Lab", 44, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        }

        static GameObject CreateTrophy(Transform parent, Materials mats, Vector3 position)
        {
            var trophy = new GameObject("Trofeu AMAGAMES");
            trophy.transform.SetParent(parent, false);
            trophy.transform.position = position;
            Primitive(PrimitiveType.Cube, "Base", trophy.transform, position + new Vector3(0f, 0.025f, 0f), new Vector3(0.12f, 0.05f, 0.12f), mats.dark);
            Primitive(PrimitiveType.Cylinder, "Haste", trophy.transform, position + new Vector3(0f, 0.09f, 0f), new Vector3(0.03f, 0.04f, 0.03f), mats.gold);
            Primitive(PrimitiveType.Cylinder, "Taca", trophy.transform, position + new Vector3(0f, 0.18f, 0f), new Vector3(0.1f, 0.06f, 0.1f), mats.gold);
            trophy.AddComponent<XRGrabInteractable>();

            var audio = trophy.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            return trophy;
        }

        static (GameObject slide, VideoPlayer video) CreateScreen(Transform parent, Materials mats)
        {
            // O "telão" é a TV do modelo (em 9.79, 12.53, virada para -z): a tela fica colada na frente do painel dela (z ~ 12.37).
            var screen = Primitive(PrimitiveType.Quad, "Telao (tela da TV)", parent, new Vector3(9.79f, 1.0f, 12.355f), new Vector3(1.06f, 0.66f, 1f), mats.dark);
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var video = screen.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.renderMode = VideoRenderMode.MaterialOverride;
            video.targetMaterialRenderer = screen.GetComponent<Renderer>();
            video.targetMaterialProperty = "_BaseMap";

            // Conteúdo provisório do telão enquanto não há vídeo: some ao adicionar um clip no VideoPlayer.
            var slide = Panel("Slide Programas", parent, new Vector3(9.79f, 1.0f, 12.345f), 0f, new Vector2(1060, 660), k_PanelColor, false);
            Text("Titulo", slide, "Programas de apoio a startups", 64, new Vector2(0f, 0.62f), Vector2.one, TextAlignmentOptions.Center, FontStyles.Bold);
            Text("Corpo", slide, "Mentorias, capacitações, eventos de inovação e conexão com parceiros e investidores.",
                44, Vector2.zero, new Vector2(1f, 0.62f), TextAlignmentOptions.Center);
            return (slide.gameObject, video);
        }

        static GameObject CreatePulpitButton(Transform parent, Materials mats, Vector3 position)
        {
            var button = Primitive(PrimitiveType.Cylinder, "Botao Telao", parent, position, new Vector3(0.14f, 0.02f, 0.14f), mats.red);
            button.AddComponent<XRSimpleInteractable>();
            return button;
        }

        class QuizUI
        {
            public GameObject panel;
            public TMP_Text question;
            public TMP_Text progress;
            public TMP_Text feedback;
            public Button[] options;
        }

        static QuizUI CreateQuizPanel(Transform parent, Vector3 position, float yaw)
        {
            var panel = Panel("Painel Quiz", parent, position, yaw, new Vector2(1000, 680), k_PanelColor, true);
            Text("Titulo", panel, "Quiz Final", 52, new Vector2(0f, 0.86f), new Vector2(0.6f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);

            var ui = new QuizUI
            {
                panel = panel.gameObject,
                progress = Text("Progresso", panel, "Pergunta 1/2", 34, new Vector2(0.6f, 0.86f), Vector2.one, TextAlignmentOptions.Right),
                question = Text("Pergunta", panel, "Pergunta", 40, new Vector2(0f, 0.58f), new Vector2(1f, 0.86f), TextAlignmentOptions.Left),
                feedback = Text("Feedback", panel, string.Empty, 38, Vector2.zero, new Vector2(1f, 0.16f), TextAlignmentOptions.Center, FontStyles.Bold),
                options = new[]
                {
                    MakeButton("Alternativa A", panel, "A", new Vector2(0f, 0.38f), new Vector2(0.5f, 0.56f)),
                    MakeButton("Alternativa B", panel, "B", new Vector2(0.5f, 0.38f), new Vector2(1f, 0.56f)),
                    MakeButton("Alternativa C", panel, "C", new Vector2(0f, 0.18f), new Vector2(0.5f, 0.36f)),
                    MakeButton("Alternativa D", panel, "D", new Vector2(0.5f, 0.18f), new Vector2(1f, 0.36f)),
                },
            };
            return ui;
        }

        static GameObject CreateBadge(Transform parent, Materials mats, Vector3 position)
        {
            var badge = Box("Cracha Inovador", parent, position, new Vector3(0.09f, 0.13f, 0.006f), mats.accent, false);
            var body = badge.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var grab = badge.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;

            var face = Panel("Face", badge.transform, Vector3.zero, 0f, new Vector2(360, 520), new Color(0.1f, 0.35f, 0.75f, 1f), false);
            face.localPosition = new Vector3(0f, 0f, -0.6f);
            face.localRotation = Quaternion.identity;
            face.localScale = new Vector3(1f / 360f, 1f / 520f, 1f);
            Text("Texto", face, "<size=70%>CRACHÁ</size>\nINOVADOR\nDO SEBRAE LAB", 52, Vector2.zero, Vector2.one, TextAlignmentOptions.Center, FontStyles.Bold);
            return badge;
        }

        class GuideUI
        {
            public TMP_Text text;
            public AudioSource audio;
        }

        static GuideUI CreateGuidePanel(Vector3 spawn, float spawnYaw)
        {
            // Posição inicial à frente do ponto de partida; em jogo o LazyFollow passa a acompanhar o olhar.
            var position = spawn + Quaternion.Euler(0f, spawnYaw, 0f) * new Vector3(0f, 1.3f, 1.1f);
            var panel = Panel("Painel Guia", null, position, spawnYaw, new Vector2(760, 260), k_PanelColor, false);
            Text("Titulo", panel, "Guia do SEBRAE Lab", 34, new Vector2(0f, 0.72f), Vector2.one, TextAlignmentOptions.Left, FontStyles.Bold);
            var text = Text("Instrucoes", panel, string.Empty, 32, Vector2.zero, new Vector2(1f, 0.72f), TextAlignmentOptions.TopLeft);

            var follow = panel.gameObject.AddComponent<LazyFollow>();
            follow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
            follow.rotationFollowMode = LazyFollow.RotationFollowMode.LookAtWithWorldUp;
            follow.targetOffset = new Vector3(0f, -0.35f, 1.1f);

            var audio = panel.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            return new GuideUI { text = text, audio = audio };
        }

        static RectTransform InfoPanel(string name, Transform parent, Vector3 position, float yaw, Vector2 size, string title, string body, bool interactive = false)
        {
            var panel = Panel(name, parent, position, yaw, size, k_PanelColor, interactive);
            Text("Titulo", panel, title, 52, new Vector2(0f, 0.7f), Vector2.one, TextAlignmentOptions.Left, FontStyles.Bold);
            Text("Corpo", panel, body, 40, interactive ? new Vector2(0f, 0.32f) : Vector2.zero, new Vector2(1f, 0.7f), TextAlignmentOptions.TopLeft);
            return panel;
        }

        static GameObject Anchor(string name, Transform parent, Vector3 position, float yaw)
        {
            var anchor = Prefab(k_TeleportAnchorPrefab, parent, position);
            anchor.name = name;
            anchor.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return anchor;
        }

        static TourStepTrigger AddTrigger(GameObject go, TourQuizManager manager, TourStage stage, int mode, AudioSource audio = null, float extraDelay = 0f)
        {
            var trigger = go.AddComponent<TourStepTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("m_Manager").objectReferenceValue = manager;
            so.FindProperty("m_Stage").enumValueIndex = (int)stage;
            so.FindProperty("m_Mode").enumValueIndex = mode;
            so.FindProperty("m_AudioOnTrigger").objectReferenceValue = audio;
            so.FindProperty("m_ExtraDelay").floatValue = extraDelay;
            so.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
        }

        static void SetStages(SerializedProperty stages, (TourStage stage, string instructions, GameObject[] enable, GameObject[] disable)[] values)
        {
            stages.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                var element = stages.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("stage").enumValueIndex = (int)values[i].stage;
                element.FindPropertyRelative("instructions").stringValue = values[i].instructions;
                SetObjectArray(element.FindPropertyRelative("enableOnEnter"), values[i].enable);
                SetObjectArray(element.FindPropertyRelative("disableOnEnter"), values[i].disable);
            }
        }

        static void SetQuestions(SerializedProperty questions, (string prompt, string[] options, int correct)[] values)
        {
            questions.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                var element = questions.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("prompt").stringValue = values[i].prompt;
                element.FindPropertyRelative("correctOptionIndex").intValue = values[i].correct;
                var options = element.FindPropertyRelative("options");
                options.arraySize = values[i].options.Length;
                for (var j = 0; j < values[i].options.Length; j++)
                    options.GetArrayElementAtIndex(j).stringValue = values[i].options[j];
            }
        }

        static void SetObjectArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool isStatic = true)
        {
            var go = Primitive(PrimitiveType.Cube, name, parent, position, size, material);
            if (isStatic)
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            return go;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Prefab(string path, Transform parent, Vector3 position)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"LabQuest: prefab não encontrado: {path}");
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }

        static RectTransform Panel(string name, Transform parent, Vector3 position, float yaw, Vector2 size, Color background, bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            if (interactive)
                go.AddComponent<TrackedDeviceGraphicRaycaster>();
            go.AddComponent<Image>().color = background;

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.position = position;
            rect.rotation = Quaternion.Euler(0f, yaw, 0f);
            rect.localScale = Vector3.one * 0.001f;
            return rect;
        }

        static TextMeshProUGUI Text(string name, RectTransform parent, string content, float fontSize, Vector2 anchorMin, Vector2 anchorMax,
            TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(24f, 12f);
            rect.offsetMax = new Vector2(-24f, -12f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static Button MakeButton(string name, RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);

            var image = go.GetComponent<Image>();
            image.color = Color.white;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = k_ButtonColor;
            colors.highlightedColor = Color.Lerp(k_ButtonColor, Color.white, 0.3f);
            colors.pressedColor = Color.Lerp(k_ButtonColor, Color.black, 0.3f);
            colors.selectedColor = k_ButtonColor;
            button.colors = colors;

            Text("Texto", rect, label, 32, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            return button;
        }

        static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == k_ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(k_ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
