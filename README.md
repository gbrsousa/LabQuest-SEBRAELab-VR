# Lab Quest — Visita Guiada Imersiva ao SEBRAE Lab

Experiência em Realidade Virtual para o **Meta Quest 2** que guia o visitante pelos principais espaços do SEBRAE Lab (Recepção, Coworking, Bancada AMAGAMES e Arena) e termina com um quiz e o crachá virtual de **"Inovador do SEBRAE Lab"**.

Projeto da disciplina **Tecnologias Emergentes 2026.2** — grupo **LabVR Studio**.

---

## Requisitos

| Item | Detalhe |
|---|---|
| Unity | **6000.4.10f1** (exatamente esta versão) |
| Módulo do Unity | **Android Build Support** com **OpenJDK** e **Android SDK & NDK Tools** |
| Headset | Meta Quest 2 em **modo desenvolvedor** |
| Cabo | USB-C que **transfira dados** (nem todo cabo de carregar serve) |

> ⚠️ Abra o projeto **somente** no Unity 6000.4.10f1. Outras versões atualizam os pacotes do projeto e podem quebrar a cena para o resto do grupo.

---

## 1. Baixar e abrir o projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com/gbrsousa/LabQuest-SEBRAELab-VR.git
   ```
2. No **Unity Hub**, clique em **Add → Add project from disk** e selecione a pasta clonada.
3. Abra com o **Unity 6000.4.10f1**. A primeira abertura demora alguns minutos (o Unity recria a pasta `Library`).
4. Na janela **Project**, abra `Assets/Scenes/MainScene_SebraeLab.unity`.

---

## 2. Testar no PC (sem headset)

Útil para conferir o fluxo antes de usar o Quest.

1. **Edit → Project Settings → XR Plug-in Management → XR Interaction Toolkit** e marque **Use XR Interaction Simulator in scenes** (aceite importar o sample, se pedir). Deixe **Instantiate In Editor Only** marcado.
2. Clique em **▶ Play**.

| Ação | Tecla |
|---|---|
| Andar / subir e descer | `W` `A` `S` `D` / `Q` `E` |
| Olhar / mirar | botão direito do mouse + mover o mouse |
| Alternar cabeça / controle esquerdo / controle direito | `Tab` |
| Grip (pegar, apertar botão vermelho) | `G` |
| Gatilho (clicar em painéis) | `T` |
| Analógico (teleporte) | `I` `J` `K` `L` |

**Atalho para testar só a lógica:** com o Play ligado, selecione **TourQuizManager** na Hierarchy → no Inspector, menu **⋮** do componente → **Avançar etapa (teste)**.

> Avisos amarelos sobre OpenXR no Console são normais sem headset conectado.

---

## 3. Gerar o app para o Quest

1. **File → Build Profiles** → selecione **Android** → **Switch Platform** (demora na primeira vez).
2. Confirme que `MainScene_SebraeLab` está marcada e em primeiro na **Scene List**.
3. **Edit → Project Settings → XR Plug-in Management → Project Validation** (aba Android): se houver itens com ⚠️/❌, clique em **Fix All**.
4. Em **Build Profiles**, clique em **Build** e salve o `.apk` numa pasta **fora** do projeto (ex.: `D:\Builds\LabQuest.apk`).

> O primeiro build leva de 15 a 30 minutos; os seguintes são bem mais rápidos.
> Se o projeto estiver numa pasta sincronizada (Google Drive/OneDrive), pause a sincronização durante o build para evitar erros de "Access denied".

---

## 4. Preparar o Quest 2

1. O Quest precisa estar em **modo desenvolvedor** (app **Meta Horizon** no celular do dono do headset → Dispositivos → Configurações do headset → Modo de desenvolvedor).
2. Ligue o Quest ao PC pelo cabo USB-C.
3. **Coloque o headset**: aparece **"Permitir depuração USB?"** → marque **"Sempre permitir neste computador"** e confirme.

---

## 5. Instalar e rodar no Quest

Escolha **uma** das opções:

### Opção A — Build And Run (gera e instala de uma vez)
**File → Build Profiles → Android → Run Device → Refresh**, escolha o Quest e clique em **Build And Run**. O app abre sozinho no headset.

### Opção B — Instalar um APK já gerado (sem buildar de novo)
Use o `adb` que vem com o Unity (ajuste o caminho de instalação do Unity e do `.apk`):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r "D:\Builds\LabQuest.apk"
```

Quando aparecer **Success**, está instalado.

### Opção C — Programa com interface
Instale o **Meta Quest Developer Hub** ou o **SideQuest** no PC, conecte o Quest e **arraste o `.apk`** para a janela do dispositivo.

**Para abrir depois:** no Quest, **Biblioteca → filtro "Fontes desconhecidas" → Lab Quest**.

---

## 6. Como jogar (controles do Quest)

| Ação | Controle |
|---|---|
| Pegar o tablet e o troféu, apertar o botão vermelho | **Grip** (botão lateral) |
| Responder o quiz e clicar em "Validar crachá" | **Gatilho** (dedo indicador) |
| Teleporte | **Analógico para frente**, mirar no marcador azul e soltar |

### Roteiro da visita (3–4 min)

1. **Recepção** — pegue o tablet sobre a mesa da recepção → o teleporte é liberado.
2. **Coworking** — teleporte até o marcador ao lado do lounge e leia o painel.
3. **Bancada AMAGAMES** — pegue e examine o troféu.
4. **Arena** — aperte o botão vermelho sobre a mesa de madeira → o conteúdo aparece na TV.
5. **Quiz** — responda às 2 perguntas no painel flutuante.
6. **Terminal do crachá** — perto da entrada, clique em **Validar crachá** e receba o crachá "Inovador do SEBRAE Lab".

---

## Problemas comuns

| Problema | Solução |
|---|---|
| Quest não aparece em **Run Device** | Aceite a depuração USB dentro do headset; troque o cabo/porta; no Windows, instale o **Oculus ADB Drivers** (site de desenvolvedores da Meta). |
| Build falha citando **Android XR** | **XR Plug-in Management → OpenXR** (aba Android): desmarque **Android XR Support** e mantenha **Meta Quest Support** marcado. |
| `IOException ... Assets\Resources ... is denied` no Console | Causado por sincronização de nuvem; o build costuma concluir mesmo assim. Pause a sincronização antes de buildar. |
| Unity pede para atualizar o projeto | Você abriu com a versão errada. Feche **sem salvar** e abra com o 6000.4.10f1. |

---

## Estrutura do projeto

| Caminho | Conteúdo |
|---|---|
| `Assets/Scenes/MainScene_SebraeLab.unity` | Cena principal da experiência |
| `Assets/LabQuest/Scripts/TourQuizManager.cs` | Máquina de estados da visita e do quiz |
| `Assets/LabQuest/Scripts/TourStepTrigger.cs` | Liga as interações da cena às etapas da visita |
| `Assets/LabQuest/Editor/MainSceneBuilder.cs` | Monta a cena (menu **LabQuest → Construir MainScene_SebraeLab**) |
| `Assets/Sebraelab3D/` | Modelo 3D do SEBRAE Lab |

> Rodar **LabQuest → Construir MainScene_SebraeLab** recria a cena do zero e descarta alterações feitas à mão nela.
