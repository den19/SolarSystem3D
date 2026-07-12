---
name: Fix Europa albedo
overview: "Предупреждения PlanetTextureManager для Europa вызваны битыми GUID в материалах: оба mat ссылаются на несуществующий texture GUID, поэтому albedo = null (розовый/магента на устройстве)."
todos:
  - id: fix-europa-mat-guids
    content: Починить GUID _BaseMap/_MainTex в EuropaTexture.mat и EuropaTexture_HD.mat
    status: completed
  - id: verify-europa-albedo
    content: Проверить отсутствие warning и видимость текстуры Europa
    status: completed
isProject: false
---

# Исправление albedo материалов Europa

## Причина

[`PlanetTextureManager.RegisterBodySwap`](Assets/Scripts/SolarSystemGraphics/PlanetTextureManager.cs) ругается, когда `_BaseMap` / `mainTexture` пустые.

У Europa материалы **ссылаются на GUID, которого нет в проекте**:

| Материал | Текущий `_BaseMap` GUID | Статус |
|----------|-------------------------|--------|
| [`EuropaTexture.mat`](Assets/Materials/EuropaTexture.mat) | `a1b2c3d4e5f7114a10b0cacafe001100` | **не существует** |
| [`EuropaTexture_HD.mat`](Assets/Resources/PlanetGraphicsHD/EuropaTexture_HD.mat) | `a1b2c3d4e5f7114a10b0cacafe001100` | **не существует** |

Реальные текстуры уже лежат в проекте:

- Standard: [`Assets/Textures/HD/EuropaTexture_8k.jpg`](Assets/Textures/HD/EuropaTexture_8k.jpg) → guid `be9c50d74b36d674f981a0b512c87a4f`
- HD/Resources: [`Assets/Resources/PlanetTexturesHD/EuropaTexture_8k.jpg`](Assets/Resources/PlanetTexturesHD/EuropaTexture_8k.jpg) → guid `b4491e8c2e545a246b5b46972a7ba7c4`

У Io/Ganymede паттерн корректный: standard → `Textures/HD`, HD → `PlanetTexturesHD`, оба `_BaseMap` и `_MainTex` заполнены. У Europa `_MainTex` тоже `{fileID: 0}`.

## Исправление

1. В [`Assets/Materials/EuropaTexture.mat`](Assets/Materials/EuropaTexture.mat) выставить `_BaseMap` и `_MainTex` на guid `be9c50d74b36d674f981a0b512c87a4f`.
2. В [`Assets/Resources/PlanetGraphicsHD/EuropaTexture_HD.mat`](Assets/Resources/PlanetGraphicsHD/EuropaTexture_HD.mat) выставить `_BaseMap` и `_MainTex` на guid `b4491e8c2e545a246b5b46972a7ba7c4`.

Код `PlanetTextureManager` менять не нужно.

## Проверка

- Play Mode: в Console нет warnings про Europa albedo.
- Europa в сцене и в HD-режиме с текстурой (не pink/magenta).
- Миниатюра Europa в body picker показывает albedo.
