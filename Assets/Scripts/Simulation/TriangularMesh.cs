using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains the model for the triangular mesh around the particle model.
/// A visualization should only use this class to display the triangluar mesh in Unity
/// </summary>
public class TriangularModelMesh : TriangularMesh
{
    private ParticleModel model;
    private TriangularMeshCalculator tmc;

    public delegate bool IsSpecialFunc( int id );
    IsSpecialFunc f;

    public TriangularModelMesh(ParticleModel model, IsSpecialFunc f, ClothSimulation settings)
    {
        this.model = model;
        this.tmc = new TriangularMeshCalculator(this);
        this.f = f;
    }

    public override Vector3[] getMainPoints()
    {
        return model.positions;
    }

    public override Vector3[] getSubPoints()
    {
        //TODO store/track subparticlepoints
        return new Vector3[0];
    }

    public override void Update()
    {
        // No calculations needed on subparticles
    }

    public override bool isSpecialPoint(int i)
    {
        return f( i );
    }
}

public abstract class TriangularMesh
{
    public abstract Vector3[] getMainPoints();
    public virtual Vector3[] getSubPoints() { return new Vector3[0];  }
    public virtual bool isSpecialPoint(int i) { return false; }
    public virtual void Update() { }
}

public class SimplePositionTriangleMesh : TriangularMesh
{
    public Vector3[] points;
    public SimplePositionTriangleMesh(Vector3[] points)
    {
        this.points = points;
    }
    public override Vector3[] getMainPoints()
    {
        return this.points;
    }
}