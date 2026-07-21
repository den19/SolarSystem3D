# Projection mode implementation
_Exported on 7/21/2026 at 22:51:07 GMT+3 from Cursor (3.12.17)_

---

**User**

Нужен новый режим Проекция, который выключен по умолчанию.
Это проекция движущегося космического тела (планеты\спутника) на гравитационную сетку. В виде затухающего градиента, зелёного цвета. (Это пожеление астронома).
Должно быть красиво, правдоподобно, совместимо со всеми остальными режимами, чтобы ничего не сломалось. Смысл - показать что нагляднее, что все объекты - в гравитационном колодце.
Локализация, обновление документации.

---

**Cursor**

Изучу, как устроены режимы визуализации, гравитационная сетка и локализация, чтобы спланировать режим «Проекция».

Уточню детали реализации по ключевым файлам и визуальному паттерну.

Выбираю классический «колодец»: зелёное пятно на сетке и вертикальная линия от тела — так нагляднее всего.

---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Начинаю с `ProjectionSettings` и проводки тумблера на боковой панели.

Создаю settings и подключаю UI, затем сэмплер сетки и визуал.

Восстанавливаю компактный формат Unity JSON (без пробела после `:`).

Режим **Проекция** готов (выкл. по умолчанию).

**Что сделано**
- `ProjectionSettings` + сброс в `SolarSystemConfigurationReset`
- Тумблер на боковой панели сразу после «Гравитационная сетка»
- `SampleSurfaceWorldY` в `SpacetimeGridController` (single/dual Educational)
- `BodyGridProjectionController`: зелёные пятна + вертикали для планет/спутников
- Шейдер/материал `GridProjectionGlow`
- Локализация (5 языков), ключи проверены
- Обновлены `Doc/Руководство_программиста.md` и `.txt`

В Play Mode включите **Проекция** на боковой панели — под телами появятся зелёные проекции на поверхность колодца.

---

**User**

Assets\Scripts\SolarSystemGraphics\BodyGridProjectionController.cs(109,25): error CS0104: 'ScaleMode' is an ambiguous reference between 'SolarSystemApp.ScaleMode' and 'UnityEngine.ScaleMode'


---

**Cursor**

Исправляю неоднозначность `ScaleMode`.

Исправлено: для `ScaleMode` используется алиас `SolarScaleMode`, как в `SpacetimeGridController`. Ошибка CS0104 должна исчезнуть.

---

**User**

В режиме Schematic у Урана и Нептуна проекции (зелёные пятна) видны вне гравитационной сетки. Подумай как исправить, ничего не сломая.

---

**Cursor**

Разберусь, почему в Schematic проекции Урана и Нептуна выходят за край сетки.

Причина: `SampleSurfaceWorldY` считает высоту для любой точки XZ, а пятна рисуются и за пределами меша. В Schematic сетка ~±120, а Уран/Нептун в сцене/при сбое layout могут оказаться дальше.

---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Готово. Проекции скрываются, если XZ тела вне квадрата сетки (`ContainsWorldXZ`); орбиты и сама сетка не менялись.

---

**User**

Не совсем то. Пусть у Урана и Нептуна проекции находятся на уровне продавливания гравитационной сетки, не ниже. И у всех объектов тоже - не ниже уровня продавливания. Ничего не сломая, совместимо со всеми режимами.

---

**Cursor**

Разберусь, почему точки проекции оказываются ниже поверхности продавливания сетки.

---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

При выкл. сетке не использовать устаревший меш — только аналитику с half-cell floor.

Готово.

Проекции садятся на **видимую** поверхность колодца: билинейный семпл меша после деформации (сетка `-50`, проекции `+50` в `LateUpdate`). Если сетка выкл. — аналитика с half-cell floor, без ухода ниже типичной вершины.

---

**User**

Проекции должны оставлять длинный затухающий след на простыне сетки (градиент) зеленого цвета.

---

**Cursor**

Изучу текущую реализацию проекций, чтобы спланировать длинный затухающий зелёный след на сетке.

---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Переписываю контроллер: буфер следа, LineRenderer с градиентом и пересэмпл Y каждый кадр.

Исправляю градиент: хвост (начало линии) должен быть прозрачным, голова — яркой.

Готово. У каждой проекции теперь длинный зелёный след по ткани сетки: буфер точек с пересчётом Y каждый кадр, градиент alpha к хвосту и сужение ширины. Пятно и вертикаль на месте.

