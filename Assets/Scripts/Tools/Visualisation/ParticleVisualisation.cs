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
            Material defaultMaterial = new Material(Shader.Find("Transparent/Diffuse"));

            Material specialMaterial = new Material(Shader.Find("Transparent/Diffuse"));
            specialMaterial.color = Color.red;

            Vector3[] points = this.model.getMainPoints();
            for ( int i = 0; i < points.Length; i++ ) {
                Material material = this.model.isSpecialPoint(i) ? specialMaterial : defaultMaterial;
                var newObj = objectManager.New( );
                newObj.Init( mesh, material );
                newObj.transform.position = points[ i ];
            }
            Debug.Log("Prticle visualization initialized for " + points.Length + " particles");
        }

        public ParticleVisualisation(TriangularMesh model, ClothSimulation settings) : this(model)
        {
            // Could store extra settings from the Unity GUI via the ClothSimulation class
        }

        public void Update( ) {
            int i = 0;
            Vector3[] points = this.model.getMainPoints();
            foreach ( var obj in objectManager.GetAll( ) ) {
                if ( i > points.Length ) break;
                obj.transform.position = points[i++];
            }
        }
    }

}