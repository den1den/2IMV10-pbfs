using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace Assets {

    public class UnityTesting : MonoBehaviour {

        MonoManager<SceneObject> objectManager;

        void Start( ) {

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

        void Update( ) {
            foreach ( var t in objectManager.GetAll( ) ) {
                Mesh m = t.GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = m.vertices;
                var rnd = new System.Random(0);
                for(int i = 0; i < vertices.Length; i++) {
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

    public class SceneObject : MonoManagable {

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

    public abstract class Manager<T> where T : Managable {
        protected ReaderWriterLockSlim myLock = new ReaderWriterLockSlim();
        object dictionaryLock = new object();
        IDGen idGen;
        Dictionary<uint, T> elements = new Dictionary<uint, T>();

        // should call Register(T), does not require a lock itself
        abstract public T New( );

        public void Destroy( uint id ) {
            // improove by making a queue of to-delete items
            myLock.EnterWriteLock( );

            T toRemove;
            if ( !elements.TryGetValue( id, out toRemove ) ) return;
            if ( elements.Remove( id ) ) toRemove.Destroy( );

            myLock.ExitWriteLock( );
        }

        public ICollection<T> GetAll( ) {
            return elements.Values;
        }

        public void DestroyAll( ) {
            myLock.EnterWriteLock( ); // wait for others to complete

            foreach ( var unit in elements ) {
                unit.Value.Destroy( );
            }
            elements.Clear( );

            myLock.ExitWriteLock( );
        }

        public void Destroy( T element ) {
            // improove by making a queue of to-delete items
            myLock.EnterWriteLock( );

            elements.Remove( element.GetID( ) );
            element.Destroy( );

            myLock.ExitWriteLock( );
        }

        public T Find( uint id ) {
            T result;

            myLock.EnterReadLock( );
            if ( !elements.TryGetValue( id, out result ) ) return default( T );
            myLock.ExitReadLock( );

            return result;
        }

        protected void Register( T element ) {
            myLock.EnterWriteLock( );

            uint id = idGen.NextLockless();
            element.SetID( id );
            elements.Add( id, element );

            myLock.ExitWriteLock( );
        }
    }

    public interface Managable {
        uint GetID( );
        void SetID( uint id );
        void Create( );
        void Destroy( );
    }

    public abstract class MonoManagable : MonoBehaviour, Managable {

        uint id;

        public abstract void Create( );

        public abstract void Destroy( );

        public uint GetID( ) {
            return id;
        }

        public void SetID( uint id ) {
            this.id = id;
        }


    }

    class MonoManager<T> : Manager<T> where T : MonoManagable {
        static GameObject elementPrefab;

        public MonoManager( ) {
            InitGameObjectPrefab( );
        }

        private static void InitGameObjectPrefab( ) {
            if ( elementPrefab == null ) {
                elementPrefab = new GameObject( "Element" );
                elementPrefab.AddComponent<T>( );
            }
        }

        /*
            The object should at least contain our <T> as component!
        */
        public void OverrideGameObject( GameObject go ) {
            elementPrefab = go;
        }

        override public T New( ) {
            GameObject new_GO = UnityEngine.Object.Instantiate(elementPrefab);
            T newElement = new_GO.GetComponent<T>();
            newElement.Create( );
            this.Register( newElement );
            return newElement;
        }
    }

}