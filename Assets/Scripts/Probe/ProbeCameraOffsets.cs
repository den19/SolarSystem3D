using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Bounds-based cockpit / PIP camera offsets and ProbeSelf layer helpers.
/// </summary>
public static class ProbeCameraOffsets
{
    public const string ProbeSelfLayerName = "ProbeSelf";

    const float CockpitMargin = 0.15f;
    const float PipMargin = 0.1f;
    const float MinCockpitOffset = 0.55f;
    const float MinPipOffset = 0.7f;

    struct Profile
    {
        public float CockpitScale;
        public float PipScale;
    }

    static readonly Profile DefaultProfile = new Profile { CockpitScale = 1f, PipScale = 1f };

    static readonly Profile[] Profiles =
    {
        /* Voyager */ new Profile { CockpitScale = 1.25f, PipScale = 1.2f },
        /* NewHorizons */ new Profile { CockpitScale = 1.15f, PipScale = 1.1f },
        /* Juno */ new Profile { CockpitScale = 1.05f, PipScale = 1.05f },
        /* Custom */ DefaultProfile,
        /* Luna1 */ new Profile { CockpitScale = 1.12f, PipScale = 1.08f },
        /* Venera7 */ new Profile { CockpitScale = 1.15f, PipScale = 1.10f },
        /* Luna16 */ DefaultProfile,
        /* Mars3 */ new Profile { CockpitScale = 1.18f, PipScale = 1.12f },
        /* Change4 */ DefaultProfile,
        /* Tianwen1 */ DefaultProfile,
        /* Change5 */ DefaultProfile,
        /* Hayabusa2 */ DefaultProfile,
        /* Akatsuki */ DefaultProfile,
        /* Chandrayaan3 */ DefaultProfile,
    };

    public static int ResolveProbeSelfLayer()
    {
        int named = LayerMask.NameToLayer(ProbeSelfLayerName);
        if (named >= 0)
            return named;
        return 9;
    }

    public static int ProbeSelfLayerBit => 1 << ResolveProbeSelfLayer();

    static Profile GetProfile(ProbeModelKind kind)
    {
        int index = (int)kind;
        if (index < 0 || index >= Profiles.Length)
            return DefaultProfile;
        return Profiles[index];
    }

    public static bool TryGetVisualBounds(ProbeCraft craft, out Bounds bounds)
    {
        bounds = default;
        if (craft == null)
            return false;

        int blipLayer = ProbePrefabFactory.ResolveMinimapBlipLayer();
        Renderer[] renderers = craft.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.gameObject.name == ProbePrefabFactory.BlipName)
                continue;
            if (renderer.gameObject.layer == blipLayer)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    public static float ClearanceAlongDirection(Bounds bounds, Vector3 origin, Vector3 direction)
    {
        Vector3 dir = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.forward;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        float max = 0f;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
            float proj = Vector3.Dot(corner - origin, dir);
            if (proj > max)
                max = proj;
        }

        return Mathf.Max(0f, max);
    }

    public static Vector3 ResolveForward(ProbeCraft craft, Vector3 velocity)
    {
        if (velocity.sqrMagnitude > 1e-5f)
            return velocity.normalized;
        if (craft != null)
            return craft.transform.forward;
        return Vector3.forward;
    }

    public static Vector3 ResolveUp(ProbeCraft craft, Vector3 forward)
    {
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.95f && craft != null)
            up = craft.transform.up;
        return up;
    }

    public static Vector3 CockpitWorldPosition(ProbeCraft craft, Vector3 velocity)
    {
        Vector3 forward = ResolveForward(craft, velocity);
        Vector3 origin = craft != null ? craft.transform.position : Vector3.zero;
        Profile profile = craft != null ? GetProfile(craft.Model) : DefaultProfile;
        float clearance = MinCockpitOffset;
        if (TryGetVisualBounds(craft, out Bounds bounds))
            clearance = ClearanceAlongDirection(bounds, origin, forward);
        float distance = Mathf.Max(MinCockpitOffset, clearance + CockpitMargin) * profile.CockpitScale;
        return origin + forward * distance;
    }

    public static Quaternion CockpitWorldRotation(ProbeCraft craft, Vector3 velocity)
    {
        Vector3 forward = ResolveForward(craft, velocity);
        Vector3 up = ResolveUp(craft, forward);
        return Quaternion.LookRotation(forward, up);
    }

    public static Vector3 PipWorldPosition(ProbeCraft craft, Vector3 direction)
    {
        Vector3 dir = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.forward;
        Vector3 origin = craft != null ? craft.transform.position : Vector3.zero;
        Profile profile = craft != null ? GetProfile(craft.Model) : DefaultProfile;
        float clearance = MinPipOffset;
        if (TryGetVisualBounds(craft, out Bounds bounds))
            clearance = ClearanceAlongDirection(bounds, origin, dir);
        float distance = Mathf.Max(MinPipOffset, clearance + PipMargin) * profile.PipScale;
        return origin + dir * distance;
    }
}
