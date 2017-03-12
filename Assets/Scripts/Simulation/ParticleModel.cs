using Assets.Scripts.Simulation.EnergyFunctions;
using Assets.Scripts.Tools.Visualisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This is the actual model as described in the original paper.
/// It should not be depending on any energy function
/// It is used to apply external forces and execute the symplectic euler intergration scheme (update velocities/speeds/masses)
/// </summary>
public class ParticleModel : MonoBehaviour
{
    public Vector3[] positions;
    public Vector3[] velocities;
    public float[] masses;
    public float[] inverseMasses;

    public int[] triangleIndices;

    public EnergyFunction[] efs;

    ParticleModelCalculator pmc;

    /// <summary>
    /// Set this.positions 
    /// </summary>
    private void initPoints(float dx, float dy, int nx, int ny, float zCoord)
    {
        // set positions
        positions = new Vector3[nx * ny];
        for (int x = 0; x < nx; x++)
            for (int y = 0; y < ny; y++)
                    positions[x * ny + y] = new Vector3(x * dx, y * dy, zCoord); // row major

        triangleIndices = new int[(nx - 1) * (ny - 1) * 3 * 2];
        int i = 0;
        for (int x0 = 0; x0 < nx - 1; x0++)
        {
            int x1 = x0 + 1;
            for (int y0 = 0; y0 < ny - 1; y0++)
            {
                int y1 = y0 + 1;
                // Create 2 triangles for each square
                if ((x0 + y0) % 2 == 0) // Alternate the ordering of the triangles
                {
                    // 10 - 11
                    // |  \ |
                    // 00 - 01
                    // the two triangles are (00, 01, 10) and (11, 10, 01)
                    triangleIndices[i++] = x0 * ny + y0;
                    triangleIndices[i++] = x0 * ny + y1;
                    triangleIndices[i++] = x1 * ny + y0;
                    triangleIndices[i++] = x1 * ny + y1;
                    triangleIndices[i++] = x1 * ny + y0;
                    triangleIndices[i++] = x0 * ny + y1;
                }
                else
                {
                    // 10 - 11
                    // |  / |
                    // 00 - 01
                    // the two triangles are (00, 01, 11) and (11, 10, 00)
                    triangleIndices[i++] = x0 * ny + y0;
                    triangleIndices[i++] = x0 * ny + y1;
                    triangleIndices[i++] = x1 * ny + y1;
                    triangleIndices[i++] = x1 * ny + y1;
                    triangleIndices[i++] = x1 * ny + y0;
                    triangleIndices[i++] = x0 * ny + y0;
                }
            }
        }

        // Set masses
        masses = new float[positions.Length];
        inverseMasses = new float[masses.Length];
        for (i = 0; i < masses.Length; i++)
        {
            masses[i] = 1f;
            inverseMasses[i] = 1f;
        }

        // Set masses of endpoints at y=0 to zero to make it static
        // TODO: mass of 0 = static point
        // masses[0] = 0; // x=0, y=0
        // masses[(nx - 1) * ny + 0] = 0; // x = nx, y = 0
        // inverseMasses[0] = -1;
        // inverseMasses[(nx - 1) * ny + 0] = -1; // x = nx, y = 0
    }

    // Use this for initialization
    public ParticleModel(ClothSimulation settings)
    {
        // Create particles
        int RES = settings.particles;
        float SIZE = settings.totalSize;
        float D = SIZE / RES; // dx, dy, dz = total size devided by resolution 
        float zCoord = 1;
        initPoints(D, D, RES, RES, zCoord);
        Debug.Log(positions.Length + " particles created");

        // Create constraints on particles
        initConstraints();

        // Initialize simulation calculation
        velocities = new Vector3[positions.Length];
        pmc = new ParticleModelCalculator(this);
    }

    private void initConstraints()
    {
        // Initial values taken from https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/Demos/Simulation/SimulationModel.cpp#L11 onwards.
        const float youngsModulusX = 1;
        const float youngsModulusY = 1;
        const float youngsModulusShear = 1;
        const float poissonRatioXY = 0.3f;
        const float poissonRatioYX = 0.3f;

        // https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/Demos/ClothDemo/main.cpp#L354
        // loop over every triangle

        int nTriangles = triangleIndices.Length / 3;
        List<EnergyFunction> efs = new List<EnergyFunction>();

        for (int i = 0; i < nTriangles; i++)
        {
            int i0 = triangleIndices[i + 0];
            int i1 = triangleIndices[i + 1];
            int i2 = triangleIndices[i + 2];

            // Only add FEMTetConstriant for now
            EnergyFunction fem = FEMTriangleFunction.create(this, i0, i1, i2, youngsModulusX, youngsModulusY, youngsModulusShear, poissonRatioXY, poissonRatioYX);
            if(fem != null)
            {
                efs.Add(fem);
            }

            // TODO: OR we can only add Distance and Volume constraints
            // https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/Demos/ClothDemo/main.cpp#L340
        }
        Debug.Log(efs.Count + " FEMFunctions created for " + nTriangles + " triangles");
        this.efs = efs.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        // Update model via the ParticleModelCalculator
        pmc.Update(Time.deltaTime);
    }
}
