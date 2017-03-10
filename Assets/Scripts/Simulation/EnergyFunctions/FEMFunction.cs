using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Simulation.EnergyFunctions
{
    /// <summary>
    /// FEM Function for a triangle
    /// </summary>
    public class FEMFunction : EnergyFunction
    {
        const float EPS = 1e-6f;

        ParticleModel pm;
        int i0, i1, i2;
        float volume;
        Matrix4x4 invRestMat;
        private FEMFunction(ParticleModel pm, int i0, int i1, int i2, float volume, Matrix4x4 invRestMat)
        {
            this.pm = pm;
            this.i0 = i0;
            this.i1 = i1;
            this.i2 = i2;
            this.volume = volume;
            this.invRestMat = invRestMat;
        }
        public Vector3 d0, d1, d2, d3;

        /// <summary>Create FEMFunction for a triangle (finite element method)</summary>
        /// <param name="pm">The actual ParticleModel</param>
        /// <param name="i0">Index of 0 particle of triangle</param>
        /// <param name="i1">Index of 1 particle of triangle</param>
        /// <param name="i2">Index of 2 particle of triangle</param>
        public static FEMFunction create(ParticleModel pm, int i0, int i1, int i2)
        {
            Vector3 p0 = pm.positions[i0];
            Vector3 p1 = pm.positions[i1];
            Vector3 p2 = pm.positions[i2];

            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            Vector3 axis01 = (p2 - p0).normalized;
            Vector3 axis02 = Vector3.Cross(normal, axis01).normalized;

            // Transform vectors to new cooridinate system
            Vector2 t0 = new Vector2(Vector3.Dot(p0, axis01), Vector3.Dot(p0, axis02));
            Vector2 t1 = new Vector2(Vector3.Dot(p1, axis01), Vector3.Dot(p1, axis02));
            Vector2 t2 = new Vector2(Vector3.Dot(p2, axis01), Vector3.Dot(p2, axis02));

            Matrix4x4 m = new Matrix4x4();
            m[0, 0] = t0.y - t2.y;
            m[1, 0] = t0.x - t2.x;
            m[0, 1] = t1.y - t2.y;
            m[1, 1] = t1.x - t2.x;
            m[2, 2] = 1;
            m[3, 3] = 1;
            float det = m.determinant;

            if(Math.Abs(det) > EPS)
            {
                Matrix4x4 invRestMat = m.inverse;
                // Calculate area
                float area = Math.Abs(normal.magnitude) * 0.5f;
                Debug.Log("Triangle (" + p0 + ", " + p1 + ", " + p2 + ")");
                Debug.Log("Has area " + area + "\n");
                return new FEMFunction(pm, i0, i1, i2, area, invRestMat);
            }
            // determinant ~= 0
            return null;
        }

        public void solve()
        {
            d0 = Vector3.zero;
            d1 = Vector3.zero;
            d2 = Vector3.zero;
            d3 = Vector3.zero;
            // TODO: https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/PositionBasedDynamics/PositionBasedDynamics.cpp#L882
        }
    }
}

