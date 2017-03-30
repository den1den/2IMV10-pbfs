using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// axis-aligned bounding box hierarchy
/// </summary>
internal class CollisionDetector
{
    private ParticleModel pm;

    AABB[] tree;
    int leafs;

    public CollisionDetector(ParticleModel pm, Vector3 minPoint, Vector3 maxPoint)
    {
        this.pm = pm;
        
        // Create all AABBs
        AABB[][] aabbs = new AABB[pm.getWidthHeight() - 1][];
        List<AABB> tree = new List<AABB>();
        for (int i = 0; i < pm.getWidthHeight() - 1; i++)
        {
            AABB[] row = new AABB[pm.getWidthHeight() - 1];
            for (int j = 0; j < pm.getWidthHeight() - 1; j++)
            {
                // Add two triangles
                // d c
                // a b
                int a = i * pm.getWidthHeight() + j;
                int b = a + 1;
                int d = (i + 1) * pm.getWidthHeight() + j;
                int c = d + 1;
                AABB leaf = new AABB(a, b, c, d, ref pm.positions, tree.Count);
                row[j] = leaf;
                tree.Add(leaf);
            }
            aabbs[i] = row;
        }

        this.leafs = tree.Count;

        while (aabbs.Length > 1)
        {
            int newWh = (aabbs.Length + 1) / 2;
            AABB[][] newAABBs = new AABB[newWh][];
            for (int i = 0; i < aabbs.Length; i += 2)
            {
                AABB[] row = new AABB[newWh];
                for (int j = 0; j < aabbs[i].Length; j += 2)
                {
                    AABB a = aabbs[i][j];
                    AABB b = j + 1 < aabbs[i].Length ? aabbs[i][j + 1] : null;
                    AABB d = i + 1 < aabbs.Length ? aabbs[i + 1][j] : null;
                    AABB c = b != null && d != null ? aabbs[i + 1][j + 1] : null;

                    if (b == null && d == null)
                    {
                        row[j / 2] = a;
                    }
                    else
                    {
                        AABB aabb = new AABB(a, b, c, d, tree.Count);
                        row[j / 2] = aabb;
                        tree.Add(aabb);
                    }
                }
                newAABBs[i / 2] = row;
            }
            aabbs = newAABBs;
        }
        this.tree = tree.ToArray();

        AABB root = this.root();
        root.level = 0;
        int maxDepth = root.updateLevel();

        // Create all GridCells
        Vector3 currentMin = minPoint;
        Vector3 currentMax = maxPoint;
        Vector3 center = 0.5f * (currentMin + currentMax);

        Grid g = Grid.construct(currentMin, currentMax, maxDepth);
    }



    private AABB root()
    {
        return tree[tree.Length - 1];
    }

    /// <summary>
    /// Sets the new pm.velocities
    /// </summary>
    internal void detectAndResolve()
    {
        // Update points
        updatePositions();

        // Detect collision
    }

    private void updatePositions()
    {
        Vector3[] tmp = new Vector3[2];
        Vector3[] tmp2 = new Vector3[2];
        // Tree can be calculated from left to right because of the construction
        for (int i = 0; i < leafs; i++)
        {
            // first all leafes
            AABB leaf = tree[i];
            calc(leaf.internalPoints[0], leaf.internalPoints[1], leaf.internalPoints[2], leaf.internalPoints[3], ref pm.positions, ref tmp);
            leaf.center = tmp[0];
            leaf.halfSizes = tmp[1];
        }
        for (int i = leafs; i < tree.Length; i++)
        {
            AABB node = tree[i];
            if(node.type == Type.FULL)
            {
                joinTmp(node.children[0].center, node.children[0].halfSizes, node.children[1].center, node.children[1].halfSizes, ref tmp);
                joinTmp(node.children[2].center, node.children[2].halfSizes, node.children[3].center, node.children[3].halfSizes, ref tmp2);
                joinTmp(tmp[0], tmp[1], tmp2[0], tmp2[1], ref tmp);
                node.center = tmp[0];
                node.halfSizes = tmp[1];
            }
            else if (node.type == Type.LOWER)
            {
                joinTmp(node.children[0].center, node.children[0].halfSizes, node.children[1].center, node.children[1].halfSizes, ref tmp);
                node.center = tmp[0];
                node.halfSizes = tmp[1];
            }
            else if (node.type == Type.LEFT)
            {
                joinTmp(node.children[0].center, node.children[0].halfSizes, node.children[3].center, node.children[3].halfSizes, ref tmp);
                node.center = tmp[0];
                node.halfSizes = tmp[1];
            }
            else
            {
                throw new Exception();
            }
        }

    }


