# Usage Guide

Kingdom Enhanced v2.2.0 — in-game menu, hotkeys, features and accessibility.

---

## Hotkeys

| Key | Function |
|-----|----------|
| `F1` | Open / close the Mod Menu |
| `F2` | Refill wallet (50 coins) — locked by active hard-mode preset if restricted |
| `F3` | Toggle the Kingdom Monitor |
| `F4` | Toggle the HUD overlay (day/night time + coins) |
| `F5` | Radar ping (nearby portals, chests, vagrants) |
| `Shift + F5` | Detailed report of the currently selected object |
| `F6` | Compass & safety report (position, direction, threat) |
| `F7` | Wallet report |
| `F8` | World report (day, time, greed count) |
| `F9` | Mount report |
| `F10` | Companions report (population) |
| `F11` | Repeat last narration |
| `Shift + F11` | Read previous narration history entry |

> Accessibility reports (F5–F11) only run when **Accessibility & Radar** is enabled in the menu.

---

## Mod Menu (F1)

The menu sidebar contains eight tabs: **Main, Cheats, Lab, Hard, Info, Guide, Settings, Report**.

### Main

**HUD**
- **Energy Bar** — shows the mount stamina bar on screen.
- **Cycle Energy Bar Style** — switches between 4 visual styles (Classic, Neon, Light, Ghost).
- **Cycle Energy Bar Position** — switches between 6 screen positions (top, bottom, side, manual).
- **HUD Display** — toggles the whole HUD overlay (day/night time + coins).
- **12-Hour Clock** — shows AM/PM time instead of 24-hour format in the HUD (localized 上午/下午 in zh-CN).
- **Cycle Monitor Style** — switches the Kingdom Monitor panel style.

**Accessibility**
- **Accessibility & Radar** — master switch for narration, radar and proximity alerts.
- **Narrator (TTS)** — enables text-to-speech output.
- **Simplify Names** — shortens object names before narration.
- **Castle Announcer** — announces entering/leaving the castle zone.

**Movement / Player**
- **Travel Speed** — mounted travel speed multiplier (0.5–10×).
- **Player Size Hack** — enables scaling of the monarch sprite.
- **Player Size** — scale multiplier (0.2–3×).

### Cheats

> The Cheats tab is locked until you press the unlock button on it (once per config).

**Invincibility**
- **Infinite Mount Stamina**
- **No Tool Cooldowns** — removes item ability cooldowns (includes Artemis Bow).

**Artemis Bow**
- Arrow count per cast, ability range multiplier, arrow damage multiplier.

**Economy**
- Add 10 / 50 coins, add 5 gems, fill wallet to 100 coins.
- **Coin Income** — income multiplier (0.25–4×).
- **Bag Drop** — bag drop multiplier (1–10×).
- **Clear Coins** — removes all coins lying on the ground.

**Military**
- **Recruit All Vagrants** — hires all nearby vagrants (respects hard-mode limit).
- **Drop Archer / Builder Tools** — drops a tool at every unemployed villager.
- **Kill All Enemies**, **Destroy All Portals**.
- **Max Army** — fills the army up to the recruit cap.
- **Spawn Units** — vagrant, villager, archer, builder, farmer, pikeman, ninja, berserker, knight (biome-restricted units are blocked in the wrong biome).
- **Spawn Hermits** — bakery, ballista, berserker, fire, horn, stable, warrior.
- **Spawn Enemies** — weak, squid, stealer, boss, crusher, knight, archer.
- **Spawn Count** — number of units/enemies spawned per action.

### Lab (Experimental)

**World**
- **Hyper Builders** (instant construction), **Builder Speed**, **Builder Work** (efficiency).
- **Larger Camps**, **Lock Summer**, **Clear Weather**, **Coins Stay Dry**, **No Blood Moons**.
- **Invincible Walls** (self-repairing), **Better Citizen Houses**, **Better Knight** (50 HP).
- **Instant Day Skip** plus manual **skip daytime / skip nighttime** buttons.
- **Tree Regrowth** multiplier, **Animal Spawn** boost.

**Units**
- **Archer Fire** (×2), **Berserker Rage** (earlier + faster), **Ninja Speed** (×2), **Recruit Cap** override.

**Buildings**
- **Farm Output** (×2), **Tower Fire** (×2), **Ballista Boost** (damage ×2) with reload/flight multipliers, **Catapult Boost** (reload ×3) with reload/flight multipliers, **Instant Castle**.

### Hard (Custom Difficulty)

Experimental director overrides with presets: **Nightmare, Relentless, Oblivion, No Escape**.

Manual controls: **Crown Stealing** disable, **Wave Size** multiplier, **Enemy Speed**, **Portal Spawn Rate**, **Greed Queen HP** scale, **Director Threat** ramp, **Steed Speed**, **Charge Damage** (×2), **Buff Aura Duration**.

> ⚠️ Experimental — behavior may vary between islands and modes.

### Info / Guide / Report

- **Info** — version, developer and hotkey tips.
- **Guide** — every feature with its current state and description.
- **Report** — mark features as *works / broken / untested* and copy a formatted report to the clipboard for bug reports.

### Settings

- **Window Scale** (0.5–2×) and **Menu Opacity** (0.5–1).
- **Language** — `English` / `简体中文`, persisted immediately; runtime switch applies to all UI and narration.
- **One-click Reset** — press the reset button twice (confirmation) to restore every setting to its default and reload the menu.

---

## Kingdom Monitor (F3)

Real-time dashboard showing day/cycle, threat status, greed count, wallet and population (archers, workers, villagers, farmers, pikemen, knights, vagrants). Drag the header to move, drag the bottom-right handle to resize.

---

## Localization

- Default language `en-US`, plus `zh-CN`; switch under **F1 → Settings → Language**.
- Catalogs are **embedded in the DLL**, so an external `Localization/` folder is optional. To override, drop your own `en-US.json` / `zh-CN.json` next to the DLL — external files take precedence.
- Fallback order: current language → English → raw resource key.

---

## Performance notes

- Per-frame hot spots (mover speed reflection, catapult/archer update hooks) only write when values actually change.
- TTS is deduplicated (identical text within 2 s is dropped) and the queue is capped at 32 messages.
- HUD and monitor texts are cached and only rebuilt on change.
