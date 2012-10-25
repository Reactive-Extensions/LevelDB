using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using LevelDB.NativePointer;

namespace LevelDB
{
    /// <summary>
    /// A DB is a persistent ordered map from keys to values.
    /// A DB is safe for concurrent access from multiple threads without any external synchronization.
    /// </summary>
    public class DB : LevelDBHandle, IEnumerable<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<byte[],byte[]>>, IEnumerable<KeyValuePair<int, int[]>>
    {
        private Cache _Cache;
        private Logger _InfoLog;
        private Comparator _Comparator;

        static void Throw(IntPtr error)
        {
            Throw(error, msg => new Exception(msg));
        }

        static void Throw(IntPtr error, Func<string, Exception> exception)
        {
            if (error != IntPtr.Zero)
            {
                try
                {
                    var msg = Marshal.PtrToStringAnsi(error);
                    throw exception(msg);
                }
                finally
                {
                    LevelDBInterop.leveldb_free(error);
                }
            }
        }

        /// <summary>
        /// Open the database with the specified "name".
        /// </summary>
        public DB(Options options, string name)
        {
            IntPtr error;
            this._Cache = options.Cache;
            this._InfoLog = options.InfoLog;
            this._Comparator = options.Comparator;
            this.Handle = LevelDBInterop.leveldb_open(options.Handle, name, out error);


            Throw(error, msg => new UnauthorizedAccessException(msg));
        }