    internal static void calc(int a, int b, int c, int d, ref Vector3[] vertices, ref Vector3[] result)
    {
        float xmin = Math.Min(Math.Min(Math.Min(vertices[a].x, vertices[b].x), vertices[c].x), vertices[d].x);
        float xmax = Math.Max(Math.Max(Math.Max(vertices[a].x, vertices[b].x), vertices[c].x), vertices[d].x);
        float ymin = Math.Min(Math.Min(Math.Min(vertices[a].y, vertices[b].y), vertices[c].y), vertices[d].y);
        float ymax = Math.Max(Math.Max(Math.Max(vertices[a].y, vertices[b].y), vertices[c].y), vertices[d].y);
        float zmin = Math.Min(Math.Min(Math.Min(vertices[a].z, vertices[b].z), vertices[c].z), vertices[d].z);
        float zmax = Math.Max(Math.Max(Math.Max(vertices[a].z, vertices[b].z), vertices[c].z), vertices[d].z);
        Vector3 halfSizes = new Vector3((xmax - xmin) / 2, (ymax - ymin) / 2, (zmax - zmin) / 2);
        Vector3 center = new Vector3(xmin + halfSizes.x, ymin + halfSizes.y, zmin + halfSizes.z);
        result[0] = center;
        result[1] = halfSizes;
    }

    private enum Type { FULL, LOWER, LEFT, LEAF };

    class AABB
    {
        public Vector3 center;
        public Vector3 halfSizes;
        public AABB parent;
        public AABB[] children;
        public int[] internalPoints;
        public Type type;
        public int level = -1;
        public int index;

        public AABB(int a, int b, int c, int d, ref Vector3[] vertices, int index)
        {
            this.index = index;
            children = new AABB[0];
            internalPoints = new int[] { a, b, c, d };
            Vector3[] abcd = new Vector3[2];
            calc(a, b, c, d, ref vertices, ref abcd);
            center = abcd[0];
            halfSizes = abcd[1];
            type = Type.LEAF;
        }

        public int updateLevel()
        {
            int maxDepth = level;
            foreach(AABB child in children)
            {
                child.level = level + 1;
                child.updateLevel();
                if(child.level > maxDepth)
                {
                    maxDepth = child.level;
                }
            }
            return maxDepth;
        }

        public AABB(AABB a, AABB b, AABB c, AABB d, int index)
        {
            this.index = index;
            internalPoints = new int[0];

            Vector3[] abcd;
            if (b == null)
            {
                type = Type.LEFT;
                abcd = join(a.center, a.halfSizes, d.center, d.halfSizes);
                children = new AABB[] { a, d };
                a.parent = this;
                d.parent = this;
            }
            else if(d == null)
            {
                type = Type.LOWER;
                abcd = join(a.center, a.halfSizes, b.center, b.halfSizes);
                children = new AABB[] { a, b };
                a.parent = this;
                b.parent = this;
            }
            else
            {
                type = Type.FULL;
                Vector3[] ab = join(a.center, a.halfSizes, b.center, b.halfSizes);
                Vector3[] cd = join(c.center, c.halfSizes, d.center, d.halfSizes);
                abcd = join(ab[0], ab[1], cd[0], cd[1]);
                children = new AABB[] { a, b, c, d };
                a.parent = this;
                b.parent = this;
                c.parent = this;
                d.parent = this;
            }
            center = abcd[0];
            halfSizes = abcd[1];
        }

        bool overlaps(AABB other)
        {
            return ! ( (Math.Abs(center.x - other.center.x) > halfSizes.x + other.halfSizes.x)
                & (Math.Abs(center.y - other.center.y) > halfSizes.y + other.halfSizes.y)
                & (Math.Abs(center.z - other.center.z) > halfSizes.z + other.halfSizes.z) );
        }
        
    }

    static Vector3[] join(Vector3 centerA, Vector3 halfSizesA, Vector3 centerB, Vector3 halfSizesB)
    {
        Vector3[] tmp = new Vector3[2];
        joinTmp(centerA, halfSizesA, centerB, halfSizesB, ref tmp);
        return tmp;
    }

