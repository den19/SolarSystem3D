using SolarSystemApp;
using UnityEngine;

/// <summary>
/// Cyan drop/glow/trail for the flying probe on the gravity-grid fabric.
/// </summary>
[DefaultExecutionOrder(55)]
public class ProbeGridProjection : MonoBehaviour
{
    static readonly Color Cyan = new Color(0.25f, 0.85f, 1f, 0.7f);

    LineRenderer _drop;
    LineRenderer _trail;
    Transform _glow;
    readonly Vector3[] _trailPts = new Vector3[64];
    int _trailCount;
    SpacetimeGridController _grid;
    Material _glowMat;
    Mesh _quad;

    public static ProbeGridProjection EnsureOnHost(GameObject host)
    {
        if (host == null)
            return null;

        var projection = host.GetComponent<ProbeGridProjection>();
        if (projection == null)
            projection = host.AddComponent<ProbeGridProjection>();
        return projection;
    }

    public void Tick(ProbeSystemController system)
    {
        bool want = system != null && system.IsFlying && ProjectionSettings.UseProjection;
        if (!want)
        {
            SetVisible(false);
            _trailCount = 0;
            return;
        }

        EnsureVisuals();
        _grid = _grid != null ? _grid : FindFirstObjectByType<SpacetimeGridController>();
        Vector3 pos = system.Craft.transform.position;
        float y = _grid != null ? _grid.SampleSurfaceWorldY(pos.x, pos.z) : pos.y - 4f;
        Vector3 foot = new Vector3(pos.x, y + 0.04f, pos.z);

        SetVisible(true);
        _drop.SetPosition(0, pos);
        _drop.SetPosition(1, foot);
        _glow.position = foot;
        _glow.localScale = new Vector3(2.4f, 1f, 2.4f);

        if (_trailCount == 0 || (foot - _trailPts[_trailCount - 1]).sqrMagnitude > 0.25f)
        {
            if (_trailCount >= _trailPts.Length)
            {
                for (int i = 1; i < _trailPts.Length; i++)
                    _trailPts[i - 1] = _trailPts[i];
                _trailCount = _trailPts.Length - 1;
            }

            _trailPts[_trailCount++] = foot;
        }

        _trail.positionCount = _trailCount;
        for (int i = 0; i < _trailCount; i++)
            _trail.SetPosition(i, _trailPts[i]);
    }

    void EnsureVisuals()
    {
        if (_drop != null)
            return;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        var lineMat = new Material(shader);
        lineMat.color = Cyan;

        var dropGo = new GameObject("ProbeProjectionDrop");
        dropGo.transform.SetParent(transform, false);
        _drop = dropGo.AddComponent<LineRenderer>();
        ConfigureLine(_drop, lineMat, 0.05f, 2);

        var trailGo = new GameObject("ProbeProjectionTrail");
        trailGo.transform.SetParent(transform, false);
        _trail = trailGo.AddComponent<LineRenderer>();
        ConfigureLine(_trail, lineMat, 0.08f, 0);

        _quad = new Mesh { name = "ProbeGlowQuad" };
        _quad.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };
        _quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        _quad.RecalculateNormals();

        Shader glowShader = Shader.Find("Custom/GridProjectionGlow");
        if (glowShader == null)
            glowShader = shader;
        _glowMat = new Material(glowShader);
        if (_glowMat.HasProperty("_Color"))
            _glowMat.SetColor("_Color", Cyan);
        if (_glowMat.HasProperty("_BaseColor"))
            _glowMat.SetColor("_BaseColor", Cyan);

        var glowGo = new GameObject("ProbeProjectionGlow");
        glowGo.transform.SetParent(transform, false);
        _glow = glowGo.transform;
        var filter = glowGo.AddComponent<MeshFilter>();
        filter.sharedMesh = _quad;
        var renderer = glowGo.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _glowMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void ConfigureLine(LineRenderer line, Material material, float width, int positions)
    {
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.positionCount = positions;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    void SetVisible(bool visible)
    {
        if (_drop != null)
            _drop.enabled = visible;
        if (_trail != null)
            _trail.enabled = visible;
        if (_glow != null)
            _glow.gameObject.SetActive(visible);
    }
}
