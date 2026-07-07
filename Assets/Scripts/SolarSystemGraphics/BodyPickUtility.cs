using UnityEngine;

/// <summary>
/// Screen-space body picking. Chooses the catalog body whose on-screen center is
/// closest to the tap, giving satellites a pixel bias so tiny moons (Phobos, Deimos)
/// win over the planet they orbit. Works in every scale/orbit toggle mode because it
/// reads live world positions each call.
/// </summary>
public static class BodyPickUtility
{
    public const float DefaultPickRadiusPixels = 44f;
    public const float SatellitePixelBias = 26f;

    public static GameObject PickBody(Camera cam, Vector2 screenPos, float pickRadiusPixels = DefaultPickRadiusPixels)
    {
        if (cam == null)
            return null;

        float radius = pickRadiusPixels * PixelScale();

        GameObject best = null;
        float bestScore = float.MaxValue;

        var bodies = SolarSystemCatalog.Bodies;
        for (int i = 0; i < bodies.Length; i++)
        {
            SolarSystemCatalog.BodyDefinition def = bodies[i];
            GameObject go = GameObject.Find(def.objectName);
            if (go == null || !go.activeInHierarchy)
                continue;

            Vector3 sp = cam.WorldToScreenPoint(go.transform.position);
            if (sp.z <= 0f)
                continue; // behind the camera

            float dist = Vector2.Distance(screenPos, new Vector2(sp.x, sp.y));
            if (dist > radius)
                continue;

            bool isSatellite = !string.IsNullOrEmpty(def.orbitCenterName);
            float score = dist - (isSatellite ? SatellitePixelBias : 0f);

            if (score < bestScore)
            {
                bestScore = score;
                best = go;
            }
        }

        return best;
    }

    static float PixelScale()
    {
#if UNITY_ANDROID || UNITY_IOS
        float dpi = Screen.dpi > 1f ? Screen.dpi : 160f;
        return Mathf.Clamp(dpi / 160f, 1f, 3f);
#else
        return 1f;
#endif
    }
}
