---
name: Mobile HD Sun Boost
overview: Поднять wow-графику на смартфонах через tier High/Balanced (авто + привязка к Extra Graphics), усилить Mobile URP и close-up Real Sun VFX, затем обновить Руководство программиста.
todos:
  - id: tier-urp
    content: GraphicsTierSettings + reset + MobileUrpQualityApplicator
    status: completed
  - id: sun-vfx
    content: Усилить SunCoronalVfxController (granulation/bloom/close-up)
    status: completed
  - id: tier-ui
    content: UI GraphicsTier в MainMenu + локализация
    status: completed
  - id: docs
    content: Обновить Doc MD и TXT
    status: completed
isProject: false
---

# Mobile HD Sun + документация

## Подход (фиксированный)

- **Balanced** (текущий Mobile URP): `renderScale = 0.8`, MSAA off.
- **High**: `renderScale = 1.0`, MSAA 2× на mobile URP asset в runtime.
- High включается автоматически на устройствах с `SystemInfo.graphicsMemorySize >= 2048` **и** когда Extra Graphics ON; иначе Balanced. Ручной override через PlayerPrefs `SolarSystem_GraphicsTier` (`Auto` / `Balanced` / `High`).
- В Main Menu рядом с Extra Graphics — dropdown/toggle «Качество графики» (Auto/Balanced/High), по паттерну [`ExtraGraphicsControlController.cs`](Assets/Scripts/ExtraGraphicsControlController.cs). Если в сцене нет слота под новый контрол — runtime bootstrap создаёт компактный dropdown под существующим Extra Graphics рядом в Settings (минимальный UI).
- Real Sun: включить granulation на close zoom, усилить corona bloom/emission при фокусе на Солнце.

## 1. Настройки tier

Новый [`Assets/Scripts/SolarSystemGraphics/GraphicsTierSettings.cs`](Assets/Scripts/SolarSystemGraphics/GraphicsTierSettings.cs):

```csharp
enum GraphicsTierMode { Auto = 0, Balanced = 1, High = 2 }
// EffectiveTier = High if (mode==High) || (mode==Auto && VRAM>=2048)
// иначе Balanced; High дополнительно требует GraphicsSettings.UseExtraGraphics
```

Событие `EffectiveTierChanged`. Сброс в [`SolarSystemConfigurationReset.cs`](Assets/Scripts/SolarSystemConfigurationReset.cs) + `SunAppearanceSettings.ResetToDefaults()` (закрыть дыру в reset).

## 2. Применение Mobile URP

Новый DDOL [`Assets/Scripts/MobileUrpQualityApplicator.cs`](Assets/Scripts/MobileUrpQualityApplicator.cs) (`RuntimeInitializeOnLoad`):

- На Android/iOS читает `UniversalRenderPipelineAsset` из `QualitySettings.renderPipeline`.
- Пишет `renderScale` и `msaaSampleCount` (1 или 2) по EffectiveTier.
- Подписка на Extra Graphics + GraphicsTier.
- На Editor/Standalone не трогает PC asset (только mobile platforms).

Базовые значения взять из [`Mobile_RPAsset.asset`](Assets/Settings/Mobile_RPAsset.asset) (сейчас 0.8 / MSAA 1).

## 3. Прокачка Real Sun

В [`SunCoronalVfxController.cs`](Assets/Scripts/SolarSystemGraphics/SunCoronalVfxController.cs):

- В `ApplyHdState` / `ApplySunSurfaceLod`: **включать granulation** при `detailBlend` выше порога (~0.35), иначе выкл (экономия на дальнем виде).
- При close zoom усилить bloom emission multipliers (сейчас сильно гасятся `* 0.28` — поднять floor на High tier / при Real Sun focus).
- Чуть сильнее flicker/sunspots на close zoom (уже есть lerp — сдвинуть кривую в сторону «wow»).
- Не требовать смены сцены: только runtime materials/VFX.

## 4. UI MainMenu

- [`GraphicsTierControlController.cs`](Assets/Scripts/GraphicsTierControlController.cs) + локализация ключей `GraphicsTierLabel` / `GraphicsTierAuto` / `Balanced` / `High` во все 5 JSON.
- Wiring: найти Settings рядом с Extra Graphics; если Dropdown отсутствует — создать runtime (как Share bootstrap), без обязательного Editor setup для первого релиза.

## 5. Документация

Обновить [`Doc/Руководство_программиста.md`](Doc/Руководство_программиста.md) и `.txt`:

- секция Mobile vs PC URP (таблица renderScale/MSAA/тени);
- Extra Graphics + Graphics Tier + Real Sun;
- как получить «бомбическое» Солнце на устройстве;
- PlayerPrefs ключ tier;
- убрать/уточнить пункт про reset без Real Sun.

## Проверка

1. Android mid-range / Editor с Forced Mobile quality: Auto → Balanced или High по VRAM.
2. Extra Graphics OFF → всегда Balanced URP даже на флагмане.
3. Фокус на Солнце + Real Sun: видны granulation/spots/flicker, bloom не «мыло».
4. Reset настроек возвращает Auto + Extra Graphics ON + Real Sun ON.
5. Doc MD/TXT синхронны.
