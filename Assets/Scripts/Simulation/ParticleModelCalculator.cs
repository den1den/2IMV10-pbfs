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
    Vector3[] initialPositions;
    Vector3[] forces;

    const int ITERATIONS = 5;

    const bool ENABLE_GRAVITY = true;

    public ParticleModelCalculator(ParticleModel pm)
    {
        this.pm = pm;

        forces = new Vector3[pm.positions.Length];
        velocities2 = new Vector3[pm.positions.Length];
        positions2 = new Vector3[pm.positions.Length];
        initialPositions = new Vector3[pm.positions.Length];
        Array.Copy(pm.positions, initialPositions, pm.positions.Length);

    }

    Vector3[] velocities2;
    Vector3[] positions2;

    int update = 0;

    // Update is called once per frame
    public void Update(float dt)
    {
        update++;
        setForces(dt);

        for(int i = 0; i < velocities2.Length; i++)
        {
            velocities2[i] = pm.velocities[i] + forces[i] * dt * pm.inverseMasses[i];
            positions2[i] = pm.positions[i] + velocities2[i] * dt;
        }
        Debug.Log("positions2: " + positions2);

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

        Debug.Log("Calling solve on "+ pm.efs.Length + " EnergyFunctions, iterating "+ITERATIONS+" times");
        Vector3[] debugPreEnergyFunctionPostitions = new Vector3[positions2.Length];
        Array.Copy(positions2, debugPreEnergyFunctionPostitions, positions2.Length);
        for (int iteration = 0; iteration < ITERATIONS; ++iteration) {
            foreach(EnergyFunction e in pm.efs)
            {
                e.solve(ref positions2);
            }
        }
        float diff = Util.getTotalDifference(debugPreEnergyFunctionPostitions, positions2);
        Debug.Log("EnergyFunction provide total change of " + diff);

        for (int i = 0; i < velocities2.Length; i++)
        {
            velocities2[i] = (1.0f / dt) * (positions2[i] - pm.positions[i]);
        }

        // TODO: collision detection
        // damp velocities:
        for (int i = 0; i < velocities2.Length; i++)
        {
            velocities2[i] *= Mathf.Pow(0.01f, 2*dt);
        }
        // Increment n
        for (int i = 0; i < pm.positions.Length; i++)
        {
            pm.positions[i] = positions2[i];
            pm.velocities[i] = velocities2[i];
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