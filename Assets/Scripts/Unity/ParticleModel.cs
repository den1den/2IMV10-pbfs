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

    // Use this for initialization
    void Start()
    {
        pmc = new ParticleModelCalculator(this);
        const int RES = 50;

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
    }

    // Update is called once per frame
    void Update()
    {
        pmc.Update();
    }
}
