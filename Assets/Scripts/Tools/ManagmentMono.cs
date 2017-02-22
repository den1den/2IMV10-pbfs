using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.Scripts.Tools {
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
