using Assets.Scripts.Tools.Visualisation;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This class only binds to a Unity GameObject.
/// </summary>
public class ParticleModel : MonoBehaviour
{
    public Vector3[] positions;
    public Vector3[] velocities;
    public float[] masses;

    ParticleModelCalculator pmc;

    ParticleVisualisation simpleVis;

    // Use this for initialization
    void Start()
    {
        const int RES = 7;

        const float dx = 200f / RES; // total size devided by resolution 
        const float dy = dx;
        const float dz = dx;

        positions = new Vector3[RES * RES * RES];

        for (int x = 0; x < RES; x++)
        {
            for(int y = 0; y < RES; y++)
            {
                for (int z = 0; z < RES; z++)
                {
                    positions[x * RES * RES + y * RES + z] = new Vector3(x * dx, y * dy, z * dz);
                }
            }
        }

        masses = new float[positions.Length];
        for(int i = 0; i < masses.Length; i++)
        {
            masses[i] = 1f;
        }

        velocities = new Vector3[positions.Length];
        
        pmc = new ParticleModelCalculator(this);
        simpleVis = new ParticleVisualisation(positions);
    }

    // Update is called once per frame
    void Update()
    {
        pmc.Update(Time.deltaTime);

        simpleVis.UpdatePositions(positions);
    }
}
