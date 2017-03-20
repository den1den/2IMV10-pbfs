using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Util : MonoBehaviour
{
    Util() { throw new NotImplementedException(); }

    public static Matrix4x4 createM(Vector3 col0, Vector3 col1, Vector3 col2)
    {
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = col0.x;
        m[1, 0] = col0.y;
        m[2, 0] = col0.z;
        m[0, 1] = col1.x;
        m[1, 1] = col1.y;
        m[2, 1] = col1.z;
        m[0, 2] = col2.x;
        m[1, 2] = col2.y;
        m[2, 2] = col2.z;
        m[3, 3] = 1;
        return m;
    }

    internal static float getTotalDifference(Vector3[] a, Vector3[] b)
    {
        float diff = 0;
        for(int i = 0; i < a.Length; i++)
        {
            diff += (a[i] - b[i]).magnitude;
        }
        return diff;
    }

    public struct Triangle
    {
        public readonly int a;
        public readonly int b;
        public readonly int c;
        public Triangle(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

    }
}
