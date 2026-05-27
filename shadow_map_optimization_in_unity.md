# Shadow map optimization in Unity
_Exported on 5/27/2026 at 04:32:07 GMT+3 from Cursor (3.4.20)_

---

**User**

Reduced additional punctual light shadows resolution by 2 to make 6 shadow maps fit in the 2048x2048 shadow atlas. To avoid this, increase shadow atlas size, decrease big shadow resolutions, or reduce the number of shadow maps active in the same frame
UnityEngine.GUIUtility:ProcessEvent (UnityEngine.EntityId,intptr,bool&)

---

**Cursor**

Searching the Unity project for shadow and rendering pipeline settings so we can plan how to address the punctual-light shadow atlas warning.



Identifying cause: Unity URP is packing punctual-light shadow maps into the Additional Lights shadow atlas (`m_AdditionalLightsShadowmapResolution: 2048` in `[PC_RPAsset.asset](Assets/Settings/PC_RPAsset.asset)`). `[Level1.unity](Assets/_Scenes/Level1.unity)` includes a **Point Light** with shadows enabled (`m_Shadows.m_Type` non-zero), which uses six cubemap faces—matching the "**6 shadow maps**" message—while lights use **`m_AdditionalLightsShadowResolutionTier: 2`** (pipeline **High**, max `1024` per tier caps in the same asset). Unity scales resolution when the requested footprints do not fit.



