using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Simulation.EnergyFunctions
{
    class DistanceFunction : EnergyFunction
    {
        int i0, i1;
        float invM0, invM1;
        float initialDistance;

        private DistanceFunction(int i0, int i1, float invM0, float invM1, float initialDistance)
        {
            this.i0 = i0;
            this.i1 = i1;
            this.invM0 = invM0;
            this.invM1 = invM1;
            this.initialDistance = initialDistance;
            
        }

        public static DistanceFunction create(ParticleModel pm, int i0, int i1)
        {
            if (i0 == i1)
                throw new Exception("Can't define Distance function on same particle");

            Vector3 v0 = pm.positions[i0];
            Vector3 v1 = pm.positions[i1];
            float invM0 = pm.inverseMasses[i0];
            float invM1 = pm.inverseMasses[i1];
            float distance = Vector3.Magnitude(new Vector3(v0.x - v1.x, v0.y - v1.y, v0.z - v1.z));

            if (i0 < i1)
                return new DistanceFunction(i0, i1, invM0, invM1, distance);
            else
                return new DistanceFunction(i1, i0, invM1, invM0, distance);

        }

        /// <summary>
        /// 
        /// </summary>
        public void solve(ref Vector3[] positions, ref Vector3[] corrections)
        {
            float compression = 0.03f;
            float stretch = 0.3f;

            //
            Vector3 p2p = Vector3.Scale(positions[i1], Vector3.one) - positions[i0];
            float distance = p2p.magnitude;

            // If the two points are distanced correctly, they don't want to move
            if (distance == initialDistance)
                return;

            // Otherwise see whether the two points are compressed or stretched and compute a new vector 
            // representing the distance between the two points
            float factor = distance < initialDistance ? compression : stretch;
            Vector3 correction = p2p.normalized * factor * ((distance - initialDistance) / (invM0 + invM1));

            // now apply the correction to both points
            corrections[i0] += invM0 * correction;
            corrections[i1] -= invM1 * correction;
            
        }

        public override int GetHashCode()
        {
            // return i0; // -> 4-8 items per bucket
            return i0 * 2 + i1 % 2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DistanceFunction)) return false;
            else
            {
                DistanceFunction other = (DistanceFunction)obj;
                return i0 == other.i0 && i1 == other.i1;
            }
        }
    }
}
