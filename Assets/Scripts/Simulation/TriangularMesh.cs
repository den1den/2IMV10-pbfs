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

    public TriangularModelMesh(ParticleModel model, ClothSimulation settings)
    {
        this.model = model;
        this.tmc = new TriangularMeshCalculator(this);
    }

    public Vector3[] getMainPoints()
    {
        return model.positions;
    }

    public Vector3[] getSubPoints()
    {
        //TODO store/track subparticlepoints
        return new Vector3[0];
    }
}

public interface TriangularMesh
{
    Vector3[] getMainPoints();
}

public class SimplePositionTriangleMesh : TriangularMesh
{
    public Vector3[] points;
    public SimplePositionTriangleMesh(Vector3[] points)
    {
        this.points = points;
    }
    public Vector3[] getMainPoints()
    {
        return this.points;
    }
}