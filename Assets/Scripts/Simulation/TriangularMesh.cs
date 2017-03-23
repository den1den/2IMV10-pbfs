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
    private SubLine[] subLines;
    private SubGrid[] subGrids;

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

        int subdivisions = 1;
        int modelWH = this.model.getWidthHeight();

        points = new Vector3[(int)Math.Pow((modelWH + (modelWH - 1)) * subdivisions, 2)];
        Array.Copy(model.positions, points, model.positions.Length);
        int index = model.positions.Length;

        List<int> triangles = new List<int>();
        
        List<SubLine> SubLines = new List<SubLine>();
        for (int j = 0; j < modelWH; j++) // horizontal
            for (int i = 0; i < modelWH - 1; i++)
            {
                int left = j * modelWH + i;
                int right = left + 1;
                SubLines.Add(new SubLine(left, right, index, index + subdivisions));
                index = index + subdivisions;
            }
        for (int j = 0; j < modelWH - 1; j++) // vertical
            for (int i = 0; i < modelWH; i++)
            {
                int bottom = j * modelWH + i;
                int top = bottom + modelWH;
                SubLines.Add(new SubLine(bottom, top, index, index + subdivisions));
                index = index + subdivisions;
            }
        List<SubGrid> SubGrids = new List<SubGrid>();
        for (int j = 0; j < modelWH - 1; j++)
            for (int i = 0; i < modelWH - 1; i++)
            {
                int a = modelWH * j + i;
                int b = a + 1;
                int c = a + modelWH;
                int d = c + 1;

                SubLine bottom = SubLines[(modelWH - 1) * j + i];
                SubLine top = SubLines[(modelWH - 1) * (j + 1) + i];
                SubLine left = SubLines[(modelWH - 1) * modelWH + modelWH * j + i];
                SubLine right = SubLines[(modelWH - 1) * modelWH + modelWH * j + i + 1];

                SubGrid grid = new SubGrid(ref bottom, ref right, ref left, ref top, index, subdivisions, subdivisions);
                index += subdivisions * subdivisions;
                SubGrids.Add(grid);

                // Add triangles for the grid
                triangles.AddRange(grid.GenTriangles());
            }

        // Calculate the uvs
        Vector2[] uvs = new Vector2[points.Length]; // uv maps texture to points
        float du = 1.0f / modelWH;
        float dv = 1.0f / modelWH;
        for (int u = 0; u < modelWH; u++)
            for (int v = 0; v < modelWH; v++)
            {
                uvs[u * modelWH + v] = new Vector2(u * du, v * dv);
            }

        SubLines.ForEach(sl => {
            sl.setPoints(ref points);
            sl.setUV(ref uvs);
        });
        this.subLines = SubLines.ToArray();
        SubGrids.ForEach(sl => {
            sl.setPoints(ref points);
            sl.setUV(ref uvs);
        });
        this.subGrids = SubGrids.ToArray();

        // Set unit mesh
        this.mesh.vertices = points;
        this.mesh.uv = uvs;
        this.mesh.triangles = addBothSide(triangles.ToArray());
    }

    public override void Update()
    {
        points = this.mesh.vertices;
        // Update positions
        for (int i = 0; i < this.model.positions.Length; i++)
        {
            points[i] = this.model.positions[i];
        }
        for (int i = 0; i < this.subLines.Length; i++)
        {
            this.subLines[i].setPoints(ref points);
        }
        for (int i = 0; i < this.subGrids.Length; i++)
        {
            this.subGrids[i].setPoints(ref points);
        }
        // triangles remains the same
        // uv remains the same
        mesh.vertices = points;
        mesh.RecalculateBounds();
    }

    private static int[] addBothSide(int[] trianlges)
    {
        int[] r = new int[trianlges.Length * 2];
        for(int i = 0; i < trianlges.Length / 3; i++)
        {
            r[3 * i + 0] = trianlges[3 * i + 0];
            r[3 * i + 1] = trianlges[3 * i + 1];
            r[3 * i + 2] = trianlges[3 * i + 2];
            r[3 * i + trianlges.Length + 0] = trianlges[3 * i];
            r[3 * i + trianlges.Length + 1] = trianlges[3 * i + 2];
            r[3 * i + trianlges.Length + 2] = trianlges[3 * i + 1];
        }
        return r;
    }

    class SubLine
    {
        public int a, b;
        public int startIndex;
        public int endIndex;
        public SubLine(int a, int b, int startIndex, int endIndex)
        {
            this.a = a; this.b = b;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }

        public void setPoints(ref Vector3[] points)
        {
            for(int i = this.startIndex; i < this.endIndex; i++)
            {
                points[i] = (points[a] + points[b]) * 0.5f;
            }
        }

        public void setUV(ref Vector2[] uvs)
        {
            for (int i = this.startIndex; i < this.endIndex; i++)
            {
                uvs[i] = (uvs[a] + uvs[b]) * 0.5f;
            }
        }
    }

    struct SubGrid
    {
        SubLine bottom; SubLine right; SubLine left; SubLine top;
        int startIndex;
        int w;
        int h;
        public SubGrid(ref SubLine b, ref SubLine r, ref SubLine l, ref SubLine t, int startIndex, int w, int h)
        {
            this.bottom = b; this.right = r; this.left = l; this.top = t;
            this.startIndex = startIndex;
            this.w = w;
            this.h = h;
        }

        public void setPoints(ref Vector3[] points)
        {
            for (int i = 0; i < this.w; i++)
                for(int j = 0; j < this.h; j++)
                    {
                        points[this.startIndex + i * this.w + j] = (points[left.a] + points[left.b] + points[right.a] + points[right.b]) * 0.25f;
                    }
        }

        public void setUV(ref Vector2[] uvs)
        {
            for (int i = 0; i < this.w; i++)
                for (int j = 0; j < this.h; j++)
                {
                    uvs[this.startIndex + i * this.w + j] = (uvs[left.a] + uvs[left.b] + uvs[right.a] + uvs[right.b]) * 0.25f;
                }
        }

        /// <summary>
        /// A submesh of 8 triangles between 4 points:
        /// [/][/]
        /// [/][/]
        /// </summary>
        internal List<int> GenTriangles()
        {
            List<int> result = new List<int>();

            // bottom
            result.Add(bottom.a);
            result.Add(bottom.startIndex);
            result.Add(this.startIndex);

            result.Add(bottom.endIndex - 1);
            result.Add(bottom.b);
            result.Add(this.startIndex + this.w - 1);

            //right
            result.Add(right.a);
            result.Add(right.startIndex);
            result.Add(this.startIndex + this.w - 1);

            result.Add(right.endIndex - 1);
            result.Add(right.b);
            result.Add(this.startIndex + this.w * this.h - 1);

            // top
            result.Add(top.b);
            result.Add(top.endIndex - 1);
            result.Add(this.startIndex + this.w * this.h - 1);

            result.Add(top.startIndex);
            result.Add(top.a);
            result.Add(this.startIndex + (this.h - 1) * this.w);

            //left
            result.Add(left.b);
            result.Add(left.endIndex - 1);
            result.Add(this.startIndex + (this.h - 1) * this.w);

            result.Add(left.startIndex);
            result.Add(left.a);
            result.Add(this.startIndex);

            return result;
        }
    }
}
public abstract class TriangularMesh
{
    public virtual void Update() { }
}