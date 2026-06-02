using System.Collections.Generic;
using UnityEngine;

public struct RingBandDescriptor
{
    public float InnerRadius;
    public float OuterRadius;

    public RingBandDescriptor(float innerRadius, float outerRadius)
    {
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
    }
}

public static class RingMeshUtility
{
    /// <summary>Planetary ring annulus in the XZ plane (Y up is normal). UV.x wraps around the ring; UV.y is radial.</summary>
    public static Mesh BuildAnnulus(float innerRadius, float outerRadius, int segments = 128)
    {
        return BuildAnnulusBands(new[] { new RingBandDescriptor(innerRadius, outerRadius) }, segments);
    }

    /// <summary>Merges multiple annulus bands into one mesh. UV.y spans the full radial extent across all bands.</summary>
    public static Mesh BuildAnnulusBands(RingBandDescriptor[] bands, int segments = 128)
    {
        if (bands == null || bands.Length == 0)
            return BuildAnnulus(1f, 1.5f, segments);

        float globalInner = bands[0].InnerRadius;
        float globalOuter = bands[0].OuterRadius;
        for (int b = 1; b < bands.Length; b++)
        {
            globalInner = Mathf.Min(globalInner, bands[b].InnerRadius);
            globalOuter = Mathf.Max(globalOuter, bands[b].OuterRadius);
        }

        float radialSpan = Mathf.Max(0.0001f, globalOuter - globalInner);
        var mesh = new Mesh { name = "PlanetRingAnnulusBands" };
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        int vertOffset = 0;

        for (int band = 0; band < bands.Length; band++)
        {
            float inner = bands[band].InnerRadius;
            float outer = bands[band].OuterRadius;
            if (outer <= inner) continue;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float ang = t * Mathf.PI * 2f;
                float c = Mathf.Cos(ang);
                float s = Mathf.Sin(ang);

                float innerNorm = (inner - globalInner) / radialSpan;
                float outerNorm = (outer - globalInner) / radialSpan;

                verts.Add(new Vector3(c * inner, 0f, s * inner));
                verts.Add(new Vector3(c * outer, 0f, s * outer));
                uvs.Add(new Vector2(t, innerNorm));
                uvs.Add(new Vector2(t, outerNorm));

                if (i == segments) break;

                int b = vertOffset + i * 2;
                tris.Add(b);
                tris.Add(b + 1);
                tris.Add(b + 2);
                tris.Add(b + 1);
                tris.Add(b + 3);
                tris.Add(b + 2);
            }

            vertOffset += (segments + 1) * 2;
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
