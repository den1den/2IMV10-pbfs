using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains the model for the triangular mesh around the particle model.
/// Only this class should be used to display the triangluar mesh in Unity
/// </summary>
public class TriangularModelMesh : TriangularMesh
{
    private ParticleModel model;

    private Mesh mesh;
    private Vector3[] points;
    private SubMesh[] subMeshes;

    public TriangularModelMesh(ParticleModel model, ClothSimulation settings)
    {
        this.model = model;

        GameObject meshGameObject = new GameObject("TriangularModelMesh");

        MeshFilter meshFilter = meshGameObject.AddComponent<MeshFilter>();
        this.mesh = meshFilter.mesh;

        MeshRenderer meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
        Material defaultMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        meshRenderer.sharedMaterial = defaultMaterial;

        this.mesh.Clear();

        const int subMeshPoints = 5; // 5 extra points added for the submesh per 4 particle points
        const int subMeshTriangleCount = 8; // 8 triangles added for each submesh
        int modelWH = this.model.getWidthHeight();
        subMeshes = new SubMesh[(modelWH - 1) * (modelWH - 1)];

        // Store all the points in the triangluar mesh
        // where points[] = particle model points + extra triangluar mesh points
        // s.t. points[i] = model.positions[i]
        points = new Vector3[model.positions.Length + subMeshPoints * subMeshes.Length];
        Array.Copy(model.positions, points, model.positions.Length);

        int index = 0;
        for (int i = 0; i < modelWH - 1; i++)
            for(int j = 0; j < modelWH - 1; j++)
            {
                int a = i * modelWH + j;
                int b = i * modelWH + j + 1;
                int c = (i + 1) * modelWH + j;
                int d = (i + 1) * modelWH + j + 1;
                SubMesh subMesh = new SubMesh(
                    model.positions.Length + index * subMeshPoints,
                    a, b, c, d,
                    ref points
                );
                subMesh.update();
                subMeshes[index] = subMesh;
                index++;
            }

        this.mesh.vertices = points;

        // Calculate the uvs
        Vector2[] uv = new Vector2[points.Length]; // uv maps texture to points
        float du = 1.0f / modelWH;
        float dv = 1.0f / modelWH;
        for (int u = 0; u < modelWH; u++)
            for (int v = 0; v < modelWH; v++)
            {
                uv[u * modelWH + v] = new Vector2(u * du, v * dv);
            }
        foreach(SubMesh subMesh in subMeshes)
        {
            subMesh.setUv(ref uv);
        }
        this.mesh.uv = uv;

        int[] triangles = new int[3 * subMeshTriangleCount * subMeshes.Length];
        for (int i = 0; i < subMeshes.Length; i++)
        {
            int[] subMeshTriangles = subMeshes[i].getTriangles();
            Debug.Assert(3 * subMeshTriangleCount == subMeshTriangles.Length);
            Array.Copy(subMeshTriangles, 0, triangles, i * 3 * subMeshTriangleCount, 3 * subMeshTriangleCount);
        }
        this.mesh.triangles = triangles;
    }

    public override void Update()
    {
        // No calculations needed on subparticles
        // Update positions
        Vector3[] vertices = this.mesh.vertices;
        for (int i = 0; i < this.model.positions.Length; i++)
        {
            vertices[i] = this.model.positions[i];
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        // triangles remains the same
        // uv remains the same
    }

    /// <summary>
    /// A submesh of 8 triangles between 4 points:
    /// [/][/]
    /// [/][/]
    /// </summary>
    struct SubMesh
    {
        int a, b, c, d;
        int ab, bc, cd, da, m;
        Vector3[] points;
        public SubMesh(int startIndex, int a, int b, int c, int d, ref Vector3[] points)
        {
            this.points = points;
            this.a = a; this.b = b; this.c = c; this.d = d;
            ab = startIndex + 0;
            bc = startIndex + 1;
            cd = startIndex + 2;
            da = startIndex + 3;
            m = startIndex + 4;
            if(m >= points.Length)
            {
                int k = 7;
            }
        }

        public int[] getTriangles()
        {
            return new int[]
            {
                a, m, da,
                a, ab, m,
                ab, bc, m,
                ab, b, bc,
                da, cd, d,
                da, m, cd,
                m, c, cd,
                m, bc, c
            };
        }

        public void update()
        {
            points[ab] = points[a] + points[b] * 0.5f;
            points[bc] = points[b] + points[c] * 0.5f;
            points[cd] = points[c] + points[d] * 0.5f;
            points[da] = points[d] + points[a] * 0.5f;
            points[m] = points[a] + points[c] * 0.5f;
        }

        public void setUv(ref Vector2[] uv)
        {
            uv[ab] = uv[a] + uv[b] * 0.5f;
            uv[bc] = uv[b] + uv[c] * 0.5f;
            uv[cd] = uv[c] + uv[d] * 0.5f;
            uv[da] = uv[d] + uv[a] * 0.5f;
            uv[m] = uv[a] + uv[c] * 0.5f;
        }
    }
}
public abstract class TriangularMesh
{
    public virtual void Update() { }
}