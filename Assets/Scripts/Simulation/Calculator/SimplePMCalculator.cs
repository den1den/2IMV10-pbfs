using Assets.Scripts.Simulation.EnergyFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

/// <summary>
/// Here the complex simulation is performed.
/// </summary>
/// 

public class SimplePMCalculator : PMCalculator {
    ParticleModel pm;
    Physics physics;
    CollisionDetector cd;
    Vector3[] positions;
    Vector3[] corrections;

    int update = 0;

    int iterations = 5;
    public int Iterations { get { return iterations; } set { iterations = value; } }

    const bool ENABLE_GRAVITY = true;

    public SimplePMCalculator( ParticleModel pm ) {
        this.pm = pm;

        Vector3 minPoint = new Vector3(-10, -10, -10);
        Vector3 maxPoint = new Vector3(pm.SIZE + 10, pm.SIZE + 10, pm.SIZE + 10);
        //cd = new CollisionDetector(pm, minPoint, maxPoint);

        corrections = new Vector3[ pm.Count ];
        positions = new Vector3[ pm.Count ];
        physics = new Physics( pm );
    }

    // Update is called once per frame
    public void Update( float dt ) {


        // overwrites positions and corrections (not incremental!)
        physics.UpdateForces( pm );
        physics.CalculateNextPosition( pm, dt, ref positions, ref corrections);

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
        for ( int iteration = 0; iteration < Iterations; ++iteration ) {

            // What if we let every iteration have 1 / ITERATIONS effect on the actual corrected values? This could lead to a more stable system.

            foreach ( EnergyFunction e in pm.efs ) {
                e.solve( ref positions, ref corrections );
            }

            for ( int i = 0; i < pm.Count; i++ ) {
                positions[ i ] = pm.positions[ i ] + corrections[ i ];
            }
        }

        // recalculate velocities
        for ( int i = 0; i < pm.Count; i++ ) {
            pm.velocities[ i ] = corrections[ i ] / dt;
        }

        //cd.detectAndResolve();

        physics.DampVelocities( pm, dt );

        // Apply position changes
        for ( int i = 0; i < pm.Count; i++ ) {
            pm.positions[ i ] = positions[ i ];
        }

        update++;
    }

    public void Release() {

    }

}
