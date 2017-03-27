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

public class ParticleModel {
    public Vector3[] positions;
    public Vector3[] velocities;
    public Vector3[] forces;
    public float[] masses;
    public float[] inverseMasses;
    int Res;

        
    public Util.Triangle[] triangles;


    public PMCalculator Calculator { get { return pmc; } }
    private PMCalculator pmc;
    

    public int Count { get { return positions.Length; } }


    public EnergyFunction[] efs;
    private readonly int particlesSize;


    /// <summary>
    /// Set this.positions 
    /// </summary>
    private void initPoints( float dx, float dy, int nx, int nz, float yCoord ) {
        // set positions
        positions = new Vector3[ nx * nz ];
        for ( int x = 0; x < nx; x++ )
            for ( int z = 0; z < nz; z++ )
                positions[ x * nz + z ] = new Vector3( x * dx, yCoord, z * dy ); // row major

        triangles = new Util.Triangle[(nx - 1) * (nz - 1) * 2];



        int i = 0;
        for ( int i0 = 0; i0 < nx - 1; i0++ ) {
            int x1 = i0 + 1;
            for ( int j0 = 0; j0 < nz - 1; j0++ ) {
                int y1 = j0 + 1;
                // Create 2 triangles for each square
                if ( ( i0 + j0 ) % 2 == 0 ) // Alternate the ordering of the triangles
                {
                    // 10 - 11
                    // |  \ |
                    // 00 - 01
                    // the two triangles are (00, 01, 10) and (11, 10, 01)
                    triangles[i++] = new Util.Triangle(i0 * nz + j0, i0 * nz + y1, x1 * nz + j0);
                    triangles[i++] = new Util.Triangle(x1 * nz + y1, x1 * nz + j0, i0 * nz + y1);

                }
                else {
                    // 10 - 11
                    // |  / |
                    // 00 - 01
                    // the two triangles are (00, 01, 11) and (11, 10, 00)
                    triangles[i++] = new Util.Triangle(i0 * nz + j0, i0 * nz + y1, x1 * nz + y1);
                    triangles[i++] = new Util.Triangle(x1 * nz + y1, x1 * nz + j0, i0 * nz + j0);
                }
            }
        }

        // Set masses
        masses = new float[ positions.Length ];
        inverseMasses = new float[ masses.Length ];
        for ( i = 0; i < masses.Length; i++ ) {
            masses[ i ] = 1;
            inverseMasses[ i ] = 1;
        }

        // Set masses of endpoints at y=0 to zero to make it static
        // mass of 0 = static point
        masses[ 0 ] = 0; // x=0, z=0
        masses[ ( nx - 1 ) * nz + 0 ] = 0; // x = nx, z = 0
        inverseMasses[ 0 ] = 0;
        inverseMasses[ ( nx - 1 ) * nz + 0 ] = 0; // x = nx, y = 0
       }

    // Use this for initialization
    public ParticleModel( ClothSimulation settings ) {
        // Create particles
        int RES = settings.particles;
        this.particlesSize = RES;
        float SIZE = settings.totalSize;
        float D = SIZE / RES; // dx, dy, dz = total size devided by resolution 
        float zCoord = 1;
        this.Res = RES;
        initPoints( D, D, RES, RES, zCoord );
        Debug.Log( positions.Length + " particles created" );

        // Create constraints on particles
        initConstraints( );

        // Initialize simulation calculation
        velocities = new Vector3[ positions.Length ];
        forces = new Vector3[ positions.Length ];
        if ( settings.parrallel )
            pmc = new ParrallelPMCalculator( this );
        else
            pmc = new SimplePMCalculator( this );
        pmc.Iterations = settings.iterations;
    }

