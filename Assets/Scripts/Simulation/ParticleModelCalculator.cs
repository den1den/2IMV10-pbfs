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
    float[] massInv;

    public ParticleModelCalculator(ParticleModel pm)
    {
        this.pm = pm;

        forces = new Vector3[pm.positions.Length];
        velocities2 = new Vector3[pm.positions.Length];
        positions2 = new Vector3[pm.positions.Length];

        // Create mass^{-1} vector because mass is only used like this
        massInv = new float[pm.masses.Length];
        for(int i = 0; i < massInv.Length; i++)
        {
            massInv[i] = 1.0f / pm.masses[i];
        }
    }

    Vector3[] velocities2;
    Vector3[] positions2;

    // Update is called once per frame
    public void Update(float dt)
    {
        setForces(dt);

        for(int i = 0; i < velocities2.Length; i++)
        {
            velocities2[i] = pm.velocities[i] + forces[i] * dt * massInv[i];
            positions2[i] = pm.positions[i] + velocities2[i] * dt;
        }

        // TODO: Energy functions

        for(int i = 0; i < velocities2.Length; i++)
        {
            velocities2[i] = (1.0f / dt) * (positions2[i] - pm.positions[i]);
        }

        // TODO: collision detection
        // TODO: damp velocities

        // Increment n
        for (int i = 0; i < pm.positions.Length; i++)
        {
            pm.positions[i] = positions2[i];
            pm.velocities[i] = velocities2[i];
        }
    }

    private void setForces(float dt)
    {
        const float MAG = 5f; // The magnitude of the force
        const float SPEED = 2f; // The speed of the change in direction of the force
        float F = Mathf.Cos(Time.time * SPEED) * MAG;
        for (int i = 0; i < forces.Length; i++)
        {
            forces[i] = Vector3.right * F;
        }
    }
}