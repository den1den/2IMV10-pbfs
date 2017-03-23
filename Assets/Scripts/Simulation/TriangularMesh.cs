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

        // Create a square grid
        Vector3[] points = new Vector3[model.positions.Length]; // points on which to render cloth
        Array.Copy(model.positions, points, points.Length);
        this.mesh.vertices = points;

        // Calculate the uvs
        Vector2[] uv = new Vector2[model.positions.Length]; // uv maps texture to points
        int width = (int)Math.Sqrt(uv.Length);
        Debug.Assert(Math.Sqrt(uv.Length) == width, "Assert model poistions is exactly squared");
        float du = 1.0f / width;
        float dv = du;
        for (int u = 0; u < width; u++)
            for (int v = 0; v < width; v++)
            {
                uv[u * width + v] = new Vector2(u * du, v * dv);
            }
        this.mesh.uv = uv;

        Util.Triangle[] triangles = this.model.triangles; // maps points to triangles
        this.mesh.triangles = Util.TrianglesToIndexArray(triangles, true);
    }

    public override Vector3[] getMainPoints()
    {
        return model.positions;
    }

    public override Vector3[] getSubPoints()
    {
        //TODO store/track subparticlepoints
        return new Vector3[0];
    }

    public override void Update()
    {
        // No calculations needed on subparticles
        // Update positions
        Vector3[] vertices = this.mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = this.model.positions[i];
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        // triangles remains the same
        // uv remains the same
    }
}
public abstract class TriangularMesh
{
    public abstract Vector3[] getMainPoints();
    public virtual Vector3[] getSubPoints() { return new Vector3[0];  }
    public virtual void Update() { }
}