    private void initConstraints( ) {
        List<EnergyFunction> efs = new List<EnergyFunction>();

        //initFEMTriangleFunctions(efs);
        initDistanceFunctions( efs );
        initBendingFunctions(efs);

        this.efs = efs.ToArray( );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="efs"></param>
    private void initDistanceFunctions( List<EnergyFunction> efs ) {

        // add straight edges
        for ( int i = 0; i < Res; i++ ) {
            for ( int j = 0; j < Res - 1; j++ ) {
                int x_0 = i * Res + j;
                int x_1 = i * Res + j + 1;
                int y_0 = j * Res + i;
                int y_1 = j * Res + i + Res;
                efs.Add( DistanceFunction.create( this, x_0, x_1 ) );
                efs.Add( DistanceFunction.create( this, y_0, y_1 ) );
            }
        }

        // add cross edges
        for ( int i = 0; i < Res - 1; i++ ) {
            for ( int j = 0; j < Res - 1; j++ ) {
                int id0 = i * Res + j;
                int id1 = id0 + 1;
                int id2 = id0 + Res;
                int id3 = id0 + Res + 1;
                efs.Add( DistanceFunction.create( this, id0, id3 ) );
                efs.Add( DistanceFunction.create( this, id1, id2 ) );
            }
        }

        /*HashSet<EnergyFunction> distancefunctions = new HashSet<EnergyFunction>();
        foreach (Util.Triangle triangle in triangles)
        {
            distancefunctions.Add(DistanceFunction.create(this, triangle.a, triangle.b));
            distancefunctions.Add(DistanceFunction.create(this, triangle.a, triangle.c));
            distancefunctions.Add(DistanceFunction.create(this, triangle.b, triangle.c));
        }
        efs.AddRange(distancefunctions);*/

     }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="efs"></param>
    private void initBendingFunctions(List<EnergyFunction> efs)
    {

        /*
         * First all vertical stencils:
         * 
         *     x,y
         *      |
         *      V
         *      
         * i2_1 *
         *      |\
         *   i0 *-* i1
         *       \|
         *        * i3_1
         *
         * And mirrored:
         * 
         *     x,y
         *      |
         *      V
         *        * i2_2
         *       /|
         *   i0 *-* i1
         *      |/
         * i3_2 *
         */
        for (int y = 0; y < Res - 2; ++y)
        {
            for (int x = 0; x < Res - 1; ++x)
            {

                //Common points
                int i0 = gridXYToParticle(x, y + 1);
                int i1 = gridXYToParticle(x + 1, y + 1);

                // Additional points for first stencil
                int i2_1 = gridXYToParticle(x, y);
                int i3_1 = gridXYToParticle(x + 1, y + 2);

                // Additional points for second stencil
                int i2_2 = gridXYToParticle(x + 1, y);
                int i3_2 = gridXYToParticle(x, y + 2);

                // Create bending function on both stencils
                efs.Add(BendingFunction.create(this, i0, i1, i2_1, i3_1));
                efs.Add(BendingFunction.create(this, i0, i1, i2_2, i3_2));
            }
        }

        /* 
         * Then all horizontal stencils
         * 
         *     x,y
         *      |
         *      V
         *        i0
         * i2_1 *-* 
         *       \|\
         *        *-* i3_1
         *        i1
         *
         * And mirrored
         * 
         *     x,y
         *      |
         *      V
         *        i0
         *        *-* i3_2
         *       /|/
         * i2_2 *-*
         *        i1
         */
        for (int y = 0; y < Res - 1; ++y)
        {
            for (int x = 0; x < Res - 2; ++x)
            {

                //Common points
                int i0 = gridXYToParticle(x + 1, y);
                int i1 = gridXYToParticle(x + 1, y + 1);

                // Additional points for first stencil
                int i2_1 = gridXYToParticle(x, y);
                int i3_1 = gridXYToParticle(x + 2, y + 1);

                // Additional points for second stencil
                int i2_2 = gridXYToParticle(x, y + 1);
                int i3_2 = gridXYToParticle(x + 2, y);

                // Create bending function on both stencils
                efs.Add(BendingFunction.create(this, i0, i1, i2_1, i3_1));
                efs.Add(BendingFunction.create(this, i0, i1, i2_2, i3_2));
            }
        }
        

        /*
         * Then all diagonal stencils
         *      
         *     i0   i1
         *      *---*
         *      |\ /|
         *      | X |
         *      |/ \|
         *      *---*
         *     i2   i3
         * Note that this uses the diagonals i0 i3 and i1 i2, not i0 i1 as was previously the case. 
         * 
         */
        for (int y = 0; y < Res - 1; ++y)
        {
            for (int x = 0; x < Res - 1; ++x)
            {
                //Common points
                int i0 = gridXYToParticle(x, y);
                int i1 = gridXYToParticle(x + 1, y);
                int i2 = gridXYToParticle(x, y + 1);
                int i3 = gridXYToParticle(x + 1, y + 1);
                
                // Create bending function on both stencils
                efs.Add(BendingFunction.create(this, i0, i3, i1, i2));
                efs.Add(BendingFunction.create(this, i1, i2, i0, i3));
            }
        }

    }

    /// <summary>
    /// Converts an x y coordinate in the particle grid to the index of that particle in the particles array. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private int gridXYToParticle(int x, int y)
    {
        return y * Res + x;
    }



    /// <summary>
    /// Define al FEM triangle functions and add them to the given list of energy functions. 
    /// </summary>
    /// <param name="efs"></param>
    private void initFEMTriangleFunctions( List<EnergyFunction> efs ) {

        // Initial values taken from https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/Demos/Simulation/SimulationModel.cpp#L11 onwards.
        const float youngsModulusX = 1;
        const float youngsModulusY = 1;
        const float youngsModulusShear = 1;
        const float poissonRatioXY = 0.3f;
        const float poissonRatioYX = 0.3f;

        // https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/Demos/ClothDemo/main.cpp#L354
        // loop over every triangle



        foreach (Util.Triangle triangle in triangles)
        {
            // Only add FEMTetConstriant for now
            EnergyFunction fem = FEMTriangleFunction.create(this, triangle.a, triangle.b, triangle.c, youngsModulusX, youngsModulusY, youngsModulusShear, poissonRatioXY, poissonRatioYX);
            if ( fem != null ) {
                efs.Add( fem );
            }

            // TODO: OR we can only add Distance and Volume constraints
            // https://github.com/InteractiveComputerGraphics/PositionBasedDynamics/blob/master/Demos/ClothDemo/main.cpp#L340
        }
        Debug.Log( efs.Count + " FEMFunctions created for " + triangles.Length + " triangles" );
    }

    // Update is called once per frame
    public void Update( ) {
        // Update model via the ParticleModelCalculator
        pmc.Update( 0.01f );// Time.deltaTime);
    }

    public void SetPMC( PMCalculator pmc ) {
        this.pmc.Release( );
        this.pmc = pmc;
    }

    public void VerifyMode(bool parrallel) {
        if ( parrallel && !( pmc is ParrallelPMCalculator ) ) {
            SetPMC( new ParrallelPMCalculator( this ) );
        }
        if ( !parrallel && !( pmc is SimplePMCalculator ) ) {
            SetPMC( new SimplePMCalculator( this ) );
        }
    }

    public bool isSpecialPoint(int index)
    {
        return masses[index] == 0;
        //return index == 0 || index == particles * particles - particles;
    }

    /// <summary>
    /// Get the total number of particles in the width and height
    /// </summary>
    /// <returns>this.particles</returns>
    public int getWidthHeight()
    {
        return this.particlesSize;
    }
}
