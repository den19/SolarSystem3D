using UnityEngine;

/// <summary>
/// Billboard cyan ring visible in World camera mode when the craft is far from the main view.
/// </summary>
public class ProbeWorldMarker : MonoBehaviour
{
    const float RingSize = 4f;

    Transform _ring;
    Material _material;

    void Awake()
    {
        _ring = BuildRing();
    }

    void LateUpdate()
    {
        if (_ring == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            _ring.gameObject.SetActive(false);
            return;
        }

        bool worldMode = SolarSystemApp.ProbeSettings.CameraMode == SolarSystemApp.ProbeCameraMode.World;
        _ring.gameObject.SetActive(worldMode);
        if (!worldMode)
            return;

        _ring.position = transform.position;
        _ring.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    Transform BuildRing()
    {
        var go = new GameObject("ProbeWorldMarkerRing");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * RingSize;

        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = BuildRingMesh(1f, 0.82f, 48);

        var meshRenderer = go.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        _material = new Material(shader);
        _material.color = new Color(0.2f, 0.92f, 1f, 0.72f);
        meshRenderer.sharedMaterial = _material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return go.transform;
    }

    static Mesh BuildRingMesh(float outerRadius, float innerRadius, int segments)
    {
        var mesh = new Mesh { name = "ProbeWorldMarkerRingMesh" };
        int vertCount = segments * 2;
        var vertices = new Vector3[vertCount];
        var triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            int outer = i * 2;
            int inner = outer + 1;
            vertices[outer] = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
            vertices[inner] = new Vector3(cos * innerRadius, sin * innerRadius, 0f);

            int nextOuter = ((i + 1) % segments) * 2;
            int nextInner = nextOuter + 1;
            int tri = i * 6;
            triangles[tri] = outer;
            triangles[tri + 1] = nextOuter;
            triangles[tri + 2] = inner;
            triangles[tri + 3] = inner;
            triangles[tri + 4] = nextOuter;
            triangles[tri + 5] = nextInner;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
