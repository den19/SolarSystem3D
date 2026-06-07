using System.Collections.Generic;
using UnityEngine;

public static class GravityGridMeshUtility
{
    /// <summary>Flat grid in the XZ plane (Y up). UVs span 0..1 for shader grid lines.</summary>
    public static Mesh BuildFlatGrid(float halfExtentX, float halfExtentZ, int segmentsX, int segmentsZ)
    {
        segmentsX = Mathf.Max(2, segmentsX);
        segmentsZ = Mathf.Max(2, segmentsZ);

        var mesh = new Mesh { name = "SpacetimeGrid" };
        int vertCountX = segmentsX + 1;
        int vertCountZ = segmentsZ + 1;
        var verts = new Vector3[vertCountX * vertCountZ];
        var uvs = new Vector2[verts.Length];
        var tris = new List<int>((segmentsX * segmentsZ) * 6);

        int vi = 0;
        for (int z = 0; z <= segmentsZ; z++)
        {
            float tz = z / (float)segmentsZ;
            float pz = Mathf.Lerp(-halfExtentZ, halfExtentZ, tz);

            for (int x = 0; x <= segmentsX; x++)
            {
                float tx = x / (float)segmentsX;
                float px = Mathf.Lerp(-halfExtentX, halfExtentX, tx);

                verts[vi] = new Vector3(px, 0f, pz);
                uvs[vi] = new Vector2(tx, tz);
                vi++;
            }
        }

        for (int z = 0; z < segmentsZ; z++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int i0 = z * vertCountX + x;
                int i1 = i0 + 1;
                int i2 = i0 + vertCountX;
                int i3 = i2 + 1;

                tris.Add(i0);
                tris.Add(i2);
                tris.Add(i1);
                tris.Add(i1);
                tris.Add(i2);
                tris.Add(i3);
            }
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
