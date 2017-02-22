using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class interacs with the unity engine and creates the 'Mesh' from Unity.
/// It is basically a MeshFilter-Controller and must be added as a component to a Unity GameObject
/// </summary>
public class TriangularMesh : MonoBehaviour
{
    TriangularMeshCalculator tmc;

    // Use this for initialization
    void Start()
    {
        this.tmc = new TriangularMeshCalculator(this);
    }

    // Update is called once per frame
    void Update()
    {

    }
}