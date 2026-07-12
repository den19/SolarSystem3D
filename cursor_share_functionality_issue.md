# Share functionality issue
_Exported on 7/12/2026 at 11:44:26 GMT+3 from Cursor (3.11.13)_

---

**User**

На телефоне Tecno Pova Ultra 7 при нажатии на кнопку Поделиться (Share) показывается тост
Unable to share. Please try again.
Включены мобильные данные и wifi.
Нужно исправить.

---

**Cursor**

Ищу в коде реализацию Share и источник сообщения «Unable to share».


Сообщение `ShareFailedMessage` указывает на сбой в `AndroidShareHelper`, а не в захвате экрана или проверке сети. Проверяю настройки minify/ProGuard — они часто ломают FileProvider в release-сборках.


Диагноз: `ShareFailedMessage` — сбой в 

**Уточнение для диагностики Share**

> **Какая сборка установлена на Tecno Pova Ultra 7?**
> - Release (из Play / APK release / AAB) ✔
> - Debug (Build And Run из Unity)
> - Не знаю

> **Когда появляется toast «Unable to share»?**
> - Сразу при нажатии (без паузы) ✔
> - Через ~0.5–2 сек после нажатия
> - Не замечал(а)

`AndroidShareHelper`, не в захвате или сети. Release minify без ProGuard keep для FileProvider — вероятная причина на Android 15 / HiOS 15.


Мгновенный toast указывает на `SimulationShareController.Instance == null`, а не на сбой FileProvider. Проверяю bootstrap.


Уточнение: release-сборка и мгновенный toast сужают причину — вероятен сбой `AndroidShareHelper` (ProGuard/FileProvider) или гонка инициализации bootstrap.


---

**User**

Implement the plan as specified, it is attached for your reference. Do NOT edit the plan file itself.

To-do's from the plan have already been created. Do not create them again. Mark them as in_progress as you work, starting with the first one. Don't stop until you have completed all the to-dos.

---

**Cursor**

Приступаю к плану: сначала ранняя инициализация share в bootstrap.


Все пункты плана выполнены. Кратко, что изменилось:

### 1. Ранняя инициализация share
[`SimulationViewBootstrap.cs`](Assets/Scripts/SimulationViewBootstrap.cs) — `SimulationShareController` и `TransientMessageController` создаются сразу при загрузке Level1, до тяжёлой корутины. Остальные системы обёрнуты в `try/catch`, чтобы их сбой не блокировал share.

### 2. Защита кнопки Share
[`ShareButtonController.cs`](Assets/Scripts/ShareButtonController.cs) — при `Instance == null` и незавершённом bootstrap делается retry через 1 кадр; иначе показывается `ShareNotReadyMessage` (не общий failed).

### 3. Android intent для HiOS 15
[`AndroidShareHelper.cs`](Assets/Scripts/AndroidShareHelper.cs):
- проверка существования файла
- `getUriForFile` через **activity** (не ApplicationContext)
- `grantUriPermission` для всех resolve targets
- `FLAG_GRANT_READ_URI_PERMISSION` на chooser
- полный лог исключений в logcat

### 4. Release minify
- [`proguard-user.txt`](Assets/Plugins/Android/proguard-user.txt) с keep-rules для `FileProvider`
- `useCustomProguardFile: 1` в Project Settings
- `androidx.core:core:1.15.0` в [`build.gradle`](Assets/Plugins/Android/SolarSystemShare.androidlib/build.gradle)

### 5. Прочее
- Убрана блокировка share по интернету в [`SimulationShareController.cs`](Assets/Scripts/SimulationShareController.cs)
- Добавлен `<files-path>` в [`file_paths.xml`](Assets/Plugins/Android/SolarSystemShare.androidlib/res/xml/file_paths.xml)
- Локализация `ShareNotReadyMessage` во всех 5 языках

### Проверка
Unity batch compile прошёл без ошибок C#. Полную **Release APK** нужно собрать в Unity и установить на Tecno.

**На телефоне:**
1. File → Build Settings → Android → Build (Release)
2. Установить APK на Tecno Pova Ultra 7
3. Нажать Share — должен открыться system chooser
4. При сбое: `adb logcat -s Unity` и искать `AndroidShareHelper:`

Если chooser не откроется — пришлите строки из logcat с `AndroidShareHelper` или `FileProvider`.

