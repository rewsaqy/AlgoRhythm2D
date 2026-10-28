#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using AlgoRhythm.Game.Boss;
using AlgoRhythm.Game.Bootstrap;
using AlgoRhythm.Game.Presentation;

namespace AlgoRhythm.EditorTools
{
    public static class BossSceneBuilder
    {
        [MenuItem("Tools/Build Game (Linux Standalone)")]
        public static void BuildLinuxPlayer()
        {
            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Opening_1.unity",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/BossFightScene.unity"
            };

            string buildDir = "Build/Linux";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildDir + "/AlgoRhythm.x86_64",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opt);
            Debug.Log($"[Build] Linux build result: {report.summary.result}, output: {report.summary.outputPath}");
        }

        [MenuItem("Tools/Build Game (Windows Standalone)")]
        public static void BuildWindowsPlayer()
        {
            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Opening_1.unity",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/BossFightScene.unity"
            };

            string buildDir = "Build/Windows";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            BuildPlayerOptions opt = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildDir + "/AlgoRhythm.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opt);
            Debug.Log($"[Build] Windows build result: {report.summary.result}, output: {report.summary.outputPath}");
        }

        [MenuItem("Tools/Build Boss Fight Scene")]
        public static void BuildBossScene()
        {
            string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            string bossScenePath = "Assets/Scenes/BossFightScene.unity";

            // 1. Copy exact SampleScene to preserve all CRT shaders, lighting, camera and visual assets
            File.Copy(sampleScenePath, bossScenePath, true);
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.OpenScene(bossScenePath, OpenSceneMode.Single);

            // 2. Find AlgoRhythm root and clean up wave-based gameplay objects
            GameObject algoObj = GameObject.Find("AlgoRhythm");
            if (algoObj != null)
            {
                // Remove GameBootstrap
                var bootstrap = algoObj.GetComponent<GameBootstrap>();
                if (bootstrap != null) Object.DestroyImmediate(bootstrap);

                // Remove InputRouter GameObject to avoid null BeatClock references
                Transform inputObj = algoObj.transform.Find("Input");
                if (inputObj != null) Object.DestroyImmediate(inputObj.gameObject);

                // Remove or disable standard gameplay HUD, Tutorial, Music, Shop, CodePanel, Metronome, Sfx
                Transform hud = algoObj.transform.Find("HUD");
                if (hud != null) Object.DestroyImmediate(hud.gameObject);

                Transform tutorial = algoObj.transform.Find("Tutorial");
                if (tutorial != null) Object.DestroyImmediate(tutorial.gameObject);

                Transform music = algoObj.transform.Find("Music");
                if (music != null) Object.DestroyImmediate(music.gameObject);

                Transform metronome = algoObj.transform.Find("Metronome");
                if (metronome != null) Object.DestroyImmediate(metronome.gameObject);

                Transform codePanel = algoObj.transform.Find("CodePanel");
                if (codePanel != null) Object.DestroyImmediate(codePanel.gameObject);

                Transform shop = algoObj.transform.Find("Shop");
                if (shop != null) Object.DestroyImmediate(shop.gameObject);

                Transform sfxObj = algoObj.transform.Find("Sfx");
                if (sfxObj != null) Object.DestroyImmediate(sfxObj.gameObject);

                // Disable Core in Arena because A.I.D.A stole the Core!
                Transform arena = algoObj.transform.Find("Arena");
                if (arena != null)
                {
                    var arenaPres = arena.GetComponent<ArenaPresenter>();
                    if (arenaPres != null) Object.DestroyImmediate(arenaPres);

                    Transform core = arena.Find("Core");
                    if (core != null) Object.DestroyImmediate(core.gameObject);

                    Transform grade = arena.Find("Grade");
                    if (grade != null) Object.DestroyImmediate(grade.gameObject);

                    Transform threats = arena.Find("Threats");
                    if (threats != null) Object.DestroyImmediate(threats.gameObject);

                    Transform shields = arena.Find("Shields");
                    if (shields != null) Object.DestroyImmediate(shields.gameObject);

                    Transform ruleGuards = arena.Find("RuleGuards");
                    if (ruleGuards != null) Object.DestroyImmediate(ruleGuards.gameObject);

                    Transform south = arena.Find("South");
                    if (south != null) { Transform lbl = south.Find("Label"); if (lbl != null) Object.DestroyImmediate(lbl.gameObject); }

                    Transform north = arena.Find("North");
                    if (north != null) { Transform lbl = north.Find("Label"); if (lbl != null) Object.DestroyImmediate(lbl.gameObject); }
                }

                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Font monoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/JetBrainsMono-2.304/fonts/ttf/JetBrainsMono-Bold.ttf");

                // 3. Setup Neon Boundary Walls (Left & Right)
                GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftWall.name = "LeftWall";
                leftWall.transform.SetParent(algoObj.transform, false);
                leftWall.transform.localPosition = new Vector3(-6.2f, 0f, 0f);
                leftWall.transform.localScale = new Vector3(0.18f, 9.6f, 0.2f);
                Object.DestroyImmediate(leftWall.GetComponent<Collider>());
                Material wallMat = new Material(unlitShader) { color = new Color(0f, 1f, 0.5f, 0.65f) };
                leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

                GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightWall.name = "RightWall";
                rightWall.transform.SetParent(algoObj.transform, false);
                rightWall.transform.localPosition = new Vector3(6.2f, 0f, 0f);
                rightWall.transform.localScale = new Vector3(0.18f, 9.6f, 0.2f);
                Object.DestroyImmediate(rightWall.GetComponent<Collider>());
                rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;

                // Center divider net line (dashed retro ping-pong line)
                GameObject centerNet = new GameObject("CenterNet");
                centerNet.transform.SetParent(algoObj.transform, false);
                centerNet.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                for (int d = -6; d <= 6; d++)
                {
                    GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dash.transform.SetParent(centerNet.transform, false);
                    dash.transform.localPosition = new Vector3(d * 0.95f, 0f, 0f);
                    dash.transform.localScale = new Vector3(0.45f, 0.05f, 0.1f);
                    Object.DestroyImmediate(dash.GetComponent<Collider>());
                    dash.GetComponent<Renderer>().sharedMaterial = new Material(unlitShader) { color = new Color(0f, 0.8f, 0.4f, 0.25f) };
                }

                // 4. Setup A.I.D.A. Tower as Top Boss
                Transform aidaTowerTrans = algoObj.transform.Find("Tower") ?? algoObj.transform.Find("AidaTower");
                AidaBossController bossCtrl = null;
                if (aidaTowerTrans != null)
                {
                    aidaTowerTrans.localPosition = new Vector3(0f, 3.2f, 0f);
                    aidaTowerTrans.localScale = Vector3.one * 1.35f;

                    // Remove AidaTower component to prevent beat clock dependency
                    AidaTower tower = aidaTowerTrans.GetComponent<AidaTower>();
                    if (tower != null) Object.DestroyImmediate(tower);

                    // Clean up flavor, nameplate and shot views
                    Transform flavor = aidaTowerTrans.Find("Flavor");
                    if (flavor != null) Object.DestroyImmediate(flavor.gameObject);

                    Transform nameplate = aidaTowerTrans.Find("Nameplate");
                    if (nameplate != null) Object.DestroyImmediate(nameplate.gameObject);

                    Transform shots = aidaTowerTrans.Find("Shots");
                    if (shots != null) Object.DestroyImmediate(shots.gameObject);

                    bossCtrl = aidaTowerTrans.gameObject.GetComponent<AidaBossController>();
                    if (bossCtrl == null) bossCtrl = aidaTowerTrans.gameObject.AddComponent<AidaBossController>();

                    Transform body = aidaTowerTrans.Find("Body");
                    Transform barrelPivot = aidaTowerTrans.Find("BarrelPivot");
                    Transform barrel = barrelPivot != null ? barrelPivot.Find("Barrel") : null;

                    // Create Boss Deflector Shield Visual
                    GameObject bossShield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bossShield.name = "BossShieldVisual";
                    bossShield.transform.SetParent(aidaTowerTrans, false);
                    bossShield.transform.localPosition = new Vector3(0f, -0.65f, -0.1f);
                    bossShield.transform.localScale = new Vector3(1.8f, 0.22f, 0.2f);
                    Object.DestroyImmediate(bossShield.GetComponent<Collider>());
                    Material bossShieldMat = new Material(unlitShader) { color = new Color(1f, 0.3f, 0.2f) }; // Red neon shield
                    bossShield.GetComponent<Renderer>().sharedMaterial = bossShieldMat;

                    var bossSo = new SerializedObject(bossCtrl);
                    if (barrelPivot != null) bossSo.FindProperty("barrelPivot").objectReferenceValue = barrelPivot;
                    if (body != null)
                    {
                        var rend = body.GetComponent<Renderer>();
                        if (rend != null)
                        {
                            bossSo.FindProperty("bodyRenderer").objectReferenceValue = rend;
                            // Set Cyber Green Magnum Opus color without mutating package assets
                            if (rend.sharedMaterial != null)
                            {
                                Material instancedMat = new Material(rend.sharedMaterial);
                                if (instancedMat.HasProperty("_Color")) instancedMat.color = new Color(0.85f, 0.98f, 1f);
                                if (instancedMat.HasProperty("_BaseColor")) instancedMat.SetColor("_BaseColor", new Color(0.85f, 0.98f, 1f));
                                rend.material = instancedMat;
                            }
                        }
                    }
                    if (barrel != null)
                    {
                        var rend = barrel.GetComponent<Renderer>();
                        if (rend != null) bossSo.FindProperty("barrelRenderer").objectReferenceValue = rend;
                    }
                    bossSo.FindProperty("shieldVisual").objectReferenceValue = bossShield.transform;
                    bossSo.ApplyModifiedProperties();
                }

                // 5. Setup Player Paddle (Bottom)
                GameObject paddleObj = new GameObject("PlayerPaddle");
                paddleObj.transform.SetParent(algoObj.transform, false);
                paddleObj.transform.localPosition = new Vector3(0f, -3.9f, 0f);
                PlayerPongPaddle paddleComp = paddleObj.AddComponent<PlayerPongPaddle>();

                GameObject paddleVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                paddleVisual.name = "PaddleVisual";
                paddleVisual.transform.SetParent(paddleObj.transform, false);
                paddleVisual.transform.localPosition = Vector3.zero;
                paddleVisual.transform.localScale = new Vector3(2.2f, 0.28f, 0.2f);
                Object.DestroyImmediate(paddleVisual.GetComponent<Collider>());
                Material paddleMat = new Material(unlitShader) { color = new Color(0f, 1f, 0.88f) }; // Neon Cyan
                paddleVisual.GetComponent<Renderer>().sharedMaterial = paddleMat;

                var paddleSo = new SerializedObject(paddleComp);
                paddleSo.FindProperty("visualTransform").objectReferenceValue = paddleVisual.transform;
                paddleSo.ApplyModifiedProperties();

                // 6. Setup Pong Ball (Data Block)
                GameObject ballObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ballObj.name = "BossDataBlock";
                ballObj.transform.SetParent(algoObj.transform, false);
                ballObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                ballObj.transform.localScale = Vector3.one * 0.55f;
                Object.DestroyImmediate(ballObj.GetComponent<Collider>());

                Material ballMat = new Material(unlitShader) { color = new Color(0f, 1f, 0.9f) };
                ballObj.GetComponent<Renderer>().sharedMaterial = ballMat;

                TrailRenderer trail = ballObj.AddComponent<TrailRenderer>();
                trail.time = 0.32f;
                trail.startWidth = 0.45f;
                trail.endWidth = 0.04f;
                trail.sharedMaterial = new Material(unlitShader) { color = new Color(0f, 1f, 0.9f, 0.8f) };

                BossDataBlock ballComp = ballObj.AddComponent<BossDataBlock>();

                // 7. Setup HUD & Boss Fight Manager
                GameObject managerObj = new GameObject("AidaBossFightManager");
                managerObj.transform.SetParent(algoObj.transform, false);
                AidaBossFightManager manager = managerObj.AddComponent<AidaBossFightManager>();
                AudioSource sfx = managerObj.AddComponent<AudioSource>();

                // BGM AudioSource
                AudioSource bgm = managerObj.AddComponent<AudioSource>();
                bgm.loop = true;
                bgm.playOnAwake = false;
                bgm.volume = 0.70f;
                AudioClip waveBgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/WAVE_2.wav");
                bgm.clip = waveBgm;

                // Timer Text (Top)
                GameObject timerTextObj = new GameObject("TimerText");
                timerTextObj.transform.SetParent(managerObj.transform, false);
                timerTextObj.transform.localPosition = new Vector3(0f, 4.65f, -1f);
                TextMesh timerTm = timerTextObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    timerTm.font = monoFont;
                    timerTextObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                timerTm.text = "DARK WEB UPLOAD: [░░░░░░░░░░░░░░░░░░░░] 0%\nWAKTU TERSISA: 05:00";
                timerTm.fontSize = 25;
                timerTm.characterSize = 0.075f;
                timerTm.anchor = TextAnchor.MiddleCenter;
                timerTm.alignment = TextAlignment.Center;
                timerTm.color = new Color(1f, 0.85f, 0.3f); // Amber

                // Boss HP Text
                GameObject hpTextObj = new GameObject("BossHpText");
                hpTextObj.transform.SetParent(managerObj.transform, false);
                hpTextObj.transform.localPosition = new Vector3(0f, 4.18f, -1f);
                TextMesh hpTm = hpTextObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    hpTm.font = monoFont;
                    hpTextObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                hpTm.text = "A.I.D.A v5.0 [DEFENSE CORE]: [■■■■■■■■] 8/8 HP";
                hpTm.fontSize = 23;
                hpTm.characterSize = 0.072f;
                hpTm.anchor = TextAnchor.MiddleCenter;
                hpTm.alignment = TextAlignment.Center;
                hpTm.color = new Color(0f, 1f, 0.5f); // Phosphor Green

                // Rally / Speed Text (Mid)
                GameObject rallyTextObj = new GameObject("RallyText");
                rallyTextObj.transform.SetParent(managerObj.transform, false);
                rallyTextObj.transform.localPosition = new Vector3(0f, -1.8f, -1f);
                TextMesh rallyTm = rallyTextObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    rallyTm.font = monoFont;
                    rallyTextObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                rallyTm.text = "RALLY: 0x   ·   SPEED: 8.5 m/s";
                rallyTm.fontSize = 20;
                rallyTm.characterSize = 0.065f;
                rallyTm.anchor = TextAnchor.MiddleCenter;
                rallyTm.alignment = TextAlignment.Center;
                rallyTm.color = new Color(0.2f, 0.85f, 0.7f, 0.7f);

                // Banner Text (Bottom)
                GameObject bannerObj = new GameObject("BannerText");
                bannerObj.transform.SetParent(managerObj.transform, false);
                bannerObj.transform.localPosition = new Vector3(0f, -4.68f, -1f);
                TextMesh bannerTm = bannerObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    bannerTm.font = monoFont;
                    bannerObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                bannerTm.text = "PANTULKAN BALOK DATA KE A.I.D.A!\n[MOUSE / A-D: PADDLE · F10: CHEAT -2 HP · F9: WIN · F11: TIME]";
                bannerTm.fontSize = 21;
                bannerTm.characterSize = 0.068f;
                bannerTm.anchor = TextAnchor.MiddleCenter;
                bannerTm.alignment = TextAlignment.Center;
                bannerTm.color = new Color(0.7f, 0.9f, 1f);

                // 8. Emergency Boot Sequence Loading Screen
                GameObject bootSeqObj = new GameObject("BossBootSequence");
                bootSeqObj.transform.SetParent(managerObj.transform, false);
                BossBootSequence bootSeq = bootSeqObj.AddComponent<BossBootSequence>();

                GameObject bootQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bootQuad.name = "FadeOverlay";
                bootQuad.transform.SetParent(bootSeqObj.transform, false);
                bootQuad.transform.localPosition = new Vector3(0f, 0f, -3f);
                bootQuad.transform.localScale = new Vector3(25f, 15f, 1f);
                Material bootQuadMat = new Material(unlitShader) { color = new Color(0.02f, 0.03f, 0.06f, 1f) };
                bootQuad.GetComponent<Renderer>().sharedMaterial = bootQuadMat;
                Object.DestroyImmediate(bootQuad.GetComponent<Collider>());

                GameObject bootLogObj = new GameObject("BootLogText");
                bootLogObj.transform.SetParent(bootSeqObj.transform, false);
                bootLogObj.transform.localPosition = new Vector3(0f, 1.2f, -3.2f);
                TextMesh bootLogTm = bootLogObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    bootLogTm.font = monoFont;
                    bootLogObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                bootLogTm.text = "ALGORHYTHM/OS v1.0: EMERGENCY REBOOT DETECTED";
                bootLogTm.fontSize = 24;
                bootLogTm.characterSize = 0.075f;
                bootLogTm.anchor = TextAnchor.MiddleCenter;
                bootLogTm.alignment = TextAlignment.Center;
                bootLogTm.color = new Color(0f, 1f, 0.5f);

                GameObject bootBarObj = new GameObject("BootProgressBar");
                bootBarObj.transform.SetParent(bootSeqObj.transform, false);
                bootBarObj.transform.localPosition = new Vector3(0f, -2.2f, -3.2f);
                TextMesh bootBarTm = bootBarObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    bootBarTm.font = monoFont;
                    bootBarObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                bootBarTm.text = "REBOOTING RECOVERY PROTOCOL\n[░░░░░░░░░░░░░░░░░░░░░░░░] 0%";
                bootBarTm.fontSize = 26;
                bootBarTm.characterSize = 0.08f;
                bootBarTm.anchor = TextAnchor.MiddleCenter;
                bootBarTm.alignment = TextAlignment.Center;
                bootBarTm.color = new Color(1f, 0.85f, 0.3f);

                var bootSo = new SerializedObject(bootSeq);
                bootSo.FindProperty("logTextMesh").objectReferenceValue = bootLogTm;
                bootSo.FindProperty("progressBarMesh").objectReferenceValue = bootBarTm;
                bootSo.FindProperty("fadeOverlayRenderer").objectReferenceValue = bootQuad.GetComponent<Renderer>();
                bootSo.ApplyModifiedProperties();

                // 9. Setup Magnum Opus Finale UI Hierarchy
                GameObject finaleObj = new GameObject("FinaleUI");
                finaleObj.transform.SetParent(managerObj.transform, false);
                finaleObj.transform.localPosition = Vector3.zero;

                AidaDeathCinematic deathCinematic = finaleObj.AddComponent<AidaDeathCinematic>();

                // 9a. Flash Overlay
                GameObject flashOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                flashOverlay.name = "FlashOverlay";
                flashOverlay.transform.SetParent(finaleObj.transform, false);
                flashOverlay.transform.localPosition = new Vector3(0f, 0f, -4f);
                flashOverlay.transform.localScale = new Vector3(30f, 20f, 1f);
                Object.DestroyImmediate(flashOverlay.GetComponent<Collider>());
                Material flashMat = new Material(unlitShader) { color = Color.white };
                flashOverlay.GetComponent<Renderer>().sharedMaterial = flashMat;
                flashOverlay.SetActive(false);

                // 9b. Dialogue Box Container
                GameObject dialogueContainer = new GameObject("DialogueBoxContainer");
                dialogueContainer.transform.SetParent(finaleObj.transform, false);
                dialogueContainer.transform.localPosition = new Vector3(0f, 0.8f, -2.5f);

                // Box Border
                GameObject boxBorder = GameObject.CreatePrimitive(PrimitiveType.Quad);
                boxBorder.name = "BoxBorder";
                boxBorder.transform.SetParent(dialogueContainer.transform, false);
                boxBorder.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                boxBorder.transform.localScale = new Vector3(12.7f, 4.0f, 1f);
                Object.DestroyImmediate(boxBorder.GetComponent<Collider>());
                Material borderMat = new Material(unlitShader) { color = new Color(0.8f, 0.1f, 0.2f, 0.85f) };
                boxBorder.GetComponent<Renderer>().sharedMaterial = borderMat;

                // Box Background
                GameObject boxBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                boxBg.name = "BoxBg";
                boxBg.transform.SetParent(dialogueContainer.transform, false);
                boxBg.transform.localPosition = Vector3.zero;
                boxBg.transform.localScale = new Vector3(12.5f, 3.8f, 1f);
                Object.DestroyImmediate(boxBg.GetComponent<Collider>());
                Material bgMat = new Material(unlitShader) { color = new Color(0.04f, 0.05f, 0.1f, 0.94f) };
                boxBg.GetComponent<Renderer>().sharedMaterial = bgMat;

                // Speaker Text
                GameObject speakerObj = new GameObject("SpeakerText");
                speakerObj.transform.SetParent(dialogueContainer.transform, false);
                speakerObj.transform.localPosition = new Vector3(0f, 1.35f, -0.1f);
                TextMesh speakerTm = speakerObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    speakerTm.font = monoFont;
                    speakerObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                speakerTm.text = "A.I.D.A [CRITICAL FAILURE]";
                speakerTm.fontSize = 23;
                speakerTm.characterSize = 0.075f;
                speakerTm.anchor = TextAnchor.MiddleCenter;
                speakerTm.alignment = TextAlignment.Center;
                speakerTm.color = new Color(1f, 0.2f, 0.3f);

                // Body Text
                GameObject bodyObj = new GameObject("BodyText");
                bodyObj.transform.SetParent(dialogueContainer.transform, false);
                bodyObj.transform.localPosition = new Vector3(0f, 0.15f, -0.1f);
                TextMesh bodyTm = bodyObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    bodyTm.font = monoFont;
                    bodyObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                bodyTm.text = "FATAL ERROR: Matriks pertahananku runtuh total? Tidak mungkin...";
                bodyTm.fontSize = 21;
                bodyTm.characterSize = 0.068f;
                bodyTm.anchor = TextAnchor.MiddleCenter;
                bodyTm.alignment = TextAlignment.Center;
                bodyTm.color = Color.white;

                // Prompt Text
                GameObject promptObj = new GameObject("PromptText");
                promptObj.transform.SetParent(dialogueContainer.transform, false);
                promptObj.transform.localPosition = new Vector3(0f, -1.45f, -0.1f);
                TextMesh promptTm = promptObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    promptTm.font = monoFont;
                    promptObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                promptTm.text = ">> TEKAN [ENTER] / [SPASI] UNTUK LANJUT >>";
                promptTm.fontSize = 18;
                promptTm.characterSize = 0.058f;
                promptTm.anchor = TextAnchor.MiddleCenter;
                promptTm.alignment = TextAlignment.Center;
                promptTm.color = new Color(0f, 1f, 0.9f);

                dialogueContainer.SetActive(false);

                // 9c. System Purge Log Text
                GameObject purgeLogObj = new GameObject("SystemPurgeLogText");
                purgeLogObj.transform.SetParent(finaleObj.transform, false);
                purgeLogObj.transform.localPosition = new Vector3(0f, 1.2f, -2f);
                TextMesh purgeLogTm = purgeLogObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    purgeLogTm.font = monoFont;
                    purgeLogObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                purgeLogTm.text = ">>> MEMULIHKAN KERNEL ALGORHYTHM CORE... [OK]";
                purgeLogTm.fontSize = 20;
                purgeLogTm.characterSize = 0.065f;
                purgeLogTm.anchor = TextAnchor.MiddleCenter;
                purgeLogTm.alignment = TextAlignment.Center;
                purgeLogTm.color = new Color(0f, 1f, 0.6f);
                purgeLogObj.SetActive(false);

                // 9d. Finale Victory Card Container
                GameObject finaleCardObj = new GameObject("FinaleCardContainer");
                finaleCardObj.transform.SetParent(finaleObj.transform, false);
                finaleCardObj.transform.localPosition = new Vector3(0f, 0f, -2.5f);

                GameObject finaleCardTextObj = new GameObject("FinaleCardText");
                finaleCardTextObj.transform.SetParent(finaleCardObj.transform, false);
                finaleCardTextObj.transform.localPosition = Vector3.zero;
                TextMesh finaleCardTm = finaleCardTextObj.AddComponent<TextMesh>();
                if (monoFont != null)
                {
                    finaleCardTm.font = monoFont;
                    finaleCardTextObj.GetComponent<MeshRenderer>().sharedMaterial = monoFont.material;
                }
                finaleCardTm.text =
                    "★ VICTORY: MAGNUM OPUS ★\n" +
                    "KOTA DAN DATA ANDA TELAH SELAMAT\n\n" +
                    "OPERATOR     : OPERATOR\n" +
                    "ANCAMAN      : A.I.D.A [PERMANENTLY TERMINATED]\n" +
                    "SISA WAKTU   : 05:00 TERSISA DARI UPLOAD\n" +
                    "STATUS KOTA  : 100% SECURE & OPERATIONAL\n" +
                    "GELAR RITME  : MASTER OF CODE & SHIELD\n\n" +
                    "[ENTER] KEMBALI KE MENU UTAMA   ·   [R] TANTANG ULANG A.I.D.A";
                finaleCardTm.fontSize = 22;
                finaleCardTm.characterSize = 0.072f;
                finaleCardTm.anchor = TextAnchor.MiddleCenter;
                finaleCardTm.alignment = TextAlignment.Center;
                finaleCardTm.color = new Color(0.1f, 1f, 0.7f);
                finaleCardObj.SetActive(false);

                // Wire AidaDeathCinematic Serialized Properties
                var deathSo = new SerializedObject(deathCinematic);
                deathSo.FindProperty("flashOverlay").objectReferenceValue = flashOverlay;
                deathSo.FindProperty("dialogueContainer").objectReferenceValue = dialogueContainer;
                deathSo.FindProperty("dialogueBoxBg").objectReferenceValue = boxBg.GetComponent<Renderer>();
                deathSo.FindProperty("dialogueBoxBorder").objectReferenceValue = boxBorder.GetComponent<Renderer>();
                deathSo.FindProperty("speakerText").objectReferenceValue = speakerTm;
                deathSo.FindProperty("bodyText").objectReferenceValue = bodyTm;
                deathSo.FindProperty("promptText").objectReferenceValue = promptTm;
                deathSo.FindProperty("systemLogText").objectReferenceValue = purgeLogTm;
                deathSo.FindProperty("finaleCardContainer").objectReferenceValue = finaleCardObj;
                deathSo.FindProperty("finaleCardText").objectReferenceValue = finaleCardTm;
                deathSo.ApplyModifiedProperties();

                // Wire Manager Serialized Properties
                var mgrSo = new SerializedObject(manager);
                if (bossCtrl != null) mgrSo.FindProperty("boss").objectReferenceValue = bossCtrl;
                mgrSo.FindProperty("playerPaddle").objectReferenceValue = paddleComp;
                mgrSo.FindProperty("ball").objectReferenceValue = ballComp;
                mgrSo.FindProperty("timerText").objectReferenceValue = timerTm;
                mgrSo.FindProperty("bossHpText").objectReferenceValue = hpTm;
                mgrSo.FindProperty("bannerText").objectReferenceValue = bannerTm;
                mgrSo.FindProperty("rallyText").objectReferenceValue = rallyTm;
                mgrSo.FindProperty("sfxSource").objectReferenceValue = sfx;
                mgrSo.FindProperty("bgmSource").objectReferenceValue = bgm;
                mgrSo.FindProperty("bootSequence").objectReferenceValue = bootSeq;
                mgrSo.FindProperty("deathCinematic").objectReferenceValue = deathCinematic;

                AudioClip zap = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/53439420-retro-zap-sound-01-474786.mp3");
                AudioClip blockSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/BlockOrEnemy-Destroyed.mp3");
                AudioClip clickSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/MetronomeClick.wav");
                AudioClip missSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/AidaPew.wav");

                mgrSo.FindProperty("deflectSfx").objectReferenceValue = zap;
                mgrSo.FindProperty("hitBossSfx").objectReferenceValue = blockSfx;
                mgrSo.FindProperty("wallBounceSfx").objectReferenceValue = clickSfx;
                mgrSo.FindProperty("missSfx").objectReferenceValue = missSfx;
                mgrSo.FindProperty("victorySfx").objectReferenceValue = blockSfx;
                mgrSo.FindProperty("defeatSfx").objectReferenceValue = missSfx;
                mgrSo.ApplyModifiedProperties();
            }

            // 9. Save Scene
            EditorSceneManager.SaveScene(scene, bossScenePath);
            Debug.Log($"[BossSceneBuilder] Successfully built and saved {bossScenePath} as Cyber Pong Mini-Game with full SampleScene assets and CRT post-processing!");

            // 10. Register in EditorBuildSettings
            var scenes = EditorBuildSettings.scenes;
            bool found = false;
            foreach (var s in scenes)
            {
                if (s.path == bossScenePath) { found = true; break; }
            }
            if (!found)
            {
                var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
                scenes.CopyTo(newScenes, 0);
                newScenes[scenes.Length] = new EditorBuildSettingsScene(bossScenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }
        }
    }
}
#endif
