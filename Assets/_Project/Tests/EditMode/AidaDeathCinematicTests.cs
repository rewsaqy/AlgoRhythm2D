using System;
using System.Reflection;
using AlgoRhythm.Game.Boss;
using NUnit.Framework;
using UnityEngine;

namespace AlgoRhythm.Tests
{
    public sealed class AidaDeathCinematicTests
    {
        [Test]
        public void AidaBoss_TakeDamage_InvokesDefeatedImmediately_WhenHealthZero()
        {
            var go = new GameObject("TestBoss");
            try
            {
                var boss = go.AddComponent<AidaBossController>();
                boss.ConfigureDifficulty(1);

                bool defeatedInvoked = false;
                boss.Defeated += () => defeatedInvoked = true;

                // Deal lethal damage
                boss.TakeDamage(boss.CurrentHealth);

                Assert.IsTrue(boss.IsDefeated);
                Assert.IsTrue(defeatedInvoked, "Defeated event should be invoked immediately upon lethal damage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AidaDeathCinematic_DyingLines_AdhereToAntiSlopRules()
        {
            // Retrieve DyingLines using reflection
            FieldInfo field = typeof(AidaDeathCinematic).GetField("DyingLines", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "DyingLines field should exist on AidaDeathCinematic.");

            Array lines = field.GetValue(null) as Array;
            Assert.IsNotNull(lines, "DyingLines should not be null.");
            Assert.GreaterOrEqual(lines.Length, 3, "There should be at least 3 dying monologue lines.");

            Type entryType = lines.GetValue(0).GetType();
            FieldInfo textProp = entryType.GetField("Text");
            FieldInfo speakerProp = entryType.GetField("Speaker");

            for (int i = 0; i < lines.Length; i++)
            {
                object entry = lines.GetValue(i);
                string text = textProp.GetValue(entry) as string;
                string speaker = speakerProp.GetValue(entry) as string;

                Assert.IsFalse(string.IsNullOrEmpty(text), $"Line {i} text must not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(speaker), $"Line {i} speaker must not be empty.");

                // Anti-slop rule: No em dash or double hyphen used as a dash
                Assert.IsFalse(text.Contains("—"), $"Line {i} contains em dash (anti-slop violation): {text}");
                Assert.IsFalse(text.Contains("--"), $"Line {i} contains double hyphen (anti-slop violation): {text}");

                // Speaker identity
                Assert.IsTrue(speaker.Contains("A.I.D.A"), $"Speaker must identify A.I.D.A: {speaker}");
            }
        }

        [Test]
        public void AidaDeathCinematic_Component_CanBeAttachedAndInitialized()
        {
            var go = new GameObject("TestCinematicObj");
            try
            {
                var cinematic = go.AddComponent<AidaDeathCinematic>();
                Assert.IsNotNull(cinematic);
                Assert.IsFalse(cinematic.IsPlaying);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AidaDeathCinematic_EnsureHierarchyReferences_DiscoversChildObjects()
        {
            var root = new GameObject("FinaleUI");
            try
            {
                var flash = new GameObject("FlashOverlay");
                flash.transform.SetParent(root.transform, false);

                var dialogue = new GameObject("DialogueBoxContainer");
                dialogue.transform.SetParent(root.transform, false);

                var bg = new GameObject("BoxBg");
                bg.transform.SetParent(dialogue.transform, false);
                bg.AddComponent<MeshRenderer>();

                var border = new GameObject("BoxBorder");
                border.transform.SetParent(dialogue.transform, false);
                border.AddComponent<MeshRenderer>();

                var speaker = new GameObject("SpeakerText");
                speaker.transform.SetParent(dialogue.transform, false);
                speaker.AddComponent<TextMesh>();

                var body = new GameObject("BodyText");
                body.transform.SetParent(dialogue.transform, false);
                body.AddComponent<TextMesh>();

                var prompt = new GameObject("PromptText");
                prompt.transform.SetParent(dialogue.transform, false);
                prompt.AddComponent<TextMesh>();

                var log = new GameObject("SystemPurgeLogText");
                log.transform.SetParent(root.transform, false);
                log.AddComponent<TextMesh>();

                var cardCont = new GameObject("FinaleCardContainer");
                cardCont.transform.SetParent(root.transform, false);

                var cardText = new GameObject("FinaleCardText");
                cardText.transform.SetParent(cardCont.transform, false);
                cardText.AddComponent<TextMesh>();

                var cinematic = root.AddComponent<AidaDeathCinematic>();
                cinematic.EnsureHierarchyReferences();

                Assert.AreSame(flash, cinematic.FlashOverlay);
                Assert.AreSame(dialogue, cinematic.DialogueContainer);
                Assert.IsNotNull(cinematic.DialogueBoxBg);
                Assert.IsNotNull(cinematic.DialogueBoxBorder);
                Assert.IsNotNull(cinematic.SpeakerText);
                Assert.IsNotNull(cinematic.BodyText);
                Assert.IsNotNull(cinematic.PromptText);
                Assert.IsNotNull(cinematic.SystemLogText);
                Assert.AreSame(cardCont, cinematic.FinaleCardContainer);
                Assert.IsNotNull(cinematic.FinaleCardText);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AutoTriggerEndingFlag_KeyConsistency()
        {
            PlayerPrefs.DeleteKey("AlgoRhythm.AutoTriggerEnding");
            Assert.AreEqual(0, PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerEnding", 0));

            PlayerPrefs.SetInt("AlgoRhythm.AutoTriggerEnding", 1);
            Assert.AreEqual(1, PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerEnding", 0));

            PlayerPrefs.DeleteKey("AlgoRhythm.AutoTriggerEnding");
            Assert.AreEqual(0, PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerEnding", 0));
        }

        [Test]
        public void AidaDeathCinematic_VictoryCard_IsCleanWithoutBoxBorders()
        {
            string card = AidaDeathCinematic.FormatVictoryCardText("TEST_OPERATOR", 5, 235f);

            // Assert no box border characters
            Assert.IsFalse(card.Contains("╔"), "Victory card should not contain ╔ box border.");
            Assert.IsFalse(card.Contains("═"), "Victory card should not contain ═ box border.");
            Assert.IsFalse(card.Contains("║"), "Victory card should not contain ║ box border.");
            Assert.IsFalse(card.Contains("╠"), "Victory card should not contain ╠ box border.");
            Assert.IsFalse(card.Contains("╚"), "Victory card should not contain ╚ box border.");

            // Assert antislop compliance
            Assert.IsFalse(card.Contains("—"), "Victory card should not contain em dash.");
            Assert.IsFalse(card.Contains("--"), "Victory card should not contain double hyphen.");

            // Assert required information presence
            Assert.IsTrue(card.Contains("★ VICTORY: MAGNUM OPUS ★"));
            Assert.IsTrue(card.Contains("KOTA DAN DATA ANDA TELAH SELAMAT"));
            Assert.IsTrue(card.Contains("TEST_OPERATOR"));
            Assert.IsTrue(card.Contains("A.I.D.A v5.0 [PERMANENTLY TERMINATED]"));
            Assert.IsTrue(card.Contains("03:55 TERSISA DARI UPLOAD"));
            Assert.IsTrue(card.Contains("[ENTER] KEMBALI KE MENU UTAMA"));
            Assert.IsTrue(card.Contains("[R] TANTANG ULANG A.I.D.A"));
        }

        [Test]
        public void AidaDeathCinematic_Flash_ActivatesOverlayObject()
        {
            var root = new GameObject("FinaleUI");
            try
            {
                var flash = new GameObject("FlashOverlay");
                flash.transform.SetParent(root.transform, false);
                flash.AddComponent<MeshRenderer>();
                flash.SetActive(false);

                var cinematic = root.AddComponent<AidaDeathCinematic>();
                cinematic.EnsureHierarchyReferences();

                Assert.IsFalse(flash.activeSelf, "FlashOverlay should initially be inactive.");

                cinematic.Flash(0.2f, 1.0f);

                Assert.IsTrue(flash.activeSelf, "FlashOverlay must be activated when Flash() is called.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AidaDeathCinematic_DefeatTauntLines_AdhereToAntiSlopRules()
        {
            FieldInfo field = typeof(AidaDeathCinematic).GetField("DefeatTauntLines", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "DefeatTauntLines field should exist on AidaDeathCinematic.");

            Array lines = field.GetValue(null) as Array;
            Assert.IsNotNull(lines, "DefeatTauntLines should not be null.");
            Assert.GreaterOrEqual(lines.Length, 3, "There should be at least 3 defeat taunt lines.");

            Type entryType = lines.GetValue(0).GetType();
            FieldInfo textProp = entryType.GetField("Text");
            FieldInfo speakerProp = entryType.GetField("Speaker");

            for (int i = 0; i < lines.Length; i++)
            {
                object entry = lines.GetValue(i);
                string text = textProp.GetValue(entry) as string;
                string speaker = speakerProp.GetValue(entry) as string;

                Assert.IsFalse(string.IsNullOrEmpty(text), $"Line {i} text must not be empty.");
                Assert.IsFalse(string.IsNullOrEmpty(speaker), $"Line {i} speaker must not be empty.");

                // Anti-slop rule: No em dash or double hyphen used as a dash
                Assert.IsFalse(text.Contains("—"), $"Defeat line {i} contains em dash (anti-slop violation): {text}");
                Assert.IsFalse(text.Contains("--"), $"Defeat line {i} contains double hyphen (anti-slop violation): {text}");

                // Speaker identity
                Assert.IsTrue(speaker.Contains("A.I.D.A"), $"Speaker must identify A.I.D.A: {speaker}");
            }
        }

        [Test]
        public void AidaDeathCinematic_DefeatCard_IsCleanWithoutBoxBorders()
        {
            string card = AidaDeathCinematic.FormatDefeatCardText("TEST_OPERATOR", 5);

            // Assert no box border characters
            Assert.IsFalse(card.Contains("╔"), "Defeat card should not contain ╔ box border.");
            Assert.IsFalse(card.Contains("═"), "Defeat card should not contain ═ box border.");
            Assert.IsFalse(card.Contains("║"), "Defeat card should not contain ║ box border.");
            Assert.IsFalse(card.Contains("╠"), "Defeat card should not contain ╠ box border.");
            Assert.IsFalse(card.Contains("╚"), "Defeat card should not contain ╚ box border.");

            // Assert antislop compliance
            Assert.IsFalse(card.Contains("—"), "Defeat card should not contain em dash.");
            Assert.IsFalse(card.Contains("--"), "Defeat card should not contain double hyphen.");

            // Assert required information and taunting CTA presence
            Assert.IsTrue(card.Contains("✕ MISSION FAILED: DATA BREACH ✕"));
            Assert.IsTrue(card.Contains("A.I.D.A TELAH MENGUASAI KERNEL KOTA"));
            Assert.IsTrue(card.Contains("TEST_OPERATOR"));
            Assert.IsTrue(card.Contains("A.I.D.A v5.0 [JARINGAN KOTA TERINFEKSI]"));
            Assert.IsTrue(card.Contains("[R] TANTANG ULANG A.I.D.A (REMATCH)"));
            Assert.IsTrue(card.Contains("[ENTER] MENYERAH KE MENU UTAMA"));
        }

        [Test]
        public void AidaDeathCinematic_AutoTriggerDefeat_PlayerPrefsRoundtrip()
        {
            PlayerPrefs.DeleteKey("AlgoRhythm.AutoTriggerDefeat");
            Assert.AreEqual(0, PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerDefeat", 0));

            PlayerPrefs.SetInt("AlgoRhythm.AutoTriggerDefeat", 1);
            Assert.AreEqual(1, PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerDefeat", 0));

            PlayerPrefs.DeleteKey("AlgoRhythm.AutoTriggerDefeat");
            Assert.AreEqual(0, PlayerPrefs.GetInt("AlgoRhythm.AutoTriggerDefeat", 0));
        }
    }
}