        public void Close()
        {
            (this as IDisposable).Dispose();
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(string key, string value, WriteOptions options)
        {
            this.Put(Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes(value), options);
        }

        /// <summary>
        /// Set the database entry for "key" to "value". 
        /// </summary>
        public void Put(string key, string value)
        {
            this.Put(key, value, new WriteOptions());
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// </summary>
        public void Put(byte[] key, byte[] value)
        {
            this.Put(key, value, new WriteOptions());
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(byte[] key, byte[] value, WriteOptions options)
        {
            IntPtr error;
            LevelDBInterop.leveldb_put(this.Handle, options.Handle, key, (IntPtr)key.LongLength, value, (IntPtr)value.LongLength, out error);
            Throw(error);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(int key, int[] value)
        {
            Put(key, value, new WriteOptions());
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(int key, int[] value, WriteOptions options)
        {
            IntPtr error;
            LevelDBInterop.leveldb_put(this.Handle, options.Handle, ref key, (IntPtr)sizeof(int), value, checked((IntPtr)(value.LongLength * 4)), out error);
            Throw(error);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(string key)
        {
            this.Delete(key, new WriteOptions());
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Delete(string key, WriteOptions options)
        {
            this.Delete(Encoding.ASCII.GetBytes(key), options);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(byte[] key)
        {
            this.Delete(key, new WriteOptions());
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Delete(byte[] key, WriteOptions options)
        {
            IntPtr error;
            LevelDBInterop.leveldb_delete(this.Handle, options.Handle, key, (IntPtr)key.LongLength, out error);
            Throw(error);
        }

        public void Write(WriteBatch batch)
        {
            Write(batch, new WriteOptions());
        }

        public void Write(WriteBatch batch, WriteOptions options)
        {
            IntPtr error;
            LevelDBInterop.leveldb_write(this.Handle, options.Handle, batch.Handle, out error);
            Throw(error);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public string Get(string key, ReadOptions options)
        {
            var value = Get(Encoding.ASCII.GetBytes(key), options);
            if (value != null) return Encoding.ASCII.GetString(value);
            return null;
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public string Get(string key)
        {
            return this.Get(key, new ReadOptions());
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(byte[] key)
        {
            return this.Get(key, new ReadOptions());
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(byte[] key, ReadOptions options)
        {
            IntPtr error;
            IntPtr length;
            var v = LevelDBInterop.leveldb_get(this.Handle, options.Handle, key, (IntPtr)key.LongLength, out length, out error);
            Throw(error);

            if (v != IntPtr.Zero)
            {
                try
                {
                    var bytes = new byte[(long)length];

                    // TODO: consider copy loop, as Marshal.Copy has 2GB-1 limit, or native pointers
                    Marshal.Copy(v, bytes, 0, checked((int)length));
                    return bytes;
                }
                finally
                {
                    LevelDBInterop.leveldb_free(v);
                }
            }
            return null;
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public int[] Get(int key)
        {
            return Get(key, new ReadOptions());
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public int[] Get(int key, ReadOptions options)
        {
            
            IntPtr error;
            IntPtr length;
            IntPtr v;
            v = LevelDBInterop.leveldb_get(this.Handle, options.Handle, ref key, (IntPtr)sizeof(int), out length, out error);
            Throw(error);

            if (v != IntPtr.Zero)
            {
                try
                {
                    var bytes = new int[(long)length/4];

                    // TODO: consider >2GB-1
                    Marshal.Copy(v, bytes, 0, checked((int)bytes.LongLength));
                    return bytes;
                }
                finally
                {
                    LevelDBInterop.leveldb_free(v);
                }
            }
            return null;
        }
        public NativeArray<T> GetRaw<T>(NativeArray key)
            where T : struct
        {
            return GetRaw<T>(key, new ReadOptions());
        }
        public NativeArray<T> GetRaw<T>(NativeArray key, ReadOptions options)
            where T : struct
        {
            IntPtr error;
            IntPtr length;

            var handle = new LevelDbFreeHandle();

            // todo: remove typecast to int
            var v = (Ptr<T>)LevelDBInterop.leveldb_get(
                this.Handle,
                options.Handle,
                key.baseAddr,
                key.byteLength,
                out length,
                out error);

            handle.SetHandle((IntPtr)v);

            // round down, truncating the array slightly if needed
            var count = (IntPtr)((ulong)length/Ptr<T>.sizeof_T);

            return new NativeArray<T> { baseAddr = v, count = count, handle = handle };
        }

        /// <summary>
        /// Return an iterator over the contents of the database.
        /// The result of CreateIterator is initially invalid (caller must
        /// call one of the Seek methods on the iterator before using it).
        /// </summary>
        public Iterator CreateIterator()
        {
            return this.CreateIterator(new ReadOptions());
        }

        /// <summary>
        /// Return an iterator over the contents of the database.
        /// The result of CreateIterator is initially invalid (caller must
        /// call one of the Seek methods on the iterator before using it).
        /// </summary>
        public Iterator CreateIterator(ReadOptions options)
        {
            return new Iterator(LevelDBInterop.leveldb_create_iterator(this.Handle, options.Handle));
        }

        /// <summary>
        /// Return a handle to the current DB state.  
        /// Iterators and Gets created with this handle will all observe a stable snapshot of the current DB state.  
        /// </summary>
        public SnapShot CreateSnapshot()
        {
            return new SnapShot(LevelDBInterop.leveldb_create_snapshot(this.Handle), this);
        }

        /// <summary>
        /// DB implementations can export properties about their state
        /// via this method.  If "property" is a valid property understood by this
        /// DB implementation, fills "*value" with its current value and returns
        /// true.  Otherwise returns false.
        ///
        /// Valid property names include:
        ///
        ///  "leveldb.num-files-at-level<N>" - return the number of files at level <N>,
        ///     where <N> is an ASCII representation of a level number (e.g. "0").
        ///  "leveldb.stats" - returns a multi-line string that describes statistics
        ///     about the internal operation of the DB.
        /// </summary>
        public string PropertyValue(string name)
        {
            string result = null;
            var ptr = LevelDBInterop.leveldb_property_value(this.Handle, name);
            if (ptr != IntPtr.Zero)
            {
                try
                {
                    return Marshal.PtrToStringAnsi(ptr);
                }
                finally
                {
                    LevelDBInterop.leveldb_free(ptr);
                }
            }
            return result;
        }

        /// <summary>
        /// If a DB cannot be opened, you may attempt to call this method to
        /// resurrect as much of the contents of the database as possible.
        /// Some data may be lost, so be careful when calling this function
        /// on a database that contains important information.
        /// </summary>
        public static void Repair(Options options, string name)
        {
            IntPtr error;
            LevelDBInterop.leveldb_repair_db(options.Handle, name, out error);
            Throw(error);
        }

        /// <summary>
        /// Destroy the contents of the specified database.
        /// Be very careful using this method.
        /// </summary>
        public static void Destroy(Options options, string name)
        {
            IntPtr error;
            LevelDBInterop.leveldb_destroy_db(options.Handle, name, out error);
            Throw(error);
        }

        protected override void FreeUnManagedObjects()
        {
            if (this.Handle != default(IntPtr))
            LevelDBInterop.leveldb_close(this.Handle);
            
            // it's critical that the database be closed first, as the logger and cache may depend on it.

            if (this._Cache != null)
                this._Cache.Dispose();

            if (this._Comparator != null)
                this._Comparator.Dispose();
            
            if (this._InfoLog != null)
                this._InfoLog.Dispose();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        
        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            using (var sn = this.CreateSnapshot())
            using (var iterator = this.CreateIterator(new ReadOptions { Snapshot = sn }))
            {
                iterator.SeekToFirst();
                while (iterator.IsValid())
                {
                    yield return new KeyValuePair<string, string>(iterator.KeyAsString(), iterator.ValueAsString());
                    iterator.Next();
                }
            }
        }

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator()
        {
            using (var sn = this.CreateSnapshot())
            using (var iterator = this.CreateIterator(new ReadOptions { Snapshot = sn }))
            {
                iterator.SeekToFirst();
                while (iterator.IsValid())
                {
                    yield return new KeyValuePair<byte[], byte[]>(iterator.Key(), iterator.Value());
                    iterator.Next();
                }
            }
        }

        IEnumerator<KeyValuePair<int, int[]>> IEnumerable<KeyValuePair<int, int[]>>.GetEnumerator()
        {
            using (var sn = this.CreateSnapshot())
            using (var iterator = this.CreateIterator(new ReadOptions { Snapshot = sn }))
            {
                iterator.SeekToFirst();
                while (iterator.IsValid())
                {
                    yield return new KeyValuePair<int, int[]>(iterator.KeyAsInt(), iterator.ValueAsInts());
                    iterator.Next();
                }
            }
        }

        
    }
}