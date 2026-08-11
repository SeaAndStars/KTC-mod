using System;
using System.Collections.Generic;
using KingdomEnhanced.Core;
using KingdomEnhanced.Features;

namespace KingdomEnhanced.UI
{
    /// <summary>
    /// Builds the metadata for all ModMenu feature items.
    /// </summary>
    public static class ModMenuFeatures
    {
        /// <summary>
        /// Builds feature metadata for a boolean toggle.
        /// </summary>
        /// <param name="id">Stable feature identifier.</param>
        /// <param name="labelKey">Feature title resource key.</param>
        /// <param name="cat">Tab category the feature belongs to.</param>
        /// <param name="sectionKey">Feature section resource key.</param>
        /// <param name="descriptionKey">Feature description resource key.</param>
        /// <param name="get">Delegate reading the current value.</param>
        /// <param name="set">Delegate writing the current value.</param>
        /// <param name="isLocked">Delegate determining whether the feature is locked.</param>
        /// <param name="lockReasonKey">Delegate returning the lock reason resource key.</param>
        /// <param name="hasConflict">Delegate determining whether a conflict warning should be shown.</param>
        /// <returns>Feature metadata ready for menu rendering.</returns>
        public static FeatureMeta Toggle(string id, string labelKey, TabCategory cat, string sectionKey, string descriptionKey,
            Func<bool> get, Action<bool> set, Func<bool> isLocked = null,
            Func<string> lockReasonKey = null, Func<bool> hasConflict = null)
        {
            return new FeatureMeta
            {
                Id = id,
                LabelKey = labelKey,
                SectionKey = sectionKey,
                Category = cat,
                DescriptionKey = descriptionKey,
                GetValue = get,
                SetValue = set,
                OnAction = null,
                IsLocked = isLocked,
                GetLockReasonKey = lockReasonKey,
                HasConflict = hasConflict
            };
        }

        /// <summary>
        /// Builds feature metadata for a button action.
        /// </summary>
        /// <param name="id">Stable feature identifier.</param>
        /// <param name="labelKey">Feature title resource key.</param>
        /// <param name="cat">Tab category the feature belongs to.</param>
        /// <param name="sectionKey">Feature section resource key.</param>
        /// <param name="descriptionKey">Feature description resource key.</param>
        /// <param name="act">Action executed when the button is clicked.</param>
        /// <param name="isLocked">Delegate determining whether the feature is locked.</param>
        /// <param name="lockReasonKey">Delegate returning the lock reason resource key.</param>
        /// <returns>Feature metadata ready for menu rendering.</returns>
        public static FeatureMeta Button(string id, string labelKey, TabCategory cat, string sectionKey, string descriptionKey,
            Action act, Func<bool> isLocked = null, Func<string> lockReasonKey = null)
        {
            return new FeatureMeta
            {
                Id = id,
                LabelKey = labelKey,
                SectionKey = sectionKey,
                Category = cat,
                DescriptionKey = descriptionKey,
                GetValue = null,
                SetValue = null,
                OnAction = act,
                IsLocked = isLocked,
                GetLockReasonKey = lockReasonKey,
                HasConflict = null
            };
        }

        /// <summary>
        /// Builds feature metadata for a slider.
        /// </summary>
        /// <param name="id">Stable feature identifier.</param>
        /// <param name="labelKey">Feature title resource key.</param>
        /// <param name="cat">Tab category the feature belongs to.</param>
        /// <param name="sectionKey">Feature section resource key.</param>
        /// <param name="descriptionKey">Feature description resource key.</param>
        /// <param name="get">Delegate reading the current value.</param>
        /// <param name="set">Delegate writing the current value.</param>
        /// <param name="min">Slider minimum value.</param>
        /// <param name="max">Slider maximum value.</param>
        /// <returns>Feature metadata ready for menu rendering.</returns>
        public static FeatureMeta Slider(string id, string labelKey, TabCategory cat, string sectionKey, string descriptionKey,
            Func<float> get, Action<float> set, float min, float max)
        {
            return new FeatureMeta
            {
                Id = id,
                LabelKey = labelKey,
                SectionKey = sectionKey,
                Category = cat,
                DescriptionKey = descriptionKey,
                GetFloatValue = get,
                SetFloatValue = set,
                MinVal = min,
                MaxVal = max,
                IsLocked = null,
                GetLockReasonKey = null,
                HasConflict = null
            };
        }

