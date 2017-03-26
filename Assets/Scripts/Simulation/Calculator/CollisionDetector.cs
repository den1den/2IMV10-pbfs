using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// axis-aligned bounding box hierarchy
/// </summary>
internal class CollisionDetector
{
    private ParticleModel pm;
    AABB root;

    public CollisionDetector(ParticleModel pm)
    {
        this.pm = pm;
        
        AABB[][] aabbs = new AABB[pm.getWidthHeight() - 1][];
        int index = 0;
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
                row[j] = new AABB(a, b, c, d, ref pm.positions);
            }
            aabbs[i] = row;
        }
        
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
                        row[j / 2] = new AABB(a, b, c, d);
                    }
                }
                newAABBs[i / 2] = row;
            }
            aabbs = newAABBs; //.Select(x => x.ToArray()).ToArray();
        }
        root = aabbs[0][0];
    }

    /// <summary>
    /// Sets the new pm.velocities
    /// </summary>
    internal void detectAndResolve()
    {
        //throw new NotImplementedException();
    }

    class AABB
    {
        Vector3 center;
        Vector3 halfSizes;
        AABB[] children;
        int[] internalPoints;

        public AABB(int a, int b, int c, int d, ref Vector3[] vertices)
        {
            children = new AABB[0];
            internalPoints = new int[] { a, b, c, d };
            
            float xmin = Math.Min(Math.Min(Math.Min(vertices[a].x, vertices[b].x), vertices[c].x), vertices[d].x);
            float xmax = Math.Max(Math.Max(Math.Max(vertices[a].x, vertices[b].x), vertices[c].x), vertices[d].x);
            float ymin = Math.Min(Math.Min(Math.Min(vertices[a].y, vertices[b].y), vertices[c].y), vertices[d].y);
            float ymax = Math.Max(Math.Max(Math.Max(vertices[a].y, vertices[b].y), vertices[c].y), vertices[d].y);
            float zmin = Math.Min(Math.Min(Math.Min(vertices[a].z, vertices[b].z), vertices[c].z), vertices[d].z);
            float zmax = Math.Max(Math.Max(Math.Max(vertices[a].z, vertices[b].z), vertices[c].z), vertices[d].z);
            halfSizes = new Vector3((xmax - xmin) / 2, (ymax - ymin) / 2, (zmax - zmin) / 2);
            center = new Vector3(xmin + halfSizes.x, ymin + halfSizes.y, zmin + halfSizes.z);
        }

        public AABB(AABB a, AABB b, AABB c, AABB d)
        {
            children = new AABB[] { a, b, c, d };
            internalPoints = new int[0];

            Vector3[] abcd;
            if (b == null)
            {
                abcd = join(a.center, a.halfSizes, d.center, d.halfSizes);
            }
            else if(d == null)
            {
                abcd = join(a.center, a.halfSizes, b.center, b.halfSizes);
            }
            else
            {
                Vector3[] ab = join(a.center, a.halfSizes, b.center, b.halfSizes);
                Vector3[] cd = join(c.center, c.halfSizes, d.center, d.halfSizes);
                abcd = join(ab[0], ab[1], cd[0], cd[1]);
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
        float xmin = Math.Min(centerA.x - halfSizesA.x, centerB.x - halfSizesB.x);
        float xmax = Math.Max(centerA.x + halfSizesA.x, centerB.x + halfSizesB.x);
        float ymin = Math.Min(centerA.y - halfSizesA.y, centerB.y - halfSizesB.y);
        float ymax = Math.Max(centerA.y + halfSizesA.y, centerB.y + halfSizesB.y);
        float zmin = Math.Min(centerA.z - halfSizesA.z, centerB.z - halfSizesB.z);
        float zmax = Math.Max(centerA.z + halfSizesA.z, centerB.z + halfSizesB.z);
        Vector3 newHalfSizes = new Vector3((xmax - xmin) / 2, (ymax - ymin) / 2, (zmax - zmin) / 2);
        Vector3 newCenter = new Vector3(xmin + newHalfSizes.x, ymin + newHalfSizes.y, zmin + newHalfSizes.z);
        return new Vector3[] { newCenter, newHalfSizes };
    }
}