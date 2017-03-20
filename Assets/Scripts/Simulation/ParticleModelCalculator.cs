using Assets.Scripts.Simulation.EnergyFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Here the complex simulation is performed.
/// </summary>
public class ParticleModelCalculator
{
    ParticleModel pm;
    Vector3[] forces;
    Vector3[] positions;
    Vector3[] corrections;

    int update = 0;

    const int ITERATIONS = 5;

    const bool ENABLE_GRAVITY = true;

    public ParticleModelCalculator(ParticleModel pm)
    {
        this.pm = pm;

        forces = new Vector3[pm.positions.Length];
        corrections = new Vector3[pm.positions.Length];
        positions = new Vector3[pm.positions.Length];
    }

    // Update is called once per frame
    public void Update(float dt)
    {
        update++;
        setForces(dt);

        // Reset corrections
        for(int i = 0; i < corrections.Length; i++)
        {
            corrections[i] = Vector2.zero;
        }

        System.Random r = new System.Random();
        for(int i = 0; i < positions.Length; i++ ) {
            Vector3 dv = pm.velocities[i] + forces[i] * dt * pm.inverseMasses[i];
            //float rv = ( float ) r.NextDouble() * 1.1f;
            positions[ i ] = pm.positions[ i ] + dv * dt;// + new Vector3(rv,rv,rv);
        }

        // Solve C(x + dx) == 0
        // Newton-Raphson: x_{n+1} = x_n - f(x_n)/f'(x_n), but computationally expensive
        // https://nl.wikipedia.org/wiki/Methode_van_Newton-Raphson

        // Gauss-Seidel for Newton-Raphson on system of C equation is faster.
        // (section 6: Gauss-Seidel could solve n*n equations, while Newton-Raphson only n. So factor n faster)
        // (Can be in parrallel)
        // Ax=b     x_{n+1} = A_{lower}^{-1} (b - A_{strict upper} x_{n})
        // https://en.wikipedia.org/wiki/Gauss%E2%80%93Seidel_method

        // Successive over-relaxation, improved Gauss-Seidel
        // https://en.wikipedia.org/wiki/Successive_over-relaxation

        // Single EnergyFunction solves
        // (1) C(x + ∆x) = C(x) + ∇_x C^T(x) ∆x = 0
        // where the gradient ∇ = (dx, dy, dz)
        // (2) considering the mass we have: ∆x = 1/m λ ∇_x C(x)
        // (3) subsituting: λ = -C(x) / ( Sum_i w_i |∇_{x,i} C(x)|^2 )

        //Debug.Log("Calling solve on "+ pm.efs.Length + " EnergyFunctions, iterating "+ITERATIONS+" times");
        for (int iteration = 0; iteration < ITERATIONS; ++iteration) {

            // What if we let every iteration have 1 / ITERATIONS effect on the actual corrected values? This could lead to a more stable system.

            foreach (EnergyFunction e in pm.efs)
            {
                e.solve(ref positions, ref corrections);
            }

            for (int i = 0; i < corrections.Length; i++)
            {
                positions[i] = pm.positions[i] + corrections[i];
            }
        }

        float timeFactor = 1f / dt;
        for (int i = 0; i < corrections.Length; i++)
        {
            pm.velocities[ i ] = timeFactor * ( positions[ i ] - pm.positions[ i ] );// (corrections[i]);
        }

        // TODO: collision detection
        // damp velocities:
        for (int i = 0; i < pm.velocities.Length; i++)
        {
            //pm.velocities[i] *= Mathf.Pow(0.3f, dt);
        }
        // Increment n
        for (int i = 0; i < pm.positions.Length; i++)
        {
            pm.positions[i] = positions[i];
        }
    }

    private void setForces(float dt)
    {
        if (ENABLE_GRAVITY)
        {
            const float G = 9.81f;
            for (int i = 0; i < forces.Length; i++)
            {
                forces[i] = Vector3.down * G;
            }
        }
    }
}