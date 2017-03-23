using Assets.Scripts.Tools.Visualisation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This class binds to a unity GameObject and sets up a cloth simulation.
/// All public settings in this object could be modified during runetime via the Unity GUI
/// </summary>
public class ClothSimulation : MonoBehaviour {
    public static ClothSimulation gInst;
    public int particles = 3; // Total particles: particles * particles
    public float totalSize = 20;
    public bool parrallel = true;
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
#if UNITY_EDITOR
        //SceneView.FocusWindowIfItsOpen( typeof( SceneView ) );
#endif
        model = new ParticleModel(this);
        meshModel = new TriangularModelMesh(model, this);
        simpleVis = new ParticleVisualisation(model, this);
        gInst = this;
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
        if (simpleVis != null) simpleVis.Update();
	}
}
