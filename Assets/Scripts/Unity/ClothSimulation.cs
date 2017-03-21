using Assets.Scripts.Tools.Visualisation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class binds to a unity GameObject and sets up a cloth simulation.
/// All public settings in this object could be modified during runetime via the Unity GUI
/// </summary>
public class ClothSimulation : MonoBehaviour {

    public int particles = 3; // Total particles: particles * particles
    public float totalSize = 20;
    public bool parrallel = false;
    public int iterations = 5;

    // TODO: link attributes to Energy Functions
    public float youngsModulusX;
    public float youngsModulusY;
    public float youngsModulusShear;
    public float poissonRatioXY;
    public float poissonRatioYX;

    private ParticleModel model;
    private TriangularMesh meshModel;
    private ParticleVisualisation simpleVis;

    // Use this for initialization
    void Start () {
        Application.runInBackground = true;
        UnityEditor.SceneView.FocusWindowIfItsOpen( typeof( UnityEditor.SceneView ) );
        model = new ParticleModel(this);
        meshModel = new TriangularModelMesh(model, x => x == 0 || x == particles * particles - particles, this);
        simpleVis = new ParticleVisualisation(meshModel, this);
	}
	
	// Update is called once per frame
	void Update () {
        // write settings
        model.Calculator.Iterations = iterations;

        // write settings if changed
        model.VerifyMode( parrallel );


        // TODO: implement other settings:

        model.Update();
        meshModel.Update();
        simpleVis.Update();
	}
}
