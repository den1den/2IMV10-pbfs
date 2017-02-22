using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Tools;

namespace Assets.Scripts {

    public class UnityTesting : MonoBehaviour {

        MonoManager<SceneObject> objectManager;

        void Start( ) {
            //testParticleVis( );
        }

        void testDeform() {
            GameObject unitPrefab = new GameObject();
            objectManager = new MonoManager<SceneObject>( );
            // elements required to show the object in unity:
            unitPrefab.AddComponent<SceneObject>( );
            unitPrefab.AddComponent<MeshFilter>( );
            unitPrefab.AddComponent<MeshRenderer>( );
            // set as instantiatable object for our factory:
            objectManager.OverrideGameObject( unitPrefab );

            SceneObject t = objectManager.New( );
            //Mesh mesh = Tools.Geometry.MeshGenerator.NewCircleMesh(8, 1);
            Mesh mesh = Tools.Geometry.PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Sphere);
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            material.color = new Color( 1, 0, 0 );
            t.Init( mesh, material );

        }

        void testParticleVis() {
            var testArr = new Vector3[4];
            testArr[ 0 ].Set( -1, -1, 0 );
            testArr[ 1 ].Set( -1, 1, 0 );
            testArr[ 2 ].Set( 1, 1, 0 );
            testArr[ 3 ].Set( 1, -1, 0 );
            var pv = new Tools.Visualisation.ParticleVisualisation(testArr);
            
        }

        void Update( ) {
            if ( objectManager != null ) {
                foreach ( var t in objectManager.GetAll( ) ) {
                    Mesh m = t.GetComponent<MeshFilter>().mesh;
                    Vector3[] vertices = m.vertices;
                    var rnd = new System.Random(0);
                    for ( int i = 0; i < vertices.Length; i++ ) {
                        Vector3 norm = vertices[ i ].normalized;
                        // 0 to 1
                        float modifier = 0.5f + 0.5f * Mathf.Sin( Time.time * 1.2f + (float) rnd.NextDouble() * 2 * Mathf.PI);
                        modifier = 0.9f + 0.2f * modifier;
                        Vector3 newPos = norm * modifier;
                        vertices[ i ] = newPos;
                    }
                    m.SetVertices( new List<Vector3>( vertices ) );
                }
            }
        }
    }

    public class SceneObject : 
        MonoManagable {

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

    struct IDGen {
        object idLock;
        uint id_gen;

        public IDGen( uint initial = 1 ) {
            id_gen = initial;
            idLock = new object( );
        }

        public uint Next( ) {
            uint result = 0;
            lock ( idLock ) result = id_gen++;
            return result;
        }

        public uint NextLockless( ) {
            return id_gen++;
        }
    }


}