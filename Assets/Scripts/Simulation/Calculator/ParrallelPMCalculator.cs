using Assets.Scripts.Simulation.EnergyFunctions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
 
class ParrallelPMCalculator : PMCalculator {

    ParticleModel pm;
    Physics physics;
    Vector3[] positions;
    Vector3[] corrections;

    int iterations = 5;
    public int Iterations { get { return iterations; } set { iterations = value; } }

    List<SubSetSolver> solverThreads;

    private bool end;
 
    public ParrallelPMCalculator( ParticleModel pm ) {
        this.pm = pm;

        corrections = new Vector3[ pm.Count ];
        positions = new Vector3[ pm.Count ];
        physics = new Physics( pm );

        int threads_1d = 3;
        int nrThreads = threads_1d * threads_1d;

        int cloth_size_1d = (int) (Mathf.Sqrt(pm.Count) + 0.5f);
        if (cloth_size_1d*cloth_size_1d != pm.Count) {
            throw new Exception( "not a square cloth?" );
        }

        SubSetSolver[,] threads = new SubSetSolver[threads_1d,threads_1d];

        int tid = 0;
        for ( int x = 0; x < threads_1d; x++ ) {
            for ( int y = 0; y < threads_1d; y++ ) {
                int low = (int)((float) pm.Count * tid / nrThreads);
                int high = (int)((float) pm.Count * (tid+1) / nrThreads);
                tid++;
                threads[ x, y ] = new SubSetSolver( this, low, high );
            }
        }

        // group be the first particle in the function (TODO: fix overlaps)
        foreach(var f in pm.efs) {
            int id = f.GetParticles()[0];
            // TODO: check other id's, and if the [tx,ty] are not the same mark
            // the solver unsafe and delay its effect untill after syncrhonization
            int px = id % cloth_size_1d;
            int py = id / cloth_size_1d;
            int tx = (int) (threads_1d * px / cloth_size_1d );
            int ty = (int) (threads_1d * py / cloth_size_1d );
            threads[ tx, ty ].Add( f );
        }

        solverThreads = threads.Cast<SubSetSolver>( ).ToList( );

        for ( int t = 0; t < nrThreads; t++ ) {
            Thread thread = new Thread(new ThreadStart(solverThreads[t].Run));
            thread.Start( );
        }

        Thread myThread = new Thread(new ThreadStart(Run));
        myThread.Start( );
    }

    class SubSetSolver {
        bool alive = true;
        List<EnergyFunction> functions;
        int rangeLow;
        int rangeHigh;
        ParrallelPMCalculator pmc;
        ManualResetEvent runFlag = new ManualResetEvent(false);
        ManualResetEvent readyFlag = new ManualResetEvent(false);

        public SubSetSolver(ParrallelPMCalculator pmc, int low, int high) {
            this.functions = new List<EnergyFunction>();
            this.pmc = pmc;
            this.rangeLow = low;
            this.rangeHigh = high;
        }
        public void Add(EnergyFunction f) {
            functions.Add( f );
        }
        public void Run() {
            bool phase1 = true;
            while ( alive ) {
                runFlag.WaitOne( );
                if ( !alive ) break;
                runFlag.Reset( );

                if ( phase1 ) Solve( );
                else Update( );
                phase1 = !phase1;

                readyFlag.Set( );
            }

            // avoid people waiting for this forever
            readyFlag.Set( );
        }
        public void Step() {
            runFlag.Set( );
        }
        public void Wait() {
            readyFlag.WaitOne( );
            readyFlag.Reset( );
        }
        public void Kill() {

        }
        private void Solve() {
            foreach(var f in functions) {
                f.solve( ref pmc.positions, ref pmc.corrections );
            }
        }
        private void Update() {
            for(int i = rangeLow; i < rangeHigh; i++) {
                pmc.positions[ i ] = pmc.pm.positions[ i ] + pmc.corrections[ i ];
            }
        }
    }

    void Run() {
        Stopwatch t = Stopwatch.StartNew();
        while(!end) {
            // busywait:
            while ( t.Elapsed.TotalSeconds < 0.01f );

            float dt = (float) t.Elapsed.TotalSeconds;
            if ( dt > 0.02f ) dt = 0.02f; // limit timestep for stability

            t.Reset( );
            t.Start( );
            Update2( dt );
        }
    }

    public void Update( float dt ) {
       // do nothing?
    }

    public void Update2 (float dt) {
        // overwrites positions and corrections (not incremental!)
        physics.UpdateForces( pm );
        physics.CalculateNextPosition( pm, dt, ref positions, ref corrections );

        for ( int iteration = 0; iteration < Iterations; ++iteration ) {

            // What if we let every iteration have 1 / ITERATIONS effect on the actual corrected values? This could lead to a more stable system.

            for ( int i = 0; i < 2; i++ ) {
                foreach ( var solver in this.solverThreads ) {
                    solver.Step( );
                    if ( end ) return;
                }
                foreach ( var solver in this.solverThreads ) {
                    solver.Wait( );
                    if ( end ) return;
                }
            }
        }

        // recalculate velocities
        for ( int i = 0; i < pm.Count; i++ ) {
            pm.velocities[ i ] = corrections[ i ] / dt;
        }

        // TODO: collision detection

        physics.DampVelocities( pm, dt );

        // Apply position changes
        for ( int i = 0; i < pm.Count; i++ ) {
            pm.positions[ i ] = positions[ i ];
        }
    }

    public void Release( ) {
        end = true;
        foreach ( var t in solverThreads ) {
            t.Kill( );
        }
    }
}