    static void joinTmp(Vector3 centerA, Vector3 halfSizesA, Vector3 centerB, Vector3 halfSizesB, ref Vector3[] tmp)
    {
        float xmin = Math.Min(centerA.x - halfSizesA.x, centerB.x - halfSizesB.x);
        float xmax = Math.Max(centerA.x + halfSizesA.x, centerB.x + halfSizesB.x);
        float ymin = Math.Min(centerA.y - halfSizesA.y, centerB.y - halfSizesB.y);
        float ymax = Math.Max(centerA.y + halfSizesA.y, centerB.y + halfSizesB.y);
        float zmin = Math.Min(centerA.z - halfSizesA.z, centerB.z - halfSizesB.z);
        float zmax = Math.Max(centerA.z + halfSizesA.z, centerB.z + halfSizesB.z);
        Vector3 newHalfSizes = new Vector3((xmax - xmin) / 2, (ymax - ymin) / 2, (zmax - zmin) / 2);
        tmp[0] = newHalfSizes;
        tmp[1] = new Vector3(xmin + newHalfSizes.x, ymin + newHalfSizes.y, zmin + newHalfSizes.z);
    }
    
    internal class Grid
    {
        GridCell[][] levels;

        private Grid(int maxDepth)
        {
            levels = new GridCell[maxDepth][];
        }

        internal static Grid construct(Vector3 currentMin, Vector3 currentMax, int maxDepth)
        {
            Grid grid = new Grid(maxDepth);
            constructR(currentMin, currentMax, 0, grid);
            return grid;
        }

        internal static void constructR(Vector3 currentMin, Vector3 currentMax, int depth, Grid grid)
        {
            if(depth == 0)
            {
                GridCell root = null;
                grid.levels[0] = new GridCell[]{ root };
                constructR(currentMin, currentMax, depth + 1, grid);
            }
            // Assume parent is set
            GridCell[] parents = grid.levels[depth - 1];
            GridCell[] thisChildren = new GridCell[parents.Length * 8];
            int count = (int)Math.Pow(2, depth);

            Vector3 center = 0.5f * (currentMax - currentMin);
            Vector3 d = center - currentMin;

            for (int i = 0; i < parents.Length; i++)
            {
                GridCell parent = parents[i];

                // Make 8 children per parent
                int nextDepth = depth + 1;

                GridCell cell;
                cell = new GridCell(parent.i * count + 0, parent.j * count + 0, parent.k * count + 0, depth);
                constructR(currentMin + new Vector3(0, 0, 0), center + new Vector3(0, 0, 0), nextDepth, grid);
                cell = new GridCell(parent.i * count + 1, parent.j * count + 0, parent.k * count + 0, depth);
                constructR(currentMin + new Vector3(d.x, 0, 0), center + new Vector3(d.x, 0, 0), nextDepth, grid);
                cell = new GridCell(parent.i * count + 0, parent.j * count + 1, parent.k * count + 0, depth);
                constructR(currentMin + new Vector3(0, d.y, 0), center + new Vector3(0, d.y, 0), nextDepth, grid);
                cell = new GridCell(parent.i * count + 1, parent.j * count + 1, parent.k * count + 0, depth);
                constructR(currentMin + new Vector3(d.x, d.y, 0), center + new Vector3(d.x, d.y, 0), nextDepth, grid);
                cell = new GridCell(parent.i * count + 0, parent.j * count + 0, parent.k * count + 1, depth);
                constructR(currentMin + new Vector3(0, 0, d.z), center + new Vector3(0, 0, d.z), nextDepth, grid);
                cell = new GridCell(parent.i * count + 1, parent.j * count + 0, parent.k * count + 1, depth);
                constructR(currentMin + new Vector3(d.x, 0, d.z), center + new Vector3(d.x, 0, d.z), nextDepth, grid);
                cell = new GridCell(parent.i * count + 0, parent.j * count + 1, parent.k * count + 1, depth);
                constructR(currentMin + new Vector3(0, d.y, d.z), center + new Vector3(0, d.y, d.z), nextDepth, grid);
                cell = new GridCell(parent.i * count + 1, parent.j * count + 1, parent.k * count + 1, depth);
                constructR(currentMin + new Vector3(d.x, d.y, d.z), center + new Vector3(d.x, d.y, d.z), nextDepth, grid);
            }
            
        }

        private int getMaxDepth()
        {
            return levels.Length;
        }
    }

    internal class GridCell
    {
        internal int i, j, k, depth;
        internal List<int> inCell = new List<int>(); // AABB indices in this cell
        internal GridCell(int i, int j, int k, int depth)
        {
            this.i = i;
            this.j = j;
            this.k = k;
            this.depth = depth;
        }

        int getIndex()
        {
            return k * depth * depth + j * depth + i;
        }
    }
}