        /// <summary>
        /// Builds the complete feature metadata collection.
        /// </summary>
        /// <returns>Feature metadata array in the existing display order.</returns>
        public static FeatureMeta[] Build()
        {
            var list = new List<FeatureMeta>();

            // ==================== MAIN ====================
            list.Add(Toggle("show_stamina", "feature.show_stamina.label", TabCategory.Main, "feature.section.hud",
                "feature.show_stamina.description",
                () => ModMenu.ShowStaminaBar, v => ModMenu.ShowStaminaBar = v));
            list.Add(Button("stamina_style", "feature.stamina_style.label", TabCategory.Main, "feature.section.hud",
                "feature.stamina_style.description",
                () => ModMenu.CycleStaminaBarStyle(),
                () => !ModMenu.ShowStaminaBar, () => "feature.lock.requires_energy_bar"));
            list.Add(Button("stamina_pos", "feature.stamina_pos.label", TabCategory.Main, "feature.section.hud",
                "feature.stamina_pos.description",
                () => ModMenu.CycleStaminaBarPosition(),
                () => !ModMenu.ShowStaminaBar, () => "feature.lock.requires_energy_bar"));
            list.Add(Toggle("display_times", "feature.display_times.label", TabCategory.Main, "feature.section.hud",
                "feature.display_times.description",
                () => ModMenu.DisplayTimes,
                v =>
                {
                    // Keep consistent with the F4 hotkey: persist the config immediately so the state does not snap back after restart
                    ModMenu.DisplayTimes = v;
                    Settings.DisplayTimes.Value = v;
                }));
            list.Add(Toggle("use_12h_clock", "feature.use_12h_clock.label", TabCategory.Main, "feature.section.hud",
                "feature.use_12h_clock.description",
                () => ModMenu.Use12HourClock, v => ModMenu.Use12HourClock = v,
                () => !ModMenu.DisplayTimes, () => "feature.lock.requires_hud"));
            list.Add(Button("monitor_style", "feature.monitor_style.label", TabCategory.Main, "feature.section.hud",
                "feature.monitor_style.description",
                () => KingdomMonitor.Instance?.NextStyle(),
                () => KingdomMonitor.Instance == null || !KingdomMonitor.Instance.IsVisible,
                () => "feature.lock.requires_monitor"));

            list.Add(Toggle("enable_accessibility", "feature.enable_accessibility.label", TabCategory.Main, "feature.section.accessibility",
                "feature.enable_accessibility.description",
                () => ModMenu.EnableAccessibility, v => ModMenu.EnableAccessibility = v));
            list.Add(Toggle("enable_tts", "feature.enable_tts.label", TabCategory.Main, "feature.section.accessibility",
                "feature.enable_tts.description",
                () => ModMenu.EnableTTS, v => ModMenu.EnableTTS = v));
            list.Add(Toggle("simplify_names", "feature.simplify_names.label", TabCategory.Main, "feature.section.accessibility",
                "feature.simplify_names.description",
                () => ModMenu.SimplifyNames, v => ModMenu.SimplifyNames = v));
            list.Add(Toggle("castle_announcer", "feature.castle_announcer.label", TabCategory.Main, "feature.section.accessibility",
                "feature.castle_announcer.description",
                () => ModMenu.EnableCastleAnnouncer, v => ModMenu.EnableCastleAnnouncer = v));

            list.Add(Slider("speed_mult", "feature.speed_mult.label", TabCategory.Main, "feature.section.movement",
                "feature.speed_mult.description",
                () => ModMenu.SpeedMultiplier, v => ModMenu.SpeedMultiplier = v, 0.5f, 10.0f));

            list.Add(Toggle("size_hack", "feature.size_hack.label", TabCategory.Main, "feature.section.player",
                "feature.size_hack.description",
                () => ModMenu.EnableSizeHack, v => ModMenu.EnableSizeHack = v));
            list.Add(Slider("target_size", "feature.target_size.label", TabCategory.Main, "feature.section.player",
                "feature.target_size.description",
                () => ModMenu.TargetSize, v => ModMenu.TargetSize = v, 0.2f, 3.0f));

            // ==================== CHEATS ====================
            list.Add(Toggle("infinite_stamina", "feature.infinite_stamina.label", TabCategory.Cheats, "feature.section.invincibility",
                "feature.infinite_stamina.description",
                () => ModMenu.InfiniteStamina, v => ModMenu.InfiniteStamina = v,
                () => !ModMenu.CheatsUnlocked));

            list.Add(Toggle("no_tool_cooldowns", "feature.no_tool_cooldowns.label", TabCategory.Cheats, "feature.section.infinite_stone",
                "feature.no_tool_cooldowns.description",
                () => ModMenu.NoToolCooldowns, v => ModMenu.NoToolCooldowns = v,
                () => !ModMenu.CheatsUnlocked));
            list.Add(Slider("artemis_arrows", "feature.artemis_arrows.label", TabCategory.Cheats, "feature.section.infinite_stone",
                "feature.artemis_arrows.description",
                () => ModMenu.ArtemisArrowCount, v => ModMenu.ArtemisArrowCount = v, 1f, 50f));
            list.Add(Slider("artemis_range", "feature.artemis_range.label", TabCategory.Cheats, "feature.section.infinite_stone",
                "feature.artemis_range.description",
                () => ModMenu.ArtemisRangeMult, v => ModMenu.ArtemisRangeMult = v, 0.5f, 5.0f));
            list.Add(Slider("artemis_damage", "feature.artemis_damage.label", TabCategory.Cheats, "feature.section.infinite_stone",
                "feature.artemis_damage.description",
                () => ModMenu.ArtemisArrowDamageMult, v => ModMenu.ArtemisArrowDamageMult = v, 0.5f, 5.0f));

            list.Add(Button("add_10_coins", "feature.add_10_coins.label", TabCategory.Cheats, "feature.section.economy",
                "feature.add_10_coins.description",
                () => ModMenu.GiveCurrency(10, false),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("add_50_coins", "feature.add_50_coins.label", TabCategory.Cheats, "feature.section.economy",
                "feature.add_50_coins.description",
                () => ModMenu.GiveCurrency(50, false),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("add_5_gems", "feature.add_5_gems.label", TabCategory.Cheats, "feature.section.economy",
                "feature.add_5_gems.description",
                () => ModMenu.GiveCurrency(5, true),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("fill_wallet", "feature.fill_wallet.label", TabCategory.Cheats, "feature.section.economy",
                "feature.fill_wallet.description",
                () => ModMenu.FillWallet(),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Slider("coin_income", "feature.coin_income.label", TabCategory.Cheats, "feature.section.economy",
                "feature.coin_income.description",
                () => ModMenu.CoinIncomeMult, v => ModMenu.CoinIncomeMult = v, 0.5f, 4.0f));
            list.Add(Slider("bag_drop", "feature.bag_drop.label", TabCategory.Cheats, "feature.section.economy",
                "feature.bag_drop.description",
                () => ModMenu.BagDropMult, v => ModMenu.BagDropMult = v, 0.5f, 4.0f));

            list.Add(Button("recruit_beggars", "feature.recruit_beggars.label", TabCategory.Cheats, "feature.section.military",
                "feature.recruit_beggars.description",
                () => ArmyManager.RecruitBeggars(),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("drop_archer", "feature.drop_archer.label", TabCategory.Cheats, "feature.section.military",
                "feature.drop_archer.description",
                () => ArmyManager.DropTools("Archer"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("drop_builder", "feature.drop_builder.label", TabCategory.Cheats, "feature.section.military",
                "feature.drop_builder.description",
                () => ArmyManager.DropTools("Builder"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));

            list.Add(Button("kill_enemies", "feature.kill_enemies.label", TabCategory.Cheats, "feature.section.military",
                "feature.kill_enemies.description",
                () => ArmyManager.KillAllEnemies(),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("destroy_portals", "feature.destroy_portals.label", TabCategory.Cheats, "feature.section.military",
                "feature.destroy_portals.description",
                () => ArmyManager.DestroyAllPortals(),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("max_army", "feature.max_army.label", TabCategory.Cheats, "feature.section.military",
                "feature.max_army.description",
                () => ArmyManager.SpawnMaxArmy(),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));

            list.Add(Slider("spawn_unit_count", "feature.spawn_unit_count.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_unit_count.description",
                () => ModMenu.SpawnUnitCount, v => ModMenu.SpawnUnitCount = (int)v, 1f, 50f));
            list.Add(Button("spawn_u_vagrant", "feature.spawn_u_vagrant.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_vagrant.description",
                () => ArmyManager.SpawnUnit("Beggar", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_u_villager", "feature.spawn_u_villager.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_villager.description",
                () => ArmyManager.SpawnUnit("Peasant", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_u_archer", "feature.spawn_u_archer.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_archer.description",
                () => ArmyManager.SpawnUnit("Archer", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_u_builder", "feature.spawn_u_builder.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_builder.description",
                () => ArmyManager.SpawnUnit("Worker", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_u_farmer", "feature.spawn_u_farmer.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_farmer.description",
                () => ArmyManager.SpawnUnit("Farmer", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_u_pikeman", "feature.spawn_u_pikeman.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_pikeman.description",
                () => ArmyManager.SpawnUnit("Pikeman", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_u_ninja", "feature.spawn_u_ninja.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_ninja.description",
                () => ArmyManager.SpawnUnit("Ninja", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked || BiomeHolder.Inst == null || BiomeHolder.Inst.BiomeIndex != (int)BiomeHolder.Biomes.Shogun,
                () => !ModMenu.CheatsUnlocked ? "feature.lock.locked" : "feature.lock.shogun_only"));
            list.Add(Button("spawn_u_berserker", "feature.spawn_u_berserker.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_berserker.description",
                () => ArmyManager.SpawnUnit("Berserker", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked || BiomeHolder.Inst == null || BiomeHolder.Inst.BiomeIndex != (int)BiomeHolder.Biomes.Norselands,
                () => !ModMenu.CheatsUnlocked ? "feature.lock.locked" : "feature.lock.norse_only"));
            list.Add(Button("spawn_u_knight", "feature.spawn_u_knight.label", TabCategory.Cheats, "feature.section.unit_spawner",
                "feature.spawn_u_knight.description",
                () => ArmyManager.SpawnUnit("Knight", (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));

            list.Add(Button("spawn_h_bakery", "feature.spawn_h_bakery.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_bakery.description",
                () => ArmyManager.SpawnHermit("Bakery"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_h_ballista", "feature.spawn_h_ballista.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_ballista.description",
                () => ArmyManager.SpawnHermit("Ballista"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_h_berserker", "feature.spawn_h_berserker.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_berserker.description",
                () => ArmyManager.SpawnHermit("Berserker"),
                () => !ModMenu.CheatsUnlocked || BiomeHolder.Inst == null || BiomeHolder.Inst.BiomeIndex != (int)BiomeHolder.Biomes.Norselands,
                () => !ModMenu.CheatsUnlocked ? "feature.lock.locked" : "feature.lock.norse_only"));
            list.Add(Button("spawn_h_fire", "feature.spawn_h_fire.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_fire.description",
                () => ArmyManager.SpawnHermit("Fire"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_h_horn", "feature.spawn_h_horn.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_horn.description",
                () => ArmyManager.SpawnHermit("Horn"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_h_stable", "feature.spawn_h_stable.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_stable.description",
                () => ArmyManager.SpawnHermit("Stable"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_h_warrior", "feature.spawn_h_warrior.label", TabCategory.Cheats, "feature.section.hermit_spawner",
                "feature.spawn_h_warrior.description",
                () => ArmyManager.SpawnHermit("Warrior"),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));

            list.Add(Button("spawn_e_weak", "feature.spawn_e_weak.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_weak.description",
                () => ArmyManager.SpawnEnemy(EnemyType.TrollWeak, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_e_squid", "feature.spawn_e_squid.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_squid.description",
                () => ArmyManager.SpawnEnemy(EnemyType.Squid, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_e_stealer", "feature.spawn_e_stealer.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_stealer.description",
                () => ArmyManager.SpawnEnemy(EnemyType.Stealer, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_e_boss", "feature.spawn_e_boss.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_boss.description",
                () => ArmyManager.SpawnEnemy(EnemyType.Boss, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_e_crusher", "feature.spawn_e_crusher.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_crusher.description",
                () => ArmyManager.SpawnEnemy(EnemyType.Crusher, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_e_knight", "feature.spawn_e_knight.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_knight.description",
                () => ArmyManager.SpawnEnemy(EnemyType.Knight, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));
            list.Add(Button("spawn_e_archer", "feature.spawn_e_archer.label", TabCategory.Cheats, "feature.section.enemy_spawner",
                "feature.spawn_e_archer.description",
                () => ArmyManager.SpawnEnemy(EnemyType.Archer, (int)ModMenu.SpawnUnitCount),
                () => !ModMenu.CheatsUnlocked, () => "feature.lock.locked"));

            list.Add(Button("clear_coins", "feature.clear_coins.label", TabCategory.Cheats, "feature.section.world",
                "feature.clear_coins.description",
                () => ArmyManager.ClearCoins()));

            list.Add(Toggle("hyper_builders", "feature.hyper_builders.label", TabCategory.Cheats, "feature.section.builders",
                "feature.hyper_builders.description",
                () => ModMenu.HyperBuilders, v => ModMenu.HyperBuilders = v,
                () => !ModMenu.CheatsUnlocked, null,
                () => ModMenu.HyperBuilders && ModMenu.LargerCamps));
            list.Add(Slider("builder_speed", "feature.builder_speed.label", TabCategory.Cheats, "feature.section.builders",
                "feature.builder_speed.description",
                () => ModMenu.BuilderSpeedMult, v => ModMenu.BuilderSpeedMult = v, 0.01f, 10.0f));
            list.Add(Slider("builder_work", "feature.builder_work.label", TabCategory.Cheats, "feature.section.builders",
                "feature.builder_work.description",
                () => ModMenu.BuilderEfficiencyMult, v => ModMenu.BuilderEfficiencyMult = v, 0.0001f, 3.0f));
            list.Add(Toggle("larger_camps", "feature.larger_camps.label", TabCategory.Cheats, "feature.section.builders",
                "feature.larger_camps.description",
                () => ModMenu.LargerCamps, v => ModMenu.LargerCamps = v,
                () => !ModMenu.CheatsUnlocked, null,
                () => ModMenu.HyperBuilders && ModMenu.LargerCamps));

            // ==================== LAB ====================
            list.Add(Toggle("lock_summer", "feature.lock_summer.label", TabCategory.Lab, "feature.section.world",
                "feature.lock_summer.description",
                () => ModMenu.LockSummer, v => ModMenu.LockSummer = v));
            list.Add(Toggle("clear_weather", "feature.clear_weather.label", TabCategory.Lab, "feature.section.world",
                "feature.clear_weather.description",
                () => ModMenu.ClearWeather, v => ModMenu.ClearWeather = v,
                null, null, () => ModMenu.LockSummer && ModMenu.ClearWeather));
            list.Add(Toggle("coins_stay_dry", "feature.coins_stay_dry.label", TabCategory.Lab, "feature.section.world",
                "feature.coins_stay_dry.description",
                () => ModMenu.CoinsStayDry, v => ModMenu.CoinsStayDry = v));
            list.Add(Toggle("no_blood_moons", "feature.no_blood_moons.label", TabCategory.Lab, "feature.section.world",
                "feature.no_blood_moons.description",
                () => ModMenu.NoBloodMoons, v => ModMenu.NoBloodMoons = v));

            list.Add(Toggle("invincible_walls", "feature.invincible_walls.label", TabCategory.Lab, "feature.section.structures",
                "feature.invincible_walls.description",
                () => ModMenu.InvincibleWalls, v => ModMenu.InvincibleWalls = v));
            list.Add(Toggle("better_citizen_houses", "feature.better_citizen_houses.label", TabCategory.Lab, "feature.section.structures",
                "feature.better_citizen_houses.description",
                () => ModMenu.BetterCitizenHouses, v => ModMenu.BetterCitizenHouses = v));
            list.Add(Toggle("better_knight", "feature.better_knight.label", TabCategory.Lab, "feature.section.combat",
                "feature.better_knight.description",
                () => ModMenu.BetterKnight, v => ModMenu.BetterKnight = v));

            list.Add(Toggle("archer_fire_boost", "feature.archer_fire_boost.label", TabCategory.Lab, "feature.section.units",
                "feature.archer_fire_boost.description",
                () => ModMenu.ArcherFireBoost, v => ModMenu.ArcherFireBoost = v));
            list.Add(Toggle("berserker_rage", "feature.berserker_rage.label", TabCategory.Lab, "feature.section.units",
                "feature.berserker_rage.description",
                () => ModMenu.BerserkerRage, v => ModMenu.BerserkerRage = v));
            list.Add(Toggle("ninja_speed_boost", "feature.ninja_speed_boost.label", TabCategory.Lab, "feature.section.units",
                "feature.ninja_speed_boost.description",
                () => ModMenu.NinjaSpeedBoost, v => ModMenu.NinjaSpeedBoost = v));

            list.Add(Slider("recruit_cap", "feature.recruit_cap.label", TabCategory.Lab, "feature.section.lab_rules",
                "feature.recruit_cap.description",
                () => ModMenu.RecruitCap, v => ModMenu.RecruitCap = (int)v, 0, 50));
            list.Add(Slider("tree_regrow", "feature.tree_regrow.label", TabCategory.Lab, "feature.section.world",
                "feature.tree_regrow.description",
                () => ModMenu.TreeRegrowthMult, v => ModMenu.TreeRegrowthMult = v, 0.1f, 5.0f));

            list.Add(Toggle("farm_output", "feature.farm_output.label", TabCategory.Lab, "feature.section.world",
                "feature.farm_output.description",
                () => ModMenu.FarmOutputBoost, v => ModMenu.FarmOutputBoost = v));
            list.Add(Toggle("tower_fire", "feature.tower_fire.label", TabCategory.Lab, "feature.section.world",
                "feature.tower_fire.description",
                () => ModMenu.TowerFireBoost, v => ModMenu.TowerFireBoost = v));
            list.Add(Toggle("ballista_boost", "feature.ballista_boost.label", TabCategory.Lab, "feature.section.world",
                "feature.ballista_boost.description",
                () => ModMenu.BallistaBoost, v => ModMenu.BallistaBoost = v));
            list.Add(Slider("ballista_reload", "feature.ballista_reload.label", TabCategory.Lab, "feature.section.world",
                "feature.ballista_reload.description",
                () => ModMenu.BallistaReloadMult, v => ModMenu.BallistaReloadMult = v, 0.001f, 2.0f));
            list.Add(Slider("ballista_flight", "feature.ballista_flight.label", TabCategory.Lab, "feature.section.world",
                "feature.ballista_flight.description",
                () => ModMenu.BallistaFlightMult, v => ModMenu.BallistaFlightMult = v, 1.0f, 5.0f));

            list.Add(Toggle("catapult_boost", "feature.catapult_boost.label", TabCategory.Lab, "feature.section.world",
                "feature.catapult_boost.description",
                () => ModMenu.CatapultBoost, v => ModMenu.CatapultBoost = v));
            list.Add(Slider("catapult_reload", "feature.catapult_reload.label", TabCategory.Lab, "feature.section.world",
                "feature.catapult_reload.description",
                () => ModMenu.CatapultReloadMult, v => ModMenu.CatapultReloadMult = v, 0.001f, 2.0f));
            list.Add(Slider("catapult_flight", "feature.catapult_flight.label", TabCategory.Lab, "feature.section.world",
                "feature.catapult_flight.description",
                () => ModMenu.CatapultFlightMult, v => ModMenu.CatapultFlightMult = v, 1.0f, 5.0f));
            list.Add(Toggle("instant_castle", "feature.instant_castle.label", TabCategory.Lab, "feature.section.world",
                "feature.instant_castle.description",
                () => ModMenu.InstantCastle, v => ModMenu.InstantCastle = v));
            list.Add(Toggle("instant_day_skip", "feature.instant_day_skip.label", TabCategory.Lab, "feature.section.world",
                "feature.instant_day_skip.description",
                () => ModMenu.InstantDaySkip, v => ModMenu.InstantDaySkip = v));
            list.Add(Toggle("animal_spawn", "feature.animal_spawn.label", TabCategory.Lab, "feature.section.world",
                "feature.animal_spawn.description",
                () => ModMenu.AnimalSpawnBoost, v => ModMenu.AnimalSpawnBoost = v));

            list.Add(Toggle("charge_dmg", "feature.charge_dmg.label", TabCategory.Lab, "feature.section.steed",
                "feature.charge_dmg.description",
                () => ModMenu.ChargeDmgBoost, v => ModMenu.ChargeDmgBoost = v));
            list.Add(Slider("buff_aura", "feature.buff_aura.label", TabCategory.Lab, "feature.section.steed",
                "feature.buff_aura.description",
                () => ModMenu.BuffAuraDuration, v => ModMenu.BuffAuraDuration = v, 1.0f, 10.0f));

            // ==================== HARD ====================
            list.Add(Toggle("no_crown_stealing", "feature.no_crown_stealing.label", TabCategory.Hard, "feature.section.wave_control",
                "feature.no_crown_stealing.description",
                () => ModMenu.NoCrownStealing, v => ModMenu.NoCrownStealing = v,
                () => DifficultyRules.IsHardModeActive(), () => "feature.lock.hard_mode"));

            list.Add(Slider("wave_size", "feature.wave_size.label", TabCategory.Hard, "feature.section.wave_sliders",
                "feature.wave_size.description",
                () => ModMenu.WaveSizeMult, v => ModMenu.WaveSizeMult = v, 0.1f, 5.0f));
            list.Add(Slider("enemy_speed", "feature.enemy_speed.label", TabCategory.Hard, "feature.section.wave_sliders",
                "feature.enemy_speed.description",
                () => ModMenu.EnemySpeedMult, v => ModMenu.EnemySpeedMult = v, 0.5f, 3.0f));
            list.Add(Slider("portal_rate", "feature.portal_rate.label", TabCategory.Hard, "feature.section.wave_sliders",
                "feature.portal_rate.description",
                () => ModMenu.PortalSpawnRate, v => ModMenu.PortalSpawnRate = v, 0.1f, 5.0f));
            list.Add(Slider("queen_hp", "feature.queen_hp.label", TabCategory.Hard, "feature.section.wave_sliders",
                "feature.queen_hp.description",
                () => ModMenu.GreedQueenHPScale, v => ModMenu.GreedQueenHPScale = v, 0.5f, 5.0f));
            list.Add(Slider("threat", "feature.threat.label", TabCategory.Hard, "feature.section.wave_sliders",
                "feature.threat.description",
                () => ModMenu.DirectorThreatMult, v => ModMenu.DirectorThreatMult = v, 0.1f, 5.0f));

            return list.ToArray();
        }
    }
}
