using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Simulation.EnergyFunctions
{

    class BendingFunction : EnergyFunction
    {
        const float EPS = 1e-6f;

        private int i0, i1, i2, i3;
        private int[] particles;
        private float invM0, invM1, invM2, invM3;
        private float[] inverseMasses;

        private Matrix4x4 Q;



        private BendingFunction(int i0, int i1, int i2, int i3, float invM0, float invM1, float invM2, float invM3, Matrix4x4 Q) {
            this.i0 = i0;
            this.i1 = i1;
            this.i2 = i2;
            this.i3 = i3;

            this.particles = new int[] { i0, i1, i2, i3 };

            this.invM0 = invM0;
            this.invM1 = invM1;
            this.invM2 = invM2;
            this.invM3 = invM3;

            this.inverseMasses = new float[] { invM0, invM1, invM2, invM3 };

            this.Q = Q;

        }

        /// <summary>
        /// Creates a bending function such that the current bending between the four given points is preserved. 
        /// The four given points are expected to form a stencil such that we have two triangles i0, i1, i2 and i0, i1, i3.
        /// Then the bending energy is used to keep i2 and i3 at the same angle with the edge i0i1 as the origin. 
        /// 
        ///     i2
        ///    /  \
        ///  i0 -- i1
        ///    \  /
        ///     i3
        /// 
        /// </summary> 
        public static BendingFunction create(ParticleModel pm, int i0, int i1, int i2, int i3)
        {
            float invM0 = pm.inverseMasses[i0];
            float invM1 = pm.inverseMasses[i1];
            float invM2 = pm.inverseMasses[i2];
            float invM3 = pm.inverseMasses[i3];

            Matrix4x4 Q = LocalHessianEnergy(i0, i1, i2, i3, ref pm.positions);

            return new BendingFunction(i0, i1, i2, i3, invM0, invM1, invM2, invM3, Q);
        }


        public int[] GetParticles()
        {
            return this.particles;
        }

        private static float TriangleArea(int i0, int i1, int i2, ref Vector3[] positions)
        {
            return 0.5f * Vector3.Cross(positions[i1] - positions[i0], positions[i2] - positions[i0]).magnitude;
        }

        /// <summary>
        /// Given a Vector4 V this method computes V^T * V. 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>The result of transposing the given vector, and then multiplying that by the given vector. </returns>
        private static float InnerProduct(Vector4 vector)
        {
            return vector.w * vector.w + vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
        }

        /// <summary>
        /// Given a Vector3 V this method computes V^T * V. 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>The result of transposing the given vector, and then multiplying that by the given vector. </returns>
        private static float InnerProduct(Vector3 vector)
        {
            return vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
        }

        /// <summary>
        /// Compute the Hessian energy for this bending function for the given positions.
        /// </summary>
        /// <param name="positions">The positions of all particles, which are to be used to define area and the likes</param>
        /// <returns>The Hessian energy for the particles given the provided positions.</returns>
        private static Matrix4x4 LocalHessianEnergy(int i0, int i1, int i2, int i3, ref Vector3[] positions)
        {
            float area0 = TriangleArea(i0, i1, i2, ref positions);
            float area1 = TriangleArea(i0, i1, i3, ref positions);

            Vector3 e0 = positions[i1] - positions[i0];
            Vector3 e1 = positions[i2] - positions[i0];
            Vector3 e2 = positions[i3] - positions[i0];
            Vector3 e3 = positions[i2] - positions[i1];
            Vector3 e4 = positions[i3] - positions[i1];

            float C01 = 1 / (float) Math.Tan(Vector3.Angle(e0, e1));
            float C02 = 1 / (float) Math.Tan(Vector3.Angle(e0, e2));
            float C03 = 1 / (float) Math.Tan(Vector3.Angle(-e0, e3));
            float C04 = 1 / (float) Math.Tan(Vector3.Angle(-e0, e4));

            float factor = -(1.5f / (area0 + area1));

            Vector4 K = new Vector4(C03 + C04, C01 + C02, -C01 - C03, -C02 - C04);
            Vector4 KT = new Vector4(factor * K[3], factor * K[2], factor * K[1], factor * K[0]);

            Matrix4x4 Q = new Matrix4x4();

            for (int i = 0; i < 4; ++i) {
                Q[i, i] = K[i] * KT[i];
                for (int j = 0; j < i; j++)
                    Q[i, j] = Q[j, i] = K[i] * KT[j];
            }

            return Q;
        }

        public void solve(ref Vector3[] positions, ref Vector3[] corrections)
        {
            float energy = 0f;
            for (int i = 0; i < 4; ++i)
                for (int j = 0; j < 4; ++j)
                    energy += Q[j, i] * (Vector3.Dot(positions[particles[i]], positions[particles[j]]));
            energy *= 0.5f;

            Vector3[] gradients = new Vector3[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero};

            for ( int i = 0; i < 4; i++ ) {
                for ( int j = 0; j < 4; j++ ) {
                    Vector3 z = Q[ j, i ] * positions[ particles[ j ] ];
                    gradients[ j ] += z;
                }
            }

            float sum_normGradC = 0.0f;
            for (int i = 0; i < 4; i++)
            {
                sum_normGradC += inverseMasses[i] * InnerProduct(gradients[i]);
            }

            // exit early if required
            if (Math.Abs(sum_normGradC) > EPS)
            {
                // compute impulse-based scaling factor
                float s = energy / sum_normGradC;

                for (int i = 0; i < 4; ++i)
                {
                    corrections[particles[i]] += /*-stiffness*/ -10000000.1f * (s * inverseMasses[i]) * gradients[i];
                }
            }
        }
    }
}
