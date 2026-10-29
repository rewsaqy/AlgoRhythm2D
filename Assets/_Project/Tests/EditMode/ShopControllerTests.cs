using AlgoRhythm.Core.Combat;
using AlgoRhythm.Core.Rhythm;
using AlgoRhythm.Game.Combat;
using AlgoRhythm.Game.Config;
using AlgoRhythm.Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AlgoRhythm.Tests
{
    [TestFixture]
    public sealed class ShopControllerTests
    {
        private DemoConfig _config;
        private CombatState _combat;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DemoConfig>();
            _config.shopUpgradeCosts = new[] { 35, 65, 85, 95, 130 };
            _config.shopRegenPerClear = new[] { 0, 15, 20, 25, 35, 50 };
            _config.shopAidaHitEvery = new[] { 8, 8, 6, 5, 4 };
            _config.shopAidaFireEvery = new[] { 4, 4, 3, 3, 2 };
            _config.shopMaxLevel = 5;

            _combat = new CombatState(maxIntegrity: 100, startingEnergy: 6);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void InitialState_DefaultLevels()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);

            Assert.AreEqual(0, controller.RegenLevel);
            Assert.AreEqual(1, controller.AidaLevel);
            Assert.IsFalse(controller.InShop);
        }

        [Test]
        public void UpgradeCost_CalculatesAccordingToTier()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);

            Assert.AreEqual(35, controller.UpgradeCost(0));
            Assert.AreEqual(65, controller.UpgradeCost(1));
            Assert.AreEqual(85, controller.UpgradeCost(2));
            Assert.AreEqual(95, controller.UpgradeCost(3));
            Assert.AreEqual(130, controller.UpgradeCost(4));
            Assert.AreEqual(130, controller.UpgradeCost(10)); // Clamped to max tier
        }

        [Test]
        public void BuyShopItem_Regen_InsufficientPts_DoesNotUpgrade()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);
            // Default currency = 10, cost = 35
            Assert.AreEqual(10, _combat.CurrencyPts);

            controller.BuyShopItem(ShopView.SlotRegen);

            Assert.AreEqual(0, controller.RegenLevel);
            Assert.AreEqual(10, _combat.CurrencyPts);
        }

        [Test]
        public void BuyShopItem_Regen_SufficientPts_UpgradesAndDeducts()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);
            // Give PTS via perfect hits
            for (int i = 0; i < 45; i++)
            {
                _combat.ResolveThreat(new ThreatEvent(100 + i, ThreatKind.StraightShooter, Direction.North, 0, 1, 0), TimingGrade.Perfect, false, 0);
            }
            Assert.GreaterOrEqual(_combat.CurrencyPts, 35);
            int ptsBefore = _combat.CurrencyPts;

            controller.BuyShopItem(ShopView.SlotRegen);

            Assert.AreEqual(1, controller.RegenLevel);
            Assert.AreEqual(ptsBefore - 35, _combat.CurrencyPts);
        }

        [Test]
        public void BuyShopItem_Aida_UpgradesLevelAndStats()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);
            // Give plenty of PTS
            for (int i = 0; i < 50; i++)
            {
                _combat.ResolveThreat(new ThreatEvent(200 + i, ThreatKind.StraightShooter, Direction.North, 0, 1, 0), TimingGrade.Perfect, false, 0);
            }

            int ptsBefore = _combat.CurrencyPts;
            controller.BuyShopItem(ShopView.SlotAida);

            Assert.AreEqual(2, controller.AidaLevel);
            Assert.AreEqual(ptsBefore - 35, _combat.CurrencyPts);
            Assert.AreEqual(controller.AidaTable(_config.shopAidaHitEvery, 2, 8), controller.AidaHitEvery);
        }

        [Test]
        public void ApplyWaveClearRegen_HealsBasedOnCurrentLevel()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);
            // Damage player
            _combat.ResolveThreat(new ThreatEvent(1, ThreatKind.StraightShooter, Direction.North, 0, 1, 50), TimingGrade.Miss, false, 50);
            Assert.AreEqual(50, _combat.Integrity);

            // Level 0: no regen
            controller.ApplyWaveClearRegen();
            Assert.AreEqual(50, _combat.Integrity);

            // Give PTS and upgrade Regen to Lv1 (instant heal 15 + regen 15/clear)
            for (int i = 0; i < 20; i++)
            {
                _combat.ResolveThreat(new ThreatEvent(300 + i, ThreatKind.StraightShooter, Direction.North, 0, 1, 0), TimingGrade.Perfect, false, 0);
            }
            controller.BuyShopItem(ShopView.SlotRegen);
            Assert.AreEqual(1, controller.RegenLevel);
            // Instant heal on purchase: 50 + 15 = 65
            Assert.AreEqual(65, _combat.Integrity);

            // Wave clear regen: 65 + 15 = 80
            controller.ApplyWaveClearRegen();
            Assert.AreEqual(80, _combat.Integrity);
        }

        [Test]
        public void OpenAndCloseShop_UpdatesInShopState()
        {
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);
            Assert.IsFalse(controller.InShop);

            controller.OpenShop(); // null ShopView advances director or stays safe
            controller.CloseShop();
            Assert.IsFalse(controller.InShop);
        }

        [Test]
        public void BuyShopItem_Aida_PersistsLevelToPlayerPrefs()
        {
            PlayerPrefs.DeleteKey(ShopController.AidaLevelPrefKey);
            var controller = new ShopController(_combat, _config, null, null, null, null, null, null);
            Assert.AreEqual(1, PlayerPrefs.GetInt(ShopController.AidaLevelPrefKey, 1));

            // Give PTS and upgrade AIDA to Lv2
            for (int i = 0; i < 20; i++)
            {
                _combat.ResolveThreat(new ThreatEvent(400 + i, ThreatKind.StraightShooter, Direction.North, 0, 1, 0), TimingGrade.Perfect, false, 0);
            }
            controller.BuyShopItem(ShopView.SlotAida);

            Assert.AreEqual(2, controller.AidaLevel);
            Assert.AreEqual(2, PlayerPrefs.GetInt(ShopController.AidaLevelPrefKey, 1));
        }

        [Test]
        public void AidaBoss_ConfigureDifficulty_ScalesStatsCorrectly()
        {
            var go = new GameObject("TestAidaBoss");
            var boss = go.AddComponent<AlgoRhythm.Game.Boss.AidaBossController>();

            // Level 0 (no upgrade, ultra easy)
            boss.ConfigureDifficulty(0);
            Assert.AreEqual(3, boss.MaxHealth);
            Assert.AreEqual(3.5f, boss.BaseAiSpeed, 0.001f);
            Assert.AreEqual(5.0f, boss.EnragedAiSpeed, 0.001f);
            Assert.AreEqual(1.5f, boss.ShieldWidth, 0.001f);
            Assert.AreEqual(0.15f, boss.PredictionAccuracy, 0.001f);

            // Level 1 (base easy)
            boss.ConfigureDifficulty(1);
            Assert.AreEqual(4, boss.MaxHealth);
            Assert.AreEqual(4.2f, boss.BaseAiSpeed, 0.001f);
            Assert.AreEqual(6.2f, boss.EnragedAiSpeed, 0.001f);
            Assert.AreEqual(1.8f, boss.ShieldWidth, 0.001f);
            Assert.AreEqual(0.30f, boss.PredictionAccuracy, 0.001f);

            // Level 2 (medium)
            boss.ConfigureDifficulty(2);
            Assert.AreEqual(6, boss.MaxHealth);
            Assert.AreEqual(6.0f, boss.BaseAiSpeed, 0.001f);
            Assert.AreEqual(8.5f, boss.EnragedAiSpeed, 0.001f);
            Assert.AreEqual(2.2f, boss.ShieldWidth, 0.001f);
            Assert.AreEqual(0.50f, boss.PredictionAccuracy, 0.001f);

            // Level 5 (max overclock)
            boss.ConfigureDifficulty(5);
            Assert.AreEqual(15, boss.MaxHealth);
            Assert.AreEqual(12.5f, boss.BaseAiSpeed, 0.001f);
            Assert.AreEqual(18.0f, boss.EnragedAiSpeed, 0.001f);
            Assert.AreEqual(3.8f, boss.ShieldWidth, 0.001f);
            Assert.AreEqual(0.98f, boss.PredictionAccuracy, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void BossDataBlock_ConfigureDifficulty_SetsSpeedAndIncrement()
        {
            var go = new GameObject("TestBall");
            var ball = go.AddComponent<AlgoRhythm.Game.Boss.BossDataBlock>();

            ball.ConfigureDifficulty(7.0f, 1.03f);
            Assert.AreEqual(7.0f, ball.BaseSpeed, 0.001f);
            Assert.AreEqual(1.03f, ball.SpeedIncrementPerBounce, 0.001f);

            ball.ConfigureDifficulty(11.5f, 1.075f);
            Assert.AreEqual(11.5f, ball.BaseSpeed, 0.001f);
            Assert.AreEqual(1.075f, ball.SpeedIncrementPerBounce, 0.001f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ShopView_GracePeriod_SuppressesConfirmationImmediatelyAfterShow()
        {
            var go = new GameObject("TestShopView");
            var shopView = go.AddComponent<ShopView>();
            shopView.Initialize();

            bool confirmedFired = false;
            shopView.Confirmed += slot => confirmedFired = true;

            shopView.Show();
            Assert.IsTrue(shopView.IsOpen);
            Assert.IsTrue(shopView.IsInGracePeriod);

            // Attempting to confirm during grace period should do nothing
            shopView.ConfirmCurrentSelection();
            Assert.IsFalse(confirmedFired, "ConfirmCurrentSelection should be ignored while IsInGracePeriod is true.");

            Object.DestroyImmediate(go);
        }
    }
}
