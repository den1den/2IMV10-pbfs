using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Tools.Visualisation {

    public class ParticleVisualisation {

        MonoManager<SceneObject> objectManager;

        public class ParticleObject : MonoManagable {

            public override void Create( ) {
                // called on creation, cannot have parameters
            }

            public void Init( Mesh mesh, Material material ) {
                this.gameObject.GetComponent<MeshFilter>( ).sharedMesh = mesh;
                this.gameObject.GetComponent<MeshRenderer>( ).sharedMaterial = material;
            }

            public override void Destroy( ) {
                Destroy( gameObject );
            }

        }

        public ParticleVisualisation( Vector3[ ] particles ) {
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

            for ( int i = 0; i < particles.Length; i++ ) {
                var newObj = objectManager.New( );
                newObj.Init( mesh, material );
                newObj.transform.position = particles[ i ];
            }
        }

        public void UpdatePositions( Vector3[ ] particles ) {
            int i = 0;
            foreach ( var obj in objectManager.GetAll( ) ) {
                if ( i > particles.Length ) break;
                obj.transform.position = particles[ i++ ];
            }
        }
    }

}