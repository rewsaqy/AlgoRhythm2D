using System;
using AlgoRhythm.Core.Combat;
using AlgoRhythm.Game.Config;
using AlgoRhythm.Game.Presentation;
using UnityEngine;

namespace AlgoRhythm.Game.Combat
{
    /// <summary>
    /// Manages between-wave shop progression, PTS economy upgrades,
    /// and upgrade track states (Regen and AIDA evolution).
    /// Decoupled from CombatController to eliminate god-object coupling.
    /// </summary>
    public sealed class ShopController : IDisposable
    {
        public const string AidaLevelPrefKey = "AlgoRhythm.AidaLevel";

        private readonly CombatState _combat;
        private readonly DemoConfig _config;
        private readonly ShopView _shop;
        private readonly HudView _hud;
        private readonly SfxPlayer _sfx;
        private readonly AidaTower _aida;
        private readonly WaveDirector _director;
        private readonly SongPlayer _song;

        private int _regenLevel;
        private int _aidaLevel = 1;
        private int _aidaHitEvery;
        private int _aidaFireEvery;
        private bool _inShop;

        public TutorialDriver Tutorial { get; set; }

        public int RegenLevel => _regenLevel;

        public int AidaLevel => _aidaLevel;

        public int AidaHitEvery => _aidaHitEvery;

        public int AidaFireEvery => _aidaFireEvery;

        public bool InShop => _inShop;

        public ShopController(
            CombatState combat,
            DemoConfig config,
            ShopView shop,
            HudView hud,
            SfxPlayer sfx,
            AidaTower aida,
            WaveDirector director,
            SongPlayer song)
        {
            _combat = combat;
            _config = config;
            _shop = shop;
            _hud = hud;
            _sfx = sfx;
            _aida = aida;
            _director = director;
            _song = song;

            _aidaHitEvery = _config != null ? _config.aidaHitEveryShots : 8;
            _aidaFireEvery = _config != null ? _config.aidaFireEveryBeats : 4;

            PlayerPrefs.SetInt(AidaLevelPrefKey, _aidaLevel);
            PlayerPrefs.Save();

            if (_shop != null)
            {
                _shop.Confirmed += OnShopConfirmed;
                _shop.Hide();
            }
        }

        public void Dispose()
        {
            if (_shop != null)
            {
                _shop.Confirmed -= OnShopConfirmed;
            }
        }

        /// <summary>
        /// Between-wave shop: game idles on the cleared wave until the player confirms LANJUT.
        /// </summary>
        public void OpenShop()
        {
            if (_shop == null)
            {
                _director?.Advance();
                return;
            }

            _inShop = true;
            _aida?.ClearShots();
            if (_song != null)
            {
                _song.StopSong();
            }

            RenderShop();
            _shop.Show();
        }

        public void CloseShop()
        {
            if (_inShop)
            {
                _inShop = false;
                if (_shop != null)
                {
                    _shop.Hide();
                }
            }
        }

        public void ApplyWaveClearRegen()
        {
            if (_regenLevel > 0)
            {
                int heal = RegenForLevel(_regenLevel);
                if (heal > 0)
                {
                    _combat?.Heal(heal);
                }
            }
        }

        private void OnShopConfirmed(int slot)
        {
            if (!_inShop)
            {
                return;
            }

            if (slot == ShopView.SlotContinue)
            {
                _inShop = false;
                _shop.Hide();
                if (Tutorial != null && Tutorial.TryConsumeShopContinue())
                {
                    return;
                }

                _director?.Advance();
                return;
            }

            BuyShopItem(slot);
        }

        public void BuyShopItem(int slot)
        {
            int maxLevel = _config != null ? Mathf.Max(1, _config.shopMaxLevel) : 5;
            int cost;
            bool maxed;
            if (slot == ShopView.SlotRegen)
            {
                cost = UpgradeCost(_regenLevel);
                maxed = _regenLevel >= maxLevel;
            }
            else if (slot == ShopView.SlotAida)
            {
                cost = UpgradeCost(_aidaLevel - 1);
                maxed = _aidaLevel >= maxLevel;
            }
            else
            {
                return;
            }

            if (maxed)
            {
                _hud?.SetStatus("Sudah MAX");
                RenderShop();
                return;
            }

            if (_combat == null || !_combat.TrySpendPts(cost))
            {
                _hud?.SetStatus("PTS tidak cukup");
                RenderShop();
                return;
            }

            if (slot == ShopView.SlotRegen)
            {
                _regenLevel = Mathf.Min(maxLevel, _regenLevel + 1);
                int instantHeal = RegenForLevel(_regenLevel);
                _combat?.Heal(instantHeal);
                _sfx?.PlayDestroy();
                _hud?.SetStatus($"REGEN LV {_regenLevel} DIBELI (+{instantHeal} HP & REGEN AKTIF)");
            }
            else
            {
                _aidaLevel = Mathf.Min(maxLevel, _aidaLevel + 1);
                PlayerPrefs.SetInt(AidaLevelPrefKey, _aidaLevel);
                PlayerPrefs.Save();
                _aidaHitEvery = AidaTable(_config?.shopAidaHitEvery, _aidaLevel, 8);
                _aidaFireEvery = AidaTable(_config?.shopAidaFireEvery, _aidaLevel, 4);
                _aida?.SetLevel(_aidaLevel, animate: true);
                _sfx?.PlayPew();
                _hud?.SetStatus($"A.I.D.A EVOLUSI: LV {_aidaLevel} AKTIF!");
            }

            RenderShop();
        }

        public int UpgradeCost(int currentLevel)
        {
            int[] costs = _config != null ? _config.shopUpgradeCosts : null;
            if (costs == null || costs.Length == 0)
            {
                return int.MaxValue;
            }

            return Mathf.Max(0, costs[Mathf.Clamp(currentLevel, 0, costs.Length - 1)]);
        }

        public int AidaTable(int[] table, int level, int fallback)
        {
            if (table == null || table.Length == 0)
            {
                return fallback;
            }

            return Mathf.Max(1, table[Mathf.Clamp(level - 1, 0, table.Length - 1)]);
        }

        public int RegenForLevel(int level)
        {
            int[] table = _config != null ? _config.shopRegenPerClear : null;
            if (table == null || table.Length == 0)
            {
                return 0;
            }

            return Mathf.Max(0, table[Mathf.Clamp(level, 0, table.Length - 1)]);
        }

        public void RenderShop()
        {
            if (_shop == null)
            {
                return;
            }

            int maxLevel = _config != null ? Mathf.Max(1, _config.shopMaxLevel) : 5;
            string regenRow = _regenLevel >= maxLevel
                ? $"REGEN Lv{maxLevel} (MAX)"
                : $"REGEN Lv{_regenLevel + 1} (+{RegenForLevel(_regenLevel + 1)}HP & clear): {UpgradeCost(_regenLevel)} PTS";
            int nextAida = Mathf.Min(maxLevel, _aidaLevel + 1);
            string aidaRow = _aidaLevel >= maxLevel
                ? $"AIDA Lv{maxLevel} (MAX)"
                : $"AIDA Lv{nextAida} {AidaTable(_config?.shopAidaHitEvery, nextAida, 8)}r/{AidaTable(_config?.shopAidaFireEvery, nextAida, 4)}b: {UpgradeCost(_aidaLevel - 1)} PTS";
            
            int currentPts = _combat != null ? _combat.CurrencyPts : 0;
            _shop.Render(currentPts, new[]
            {
                regenRow,
                aidaRow,
                "LANJUT →",
            });
        }
    }
}
