#define ALL_TRIANGLES

using Assets.Scripts;
using Assets.Scripts.Tools;
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

    private const bool showGridPoints = false;
    private MonoManager<SceneObject> gridPointsObjectManager;

    public int bezier = 1;

    public TriangularModelMesh(ParticleModel model, ClothSimulation settings)
    {
        this.model = model;

        GameObject meshGameObject = new GameObject("TriangularModelMesh");

        MeshFilter meshFilter = meshGameObject.AddComponent<MeshFilter>();
        this.mesh = meshFilter.mesh;

        MeshRenderer meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
        Material defaultMaterial = new Material(Shader.Find("VR/SpatialMapping/Wireframe"));
        meshRenderer.sharedMaterial = defaultMaterial;

        this.mesh.Clear();

        int subdivisions = 6;
        int modelWH = this.model.getWidthHeight();

        points = new Vector3[(int)Math.Pow((modelWH + (modelWH - 1)) * subdivisions, 2)];
        Array.Copy(model.positions, points, model.positions.Length);
        int index = model.positions.Length;

        List<int> triangles = new List<int>();
        
        List<SubLine> SubLines = new List<SubLine>();
        // horizontal
        for (int j = 0; j < modelWH; j++)
        {
            for (int i = 0; i < modelWH - 1; i++)
            {
                int left = j * modelWH + i;
                int right = left + 1;

                SubLine subLine = bezier > 0 ? new BezierSubline(left, right, index, index + subdivisions) : new SubLine(left, right, index, index + subdivisions);
                index = index + subdivisions;

                if (i > 0)
                {
                    SubLine prev = SubLines[(modelWH - 1) * j + (i - 1)];
                    subLine.link(ref prev);
                }

                SubLines.Add(subLine);
            }
        }
        // vertical
        for (int j = 0; j < modelWH - 1; j++)
        {
            for (int i = 0; i < modelWH; i++)
            {
                int bottom = j * modelWH + i;
                int top = bottom + modelWH;

                SubLine subLine = bezier > 0 ? new BezierSubline(bottom, top, index, index + subdivisions) : new SubLine(bottom, top, index, index + subdivisions);
                index = index + subdivisions;

                if(j > 0)
                {
                    SubLine prev = SubLines[modelWH * (modelWH - 1) + modelWH * (j - 1) + i];
                    subLine.link(ref prev);
                }

                SubLines.Add(subLine);
            }
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

                if(i > 0)
                {
                    SubGrid prev = SubGrids[(modelWH - 1) * j + i - 1];
                    grid.linkHorizontal(ref prev);
                }
                if(j > 0)
                {
                    SubGrid prev = SubGrids[(modelWH - 1) * (j - 1) + i];
                    grid.linkVertical(ref prev);
                }

                SubGrids.Add(grid);

                // Add triangles for the grid
                triangles.AddRange(grid.GenTriangles());
            }

        List<int> included = new List<int>(new HashSet<int>(triangles));

        // Calculate the uvs
        Vector2[] uvs = new Vector2[points.Length]; // uv maps texture to points
        float du = 1.0f / modelWH;
        float dv = 1.0f / modelWH;
        for (int u = 0; u < modelWH; u++)
            for (int v = 0; v < modelWH; v++)
            {
                uvs[u * modelWH + v] = new Vector2(u * du, v * dv);
            }

        if (showGridPoints)
        {
            gridPointsObjectManager = new MonoManager<SceneObject>();
            GameObject unitPrefab = new GameObject();
            unitPrefab.AddComponent<SceneObject>();
            unitPrefab.AddComponent<MeshFilter>();
            unitPrefab.AddComponent<MeshRenderer>();
            gridPointsObjectManager.OverrideGameObject(unitPrefab);

            Mesh mesh = Assets.Scripts.Tools.Geometry.PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Sphere);
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            material.color = Color.yellow;


            for (int i = model.positions.Length; i < points.Length; i++)
            {
                var newObj = gridPointsObjectManager.New();
                newObj.Init(mesh, material);
                newObj.transform.position = points[i];
            }
        }

        SubLines.ForEach(sl => {
            sl.setUV(ref uvs);
        });
        this.subLines = SubLines.ToArray();
        SubGrids.ForEach(sl => {
            sl.setUV(ref uvs);
        });
        this.subGrids = SubGrids.ToArray();

        SubLines.ForEach(sl => {
            sl.setPoints(ref points);
        });
        SubGrids.ForEach(sl => {
            sl.setPoints(ref points);
        });

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

        if (showGridPoints)
        {
            List<SceneObject> gridPointsObjects = new List<SceneObject>(gridPointsObjectManager.GetAll());
            for (int i = model.positions.Length; i < gridPointsObjects.Count; i++)
            {
                gridPointsObjects[i].transform.position = points[i];
            }
        }
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

    class BezierSubline : SubLine
    {
        Bezier prevB = null;
        Bezier nextB = null;
        public BezierSubline(int a, int b, int startIndex, int endIndex) : base(a, b, startIndex, endIndex)
        {
        }

        /// <summary>
        /// Every bezier curve is liked to its previous and current curve:
        /// _  _  _  _
        ///  c1 c2 c3
        /// </summary>
        /// <param name="prev"></param>
        internal override void link(ref SubLine prev)
        {
            BezierSubline bPrev = (BezierSubline)prev;
            base.link(ref prev);
            Bezier prevB = new Bezier(new int[] { prev.a, a, b }, endIndex - startIndex);
            this.prevB = prevB;
            bPrev.nextB = prevB;
        }

        public override void setPoints(ref Vector3[] writePoints, ref Vector3[] readPoints)
        {
            int L = endIndex - startIndex; // number of intermediate points
            int bezierHalfIndex = L + 2 - 1;
            Vector3 a, b, point;
            
            if (prevB == null || nextB == null)
            {
                // Get functiom from single bezier
                Bezier bezier;
                int bI;
                if (prevB != null)
                {
                    // prev bezier has to start halfway
                    bezier = prevB;
                    bI = bezierHalfIndex;
                }
                else
                {
                    // current bezier starts at i=0
                    bezier = nextB;
                    bI = 0;
                }

                // Write bezier to points
                a = bezier.point(bI++, ref readPoints);
                writePoints[base.a] = a; // control point
                for (int i = 0; i < L; i++)
                {
                    // calculate intermediate bezier points
                    point = bezier.point(bI++, ref readPoints);
                    writePoints[i + startIndex] = point;
                }
                b = bezier.point(bI++, ref readPoints);
                writePoints[base.b] = b; // TODO: this control point is updated multiple times (this and next)
            }
            else
            {
                // Two beziers in this segment
                // Same as 1 but interpolated
                a = prevB.point(bezierHalfIndex, ref readPoints);
                writePoints[base.a] = a;

                float t = 0;
                float dt = 1.0f / (L + 1);
                for (int i = 0; i < L; i++)
                {
                    a = prevB.point(bezierHalfIndex + 1 + i, ref readPoints);
                    b = nextB.point(i + 1, ref readPoints);

                    float sint = (float) Math.Sin(Math.PI * t / 2);
                    point = a * (1 - sint) + b * sint; // interpolate between the two bezier curves
                    writePoints[startIndex + i] = point;
                    t += dt;
                }

                b = nextB.point(bezierHalfIndex, ref readPoints);
                writePoints[base.b] = b;
            }

            return;

            if (prevB == null || nextB == null)
            {
                Bezier bezier;
                int bI;
                if (prevB != null)
                {
                    // prev bezier has to start halfway
                    bezier = prevB;
                    bI = bezierHalfIndex;
                }
                else
                {
                    bezier = nextB;
                    bI = 0;
                }

                // Write bezier to points
                a = bezier.point(bI++, ref readPoints);
                writePoints[base.a] = a;
                for (int i = 0; i < L; i++)
                {
                    point = bezier.point(bI++, ref readPoints);
                    writePoints[i + startIndex] = point;
                }
                b = bezier.point(bI++, ref readPoints);
                writePoints[base.b] = b;
            } else
            {
                a = prevB.point(bezierHalfIndex, ref readPoints);
                writePoints[base.a] = a;
                float t = 0;
                float dt = 1.0f / (L + 1);
                for (int i = 0; i < L; i++)
                {
                    a = prevB.point(bezierHalfIndex + i, ref readPoints);
                    b = nextB.point(i, ref readPoints);
                    point = a * (1 - t) + b * t;
                    writePoints[startIndex + i] = point;
                    t += dt;
                }
                b = nextB.point(bezierHalfIndex, ref readPoints);
                writePoints[base.b] = b;
            }
        }
    }

    class SubLine
    {
        public int a, b;
        public int startIndex;
        public int endIndex;
        private float dt;
        private SubLine prev = null;
        private SubLine next = null;
        public SubLine(int a, int b, int startIndex, int endIndex)
        {
            this.a = a; this.b = b;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.dt = 1.0f / (this.endIndex - this.startIndex + 1);
        }

        public void setPointsBilinear(ref Vector3[] points)
        {
            float fa = 1;
            float fb = 0;
            for (int i = this.startIndex; i < this.endIndex; i++)
            {
                fa -= dt;
                fb += dt;
                points[i] = fa * points[a] + fb * points[b];
            }
        }

        public virtual void setPoints(ref Vector3[] writePoints, ref Vector3[] readPoints)
        {
            setPointsBilinear(ref writePoints);
        }

        public void setUV(ref Vector2[] uvs)
        {
            for (int i = this.startIndex; i < this.endIndex; i++)
            {
                uvs[i] = (uvs[a] + uvs[b]) * 0.5f;
            }
        }

        internal virtual void link(ref SubLine prev)
        {
            this.prev = prev;
            prev.next = this;
            Debug.Assert(prev.b == this.a);
        }

        internal void setPoints(ref Vector3[] points)
        {
            Vector3[] pointsCopy = new Vector3[points.Length];
            Array.Copy(points, pointsCopy, points.Length);
            setPoints(ref points, ref pointsCopy);
        }
    }

    class SubGrid
    {
        SubLine bottom; SubLine right; SubLine left; SubLine top;
        int startIndex;
        int w;
        int h;
        SubGrid gBottom = null;
        SubGrid gRight = null;
        SubGrid gLeft = null;
        SubGrid gTop = null;
        private float dtw, dth;
        public SubGrid(ref SubLine b, ref SubLine r, ref SubLine l, ref SubLine t, int startIndex, int w, int h)
        {
            this.bottom = b; this.right = r; this.left = l; this.top = t;
            this.startIndex = startIndex;
            this.w = w;
            this.h = h;
            dtw = 1.0f / (this.w + 1);
            dth = 1.0f / (this.h + 1);
        }

        public void setPointsBilinear(ref Vector3[] points)
        {
            float fw0 = 1;
            float fw1 = 0;
            for (int i = 0; i < this.w; i++)
            {
                fw0 -= dtw;
                fw1 += dtw;
                float fh0 = 1;
                float fh1 = 0;
                for (int j = 0; j < this.h; j++)
                {
                    fh0 -= dth;
                    fh1 += dth;
                    points[this.startIndex + j * this.w + i] =
                        points[left.a] * fw0 * fh0 + points[left.b] * fw0 * fh1 + points[right.a] * fw1 * fh0 + points[right.b] * fw1 * fh1;
                }
            }
        }

        public void setPoints(ref Vector3[] points)
        {
            setPointsBilinear(ref points);
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

            // bottom corner start
            result.Add(bottom.a);
            result.Add(bottom.startIndex);
            result.Add(this.startIndex);

#if ALL_TRIANGLES
            // bottom line
            addBorderTriangles(this.startIndex, 1, ref bottom, ref result);

            // bottom corner end
            result.Add(bottom.endIndex - 1);
            result.Add(bottom.b);
            result.Add(this.startIndex + this.w - 1);

            //right
            result.Add(right.a);
            result.Add(right.startIndex);
            result.Add(this.startIndex + this.w - 1);
            addBorderTriangles(this.startIndex + this.w - 1, this.w, ref right, ref result);
            result.Add(right.endIndex - 1);
            result.Add(right.b);
            result.Add(this.startIndex + this.w * this.h - 1);

            // top
            result.Add(top.b);
            result.Add(top.endIndex - 1);
            result.Add(this.startIndex + this.w * this.h - 1);
            addBorderTriangles(this.startIndex + this.w * (this.h - 1), 1, ref top, ref result);
            result.Add(top.startIndex);
            result.Add(top.a);
            result.Add(this.startIndex + (this.h - 1) * this.w);

            //left
            result.Add(left.b);
            result.Add(left.endIndex - 1);
            result.Add(this.startIndex + (this.h - 1) * this.w);
            addBorderTriangles(this.startIndex, this.w, ref left, ref result);
            result.Add(left.startIndex);
            result.Add(left.a);
            result.Add(this.startIndex);

            //inner
            addInnerTriangles(ref result);
#endif

            return result;
        }

        private void addBorderTriangles(int thisStart, int thisIncr, ref SubLine line, ref List<int> triangles)
        {
            for (int i = 0; i < line.endIndex - line.startIndex - 1; i++)
            {
                int gridA = thisStart + thisIncr * i;
                int gridB = thisStart + thisIncr * (i + 1);
                int lineA = line.startIndex + i;
                int lineB = lineA + 1;

                triangles.Add(lineA);
                triangles.Add(lineB);
                triangles.Add(gridB);

                triangles.Add(lineA);
                triangles.Add(gridB);
                triangles.Add(gridA);
            }
        }

        private void addInnerTriangles(ref List<int> triangles)
        {
            for (int i = 0; i < this.h - 1; i++)
                for (int j = 0; j < this.w - 1; j++)
                {
                    int gridA = this.startIndex + i * this.w + j;
                    int gridB = gridA + 1;
                    int gridD = gridA + this.w;
                    int gridC = gridD + 1;
                    
                    triangles.Add(gridA);
                    triangles.Add(gridB);
                    triangles.Add(gridC);

                    triangles.Add(gridA);
                    triangles.Add(gridC);
                    triangles.Add(gridD);
                }
        }

        internal void linkHorizontal(ref SubGrid prev)
        {
            this.gLeft = prev;
            prev.gRight = this;
        }

        internal void linkVertical(ref SubGrid prev)
        {
            this.gBottom = prev;
            prev.gTop = this;
        }
    }
}
public abstract class TriangularMesh
{
    public virtual void Update() { }
}
public class Bezier
{
    int[] points;
    float[][] factors;
    public Bezier(int[] points, int subPoints)
    {
        this.points = points;
        int totalPoints = (points.Length - 1) * (subPoints + 1) + 1;
        int N = points.Length - 1;
        factors = new float[totalPoints][];
        float dt = 1.0f / (totalPoints - 1);

        for(int p = 0; p < factors.Length; p++)
        {
            factors[p] = new float[points.Length];
            float t = p * dt;
            
            for(int i = 0; i < points.Length; i++)
            {
                long t_c = combination(N, i);
                double t_p = Math.Pow(1 - t, N - i);
                double t_n = Math.Pow(t, i);
                
                float f = (float)(t_c * t_p * t_n);
                factors[p][i] = f;
            }
        }
    }

    public Vector3 point(int pi, ref Vector3[] points)
    {
        Vector3 p = new Vector3();
        float[] factors = this.factors[pi];
        for(int i = 0; i < factors.Length; i++)
        {
            p += points[this.points[i]] * factors[i];
        }
        return p;
    }

    public static long combination(long n, long k)
    {
        double sum = 0;
        for (long i = 0; i < k; i++)
        {
            sum += Math.Log10(n - i);
            sum -= Math.Log10(i + 1);
        }
        return (long)Math.Pow(10, sum);
    }

}