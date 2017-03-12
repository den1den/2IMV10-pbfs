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
    public class FEMTriangleFunction : EnergyFunction
    {
        const float EPS = 1e-6f;

        ParticleModel pm;
        int i0, i1, i2;
        int[] particles;
        float area;
        Matrix4x4 invRestMat;

        float youngsModulusX;
        float youngsModulusY;
        float youngsModulusShear;
        float poissonRatioXY;
        float poissonRatioYX;


        private FEMTriangleFunction(ParticleModel pm, int i0, int i1, int i2, float area, float youngsModulusX, 
            float youngsModulusY, float youngsModulusShear, float poissonRatioXY, float poissonRatioYX, Matrix4x4 invRestMat)
        {
            this.pm = pm;
            this.i0 = i0;
            this.i1 = i1;
            this.i2 = i2;
            particles = new int[] { i0, i1, i2};

            this.youngsModulusX = youngsModulusX;
            this.youngsModulusY = youngsModulusY;
            this.youngsModulusShear = youngsModulusShear;
            this.poissonRatioXY = poissonRatioXY;
            this.poissonRatioYX = poissonRatioYX;
 
            this.area = area;
            this.invRestMat = invRestMat;
        }
        public Vector3 d0, d1, d2, d3;

        /// <summary>Create FEMFunction for a triangle (finite element method)</summary>
        /// <param name="pm">The actual ParticleModel</param>
        /// <param name="i0">Index of 0 particle of triangle</param>
        /// <param name="i1">Index of 1 particle of triangle</param>
        /// <param name="i2">Index of 2 particle of triangle</param>
        public static FEMTriangleFunction create(ParticleModel pm, int i0, int i1, int i2, float youngsModulusX,
            float youngsModulusY, float youngsModulusShear, float poissonRatioXY, float poissonRatioYX)
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
                return new FEMTriangleFunction(pm, i0, i1, i2, area, youngsModulusX, youngsModulusY, youngsModulusShear, poissonRatioXY, poissonRatioYX, invRestMat);
            }
            // determinant ~= 0
            return null;
        }

        /// <summary>
        /// Solve the FEM triangle constraint. 
        /// Based on code by Müller and Bender. The original C++ code is available through the following link:
        /// https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/a737f21a704a94227c943a9a2291a0e9f31366e2/PositionBasedDynamics/PositionBasedDynamics.cpp#L882
        /// </summary>
        public void solve(ref Vector3[] positions)
        {

            // Orthotropic elasticity tensor
            Matrix4x4 C = Matrix4x4.zero;
            C[0, 0] = youngsModulusX / (1.0f - poissonRatioXY * poissonRatioYX);
            C[0, 1] = youngsModulusX * poissonRatioYX / (1.0f - poissonRatioXY * poissonRatioYX);
            C[1, 1] = youngsModulusY / (1.0f - poissonRatioXY * poissonRatioYX);
            C[1, 0] = youngsModulusY * poissonRatioXY / (1.0f - poissonRatioXY * poissonRatioYX);
            C[2, 2] = youngsModulusShear;


            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];

            // Determine \partial x/\partial m_i
            Matrix4x4 F = Matrix4x4.zero;
            Vector3 p13 = p0 - p2;
            Vector3 p23 = p1 - p2;
            F[0, 0] = p13[0] * invRestMat[0, 0] + p23[0] * invRestMat[1, 0];
            F[0, 1] = p13[0] * invRestMat[0, 1] + p23[0] * invRestMat[1, 1];
            F[1, 0] = p13[1] * invRestMat[0, 0] + p23[1] * invRestMat[1, 0];
            F[1, 1] = p13[1] * invRestMat[0, 1] + p23[1] * invRestMat[1, 1];
            F[2, 0] = p13[2] * invRestMat[0, 0] + p23[2] * invRestMat[1, 0];
            F[2, 1] = p13[2] * invRestMat[0, 1] + p23[2] * invRestMat[1, 1];

            // epsilon = 0.5(F^T * F - I)
            Matrix4x4 epsilon = Matrix4x4.zero;
            epsilon[0, 0] = 0.5f * (F[0, 0] * F[0, 0] + F[1, 0] * F[1, 0] + F[2, 0] * F[2, 0] - 1.0f);      // xx
            epsilon[1, 1] = 0.5f * (F[0, 1] * F[0, 1] + F[1, 1] * F[1, 1] + F[2, 1] * F[2, 1] - 1.0f);      // yy
            epsilon[0, 1] = 0.5f * (F[0, 0] * F[0, 1] + F[1, 0] * F[1, 1] + F[2, 0] * F[2, 1]);             // xy
            epsilon[1, 0] = epsilon[0, 1];

            // P(F) = det(F) * C*E * F^-T => E = green strain
            Matrix4x4 stress = Matrix4x4.zero;
            stress[0, 0] = C[0, 0] * epsilon[0, 0] + C[0, 1] * epsilon[1, 1] + C[0, 2] * epsilon[0, 1];
            stress[1, 1] = C[1, 0] * epsilon[0, 0] + C[1, 1] * epsilon[1, 1] + C[1, 2] * epsilon[0, 1];
            stress[0, 1] = C[2, 0] * epsilon[0, 0] + C[2, 1] * epsilon[1, 1] + C[2, 2] * epsilon[0, 1];
            stress[1, 0] = stress[0, 1];

            Matrix4x4 piolaKirchhoffStress = F * stress;

            float psi = 0.0f;
            for (int j = 0; j < 2; j++)
                for (int k = 0; k < 2; k++)
                    psi += epsilon[j, k] * stress[j, k];

            float energy = area * 0.5f * psi;

            // compute gradient
            Matrix4x4 H = (piolaKirchhoffStress * invRestMat.transpose);
            Vector3[] gradC = new Vector3[3];
            for (int j = 0; j < 3; ++j)
            {
                gradC[0][j] = H[j, 0] * area;
                gradC[1][j] = H[j, 1] * area;
            }
            gradC[2] = -gradC[0] - gradC[1];

            
            float sumNormGradC = 0;
            for (int i = 0; i < particles.Length; ++i)
                sumNormGradC =  pm.inverseMasses[particles[i]] * (float)Math.Pow(gradC[i].magnitude, 2);

            // only update if the change is not negligble. 
            if (Math.Abs(sumNormGradC) > EPS)
            {
                // compute scaling factor
                float s = energy / sumNormGradC;

                // update positions
                for(int i = 0; i < particles.Length; ++i)
                {
                    int particle = particles[i];
                    if(pm.inverseMasses[particle] != 0f)
                        positions[particle] += -(s * pm.inverseMasses[particle]) * gradC[i];

                }
            }
        }
    }
}

