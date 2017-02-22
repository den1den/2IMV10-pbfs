using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Simulation.EnergyFunctions
{
    public class FEMFunction : EnergyFunction
    {
        const float EPS = 1e-6f;

        ParticleModel pm;
        int i0, i1, i2, i3;
        float volume;
        Matrix4x4 invRestMat;
        public FEMFunction(ParticleModel pm, int i0, int i1, int i2, int i3, float volume, Matrix4x4 invRestMat)
        {
            this.pm = pm;
            this.i0 = i0;
            this.i1 = i1;
            this.i2 = i2;
            this.i3 = i3;
            this.volume = volume;
            this.invRestMat = invRestMat;
        }
        public Vector3 d0, d1, d2, d3;

        /// <summary>Create FEMFunction (finite element method)</summary>
        /// <param name="pm">The actual ParticleModel</param>
        /// <param name="i0">Index of 0 particle of tetrahedron</param>
        /// <param name="i1">Index of 1 particle of tetrahedron</param>
        /// <param name="i2">Index of 2 particle of tetrahedron</param>
        /// <param name="i3">Index of 3 particle of tetrahedron</param>
        public static FEMFunction create(ParticleModel pm, int i0, int i1, int i2, int i3)
        {
            Vector3 p0 = pm.positions[i0];
            Vector3 p1 = pm.positions[i1];
            Vector3 p2 = pm.positions[i2];
            Vector3 p3 = pm.positions[i3];

            Matrix4x4 m = Util.createM(p0 - p3, p1 - p3, p2 - p3);
            float det = m.determinant;

            if(Math.Abs(det) > EPS)
            {
                Matrix4x4 invRestMat = m.inverse;

                float volume = calcVolume(p0, p1, p2, p3);

                return new FEMFunction(pm, i0, i1, i2, i3, volume, invRestMat);
            }
            return null;
        }

        private static float calcVolume(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float volume = Math.Abs(1f / 6 * Vector3.Dot(p3 - p0, Vector3.Cross(p2 - p0, p1 - p0)));
            Debug.Log("Thetrahedon (" + p0 + ", " + p1 + ", " + p2 + ", " + p3 + ")");
            Debug.Log("Has volume " + volume + "\n");
            return volume;
        }

        public void solve()
        {
            d0 = Vector3.zero;
            d1 = Vector3.zero;
            d2 = Vector3.zero;
            d3 = Vector3.zero;
            // TODO
        }
    }
}
