using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyParticleMesh : MonoBehaviour {
    public Vector3[] vertices;
    public Vector3[] normals;
    public Vector2[] uvs;
    public int[] triangles;
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();

        int w, h;
        w = 10;
        h = 20;

        vertices = new Vector3[4];
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(w, 0, 0);
        vertices[2] = new Vector3(0, h, 0);
        vertices[3] = new Vector3(w, h, 0);
        mesh.vertices = vertices;

        triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;
        mesh.triangles = triangles;

        normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        mesh.normals = normals;

        uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        uvs[3] = new Vector2(1, 1);
        mesh.uv = uvs;

        mf.mesh = mesh;
    }

    // Update is called once per frame
    void Update () {
        int i = 0;
        while (i < vertices.Length)
        {
            vertices[i] += normals[i] * Mathf.Sin(Time.time);
            i++;
        }
        //mesh.vertices = vertices;
    }
}
