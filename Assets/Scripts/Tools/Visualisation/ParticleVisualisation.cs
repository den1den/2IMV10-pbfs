using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Tools.Visualisation {
    /// <summary>
    /// Given a set of positions, points/particles are rendered in unity
    /// </summary>
    public class ParticleVisualisation {

        MonoManager<SceneObject> objectManager;

        private TriangularMesh model;

        public ParticleVisualisation( TriangularMesh model ) {
            this.model = model;
            GameObject unitPrefab = new GameObject();
            objectManager = new MonoManager<SceneObject>( );
            // elements required to show the object in unity:
            unitPrefab.AddComponent<SceneObject>( );
            unitPrefab.AddComponent<MeshFilter>( );
            unitPrefab.AddComponent<MeshRenderer>( );
            // set as instantiatable object for our factory:
            objectManager.OverrideGameObject( unitPrefab );

            Mesh mesh = Geometry.PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Cube);
            Material material = new Material(Shader.Find("Transparent/Diffuse"));

            Vector3[] points = this.model.getMainPoints();
            for ( int i = 0; i < points.Length; i++ ) {
                var newObj = objectManager.New( );
                newObj.Init( mesh, material );
                newObj.transform.position = points[ i ];
            }
        }

        public ParticleVisualisation(TriangularMesh model, ClothSimulation settings) : this(model)
        {
            // Could store extra settings from the Unity GUI via the ClothSimulation class
        }

        public void UpdatePositions( ) {
            int i = 0;
            Vector3[] points = this.model.getMainPoints();
            foreach ( var obj in objectManager.GetAll( ) ) {
                if ( i > points.Length ) break;
                obj.transform.position = points[ i++ ];
            }
        }
    }

}