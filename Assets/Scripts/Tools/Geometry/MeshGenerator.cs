﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Tools.Geometry {
    public static class MeshGenerator {
        public static Mesh NewCircleMesh( int refineCount, float scale = 1 ) {
            refineCount = refineCount < 3 ? 3 : refineCount;
            Mesh m = new Mesh();
            m.name = "Circle";

            var vertices = new Vector3[refineCount + 1];
            var triangles = new int[3 * refineCount];
            vertices[ refineCount ] = new Vector3( 0, 0, 0 );
            for ( int i = 0; i < refineCount; i++ ) {
                float p = i / (float)refineCount;
                float x = (float)Math.Sin(p * Math.PI * 2) * scale;
                float y = (float)Math.Cos(p * Math.PI * 2) * scale;
                vertices[ i ] = new Vector3( x, y, 0 );
                triangles[ i * 3 ] = i;
                triangles[ i * 3 + 2 ] = refineCount;
                triangles[ i * 3 + 1 ] = ( i + 1 ) % refineCount;
            }

            m.vertices = vertices;
            m.triangles = triangles;
            m.RecalculateNormals( );
            //m.RecalculateBounds();

            return m;
        }

        public static Mesh NewPlane( float xSize, float zSize, float y, bool centered, float cx = 0, float cy = 0 ) {
            Mesh m = new Mesh();
            m.name = "ScriptedMesh";

            float xLow;
            float xHigh;
            float yLow;
            float yHigh;

            if ( centered ) {
                xLow = -xSize / 2;
                xHigh = xSize / 2;
                yLow = -zSize / 2;
                yHigh = zSize / 2;
            }
            else {
                xLow = yLow = 0;
                xHigh = xSize;
                yHigh = zSize;
            }

            m.vertices = new Vector3[ ] {
                new Vector3(xLow + cx, y, yLow + cy),
                new Vector3(xHigh + cx, y, yLow + cy),
                new Vector3(xHigh + cx, y, yHigh + cy),
                new Vector3(xLow + cx, y, yHigh + cy)
            };
            m.uv = new Vector2[ ] {
                 new Vector2 (0, 0),
                 new Vector2 (0, 1),
                 new Vector2(1, 1),
                 new Vector2 (1, 0)
            };
            m.triangles = new int[ ] { 2, 1, 0, 3, 2, 0 };
            m.RecalculateNormals( );

            return m;
        }
    }

}
