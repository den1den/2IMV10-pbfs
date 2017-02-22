using Assets.Scripts.Simulation.EnergyFunctions;
using Assets.Scripts.Tools.Visualisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This class only binds to a Unity GameObject.
/// A aquare particle model
/// </summary>
public class ParticleModel : MonoBehaviour
{
    public Vector3[] positions;
    public Vector3[] velocities;
    public float[] masses;

    public int[] tetras;

    public EnergyFunction[] efs;

    ParticleModelCalculator pmc;

    ParticleVisualisation simpleVis;

    void initRect(float dx, float dy, float dz, int nx, int ny, int nz)
    {
        // Positions
        positions = new Vector3[nx * ny * nz];
        for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
                for (int z = 0; z < nz; z++)
                {
                    positions[getRectI(nz, ny, x, y, z)] = new Vector3(x * dx, y * dy, z * dz);
                }

        // Tetrahedrons
        tetras = new int[nx * ny * nz * 20];
        for (int x0 = 0; x0 < nx - 1; x0++)
        {
            int x1 = x0 + 1;
            for (int y0 = 0; y0 < ny - 1; y0++)
            {
                int y1 = y0 + 1;
                for (int z0 = 0; z0 < nz - 1; z0++)
                {
                    int z1 = z0 + 1;
                    int p0 = getRectI(ny, nz, x0, y0, z0);
                    int p1 = getRectI(ny, nz, x0, y0, z1);
                    int p2 = getRectI(ny, nz, x1, y0, z1);
                    int p3 = getRectI(ny, nz, x1, y0, z0);
                    int p4 = getRectI(ny, nz, x0, y1, z0);
                    int p5 = getRectI(ny, nz, x0, y1, z1);
                    int p6 = getRectI(ny, nz, x1, y1, z1);
                    int p7 = getRectI(ny, nz, x1, y1, z0);
                    // Alternate the order
                    if ((x0 + y0 + z0) % 2 == 1)
                    {
                        Array.Copy(new int[] {
                            p2, p1, p6, p3,
                            p6, p3, p4, p7,
                            p4, p1, p6, p5,
                            p3, p1, p4, p0,
                            p6, p1, p4, p3
                        }, 0, tetras, p0 * 20, 20);
                    } else
                    {
                        Array.Copy(new int[] {
                            p0, p2, p5, p1,
                            p7, p2, p0, p3,
                            p5, p2, p7, p6,
                            p7, p0, p5, p4,
                            p0, p2, p7, p5
                        }, 0, tetras, p0 * 20, 20);
                    }
                }
            }
        }

        // Masses
        masses = new float[positions.Length];
        for (int i = 0; i < masses.Length; i++)
        {
            masses[i] = 1f;
        }
        // Everywhere 1, except for x = 0
        for (int y = 0; y < ny; y++)
            for (int z = 0; z < nz; z++)
            {
                //masses[getRectI(ny, nz, 0, y, z)] = 0f;
                // Make sure this goes well in first ParticleModelCalculation step
            }
    }

    // Use this for initialization
    void Start()
    {
        // Create particles
        const int RES = 7;
        const float SIZE = 200f;
        const float D = SIZE / RES; // dx, dy, dz = total size devided by resolution 
        initRect(D, D, D, RES, RES, RES);
        Debug.Log(positions.Length + " particles created");

        // Create constraints on particles
        initConstraints();

        // Initialize simulation calculation
        velocities = new Vector3[positions.Length];
        pmc = new ParticleModelCalculator(this);

        // Render visible aspect of this particle model
        simpleVis = new ParticleVisualisation(positions);
    }

    private void initConstraints()
    {
        // loop over every tetrahedron
        int nTets = tetras.Length / 4;
        List<EnergyFunction> efs = new List<EnergyFunction>();

        for (int i = 0; i < nTets; i++)
        {
            int p0 = tetras[i + 0];
            int p1 = tetras[i + 1];
            int p2 = tetras[i + 2];
            int p3 = tetras[i + 3];

            // Only add FEMTetConstriant for now
            EnergyFunction fem = FEMFunction.create(this, p0, p1, p2, p3);
            if(fem != null)
            {
                efs.Add(fem);
            }

            // TODO: OR we can only add Distance and Volume constraints
            // https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/36f571d27718f8d3fca9cd3344d47f2c3a1e4a09/Demos/BarDemo/main.cpp#L326
        }
        Debug.Log(efs.Count + " FEMFunctions created for " + nTets + " thetrahedons");
        this.efs = efs.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        // Update via the simulation
        pmc.Update(Time.deltaTime);
        // Show new positions
        simpleVis.UpdatePositions(positions);
    }

    public static int getRectI(int ny, int nz, int x, int y, int z)
    {
        return x * ny * nz + y * nz + z;
    }
}
