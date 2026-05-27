using System.Collections.Generic;
using UnityEngine;

public static class RingMeshUtility
{
    /// <summary>Planetary ring annulus in the XZ plane (Y up is normal). UV.x wraps around the ring; UV.y is radial.</summary>
    public static Mesh BuildAnnulus(float innerRadius, float outerRadius, int segments = 128)
    {
        var mesh = new Mesh { name = "PlanetRingAnnulus" };
        var verts = new List<Vector3>(segments * 2 + 2);
        var uvs = new List<Vector2>(segments * 2 + 2);
        var tris = new List<int>(segments * 6);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ang = t * Mathf.PI * 2f;
            float c = Mathf.Cos(ang);
            float s = Mathf.Sin(ang);

            verts.Add(new Vector3(c * innerRadius, 0f, s * innerRadius));
            verts.Add(new Vector3(c * outerRadius, 0f, s * outerRadius));
            uvs.Add(new Vector2(t, 0f));
            uvs.Add(new Vector2(t, 1f));

            if (i == segments) break;

            int b = i * 2;
            tris.Add(b);
            tris.Add(b + 1);
            tris.Add(b + 2);
            tris.Add(b + 1);
            tris.Add(b + 3);
            tris.Add(b + 2);
        }

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
