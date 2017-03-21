using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;


class Physics {
    private const float G = 9.81f;
    float dampStrength = 0.001f;
    float airResistance = 0.001f;

    public Physics( ParticleModel pm ) {
        UpdateForces( pm );
    }

    public void CalculateNextPosition( ParticleModel pm, float dt, ref Vector3[ ] next_position, ref Vector3[ ] position_change ) {
        for ( int i = 0; i < pm.Count; i++ ) {
            Vector3 tranformation = (pm.velocities[i] + pm.forces[i] * dt * pm.inverseMasses[i]) * dt;
            position_change[ i ] = tranformation;
            next_position[ i ] = pm.positions[ i ] + tranformation;
        }
    }

    public void DampVelocities( ParticleModel pm, float dt ) {
        for ( int i = 0; i < pm.Count; i++ ) {
            pm.velocities[ i ] *= Mathf.Pow( 1 - dampStrength, dt );
        }
    }

    public void UpdateForces( ParticleModel pm ) {
        for ( int i = 0; i < pm.forces.Length; i++ ) {
            // air resitance and gravity
            float speed = pm.velocities[i].magnitude;
            pm.forces[ i ] = G * Vector3.down + airResistance * ( -pm.velocities[ i ].normalized * speed * speed);
        }
    }
} 

