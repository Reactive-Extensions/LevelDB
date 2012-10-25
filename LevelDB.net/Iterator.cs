using System;
using System.Runtime.InteropServices;
using System.Text;
namespace LevelDB
{
    /// <summary>
    /// An iterator yields a sequence of key/value pairs from a database.
    /// </summary>
    public class Iterator: LevelDBHandle
    {
        internal Iterator(IntPtr Handle)
        {
            this.Handle = Handle;
        }

        /// <summary>
        /// An iterator is either positioned at a key/value pair, or
        /// not valid.  
        /// </summary>
        /// <returns>This method returns true iff the iterator is valid.</returns>
        public bool IsValid()
        {
            return (int)LevelDBInterop.leveldb_iter_valid(this.Handle) != 0;
        }

        /// <summary>
        /// Position at the first key in the source.  
        /// The iterator is Valid() after this call iff the source is not empty.
        /// </summary>
        public void SeekToFirst()
        {
            LevelDBInterop.leveldb_iter_seek_to_first(this.Handle);
            Throw();
        }

        /// <summary>
        /// Position at the last key in the source.  
        /// The iterator is Valid() after this call iff the source is not empty.
        /// </summary>
        public void SeekToLast()
        {
            LevelDBInterop.leveldb_iter_seek_to_last(this.Handle);
            Throw();
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(byte[] key)
        {
            LevelDBInterop.leveldb_iter_seek(this.Handle, key, key.Length);
            Throw();
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(string key)
        {
            Seek(Encoding.ASCII.GetBytes(key));
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(int key)
        {
            LevelDBInterop.leveldb_iter_seek(this.Handle, ref key, 4);
            Throw();
        }

        /// <summary>
        /// Moves to the next entry in the source.  
        /// After this call, Valid() is true iff the iterator was not positioned at the last entry in the source.
        /// REQUIRES: Valid()
        /// </summary>
        public void Next()
        {
            LevelDBInterop.leveldb_iter_next(this.Handle);
            Throw();
        }

        /// <summary>
        /// Moves to the previous entry in the source.  
        /// After this call, Valid() is true iff the iterator was not positioned at the first entry in source.
        /// REQUIRES: Valid()
        /// </summary>
        public void Prev()
        {
            LevelDBInterop.leveldb_iter_prev(this.Handle);
            Throw();
        }


        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public int KeyAsInt()
        {
            int length;
            var key = LevelDBInterop.leveldb_iter_key(this.Handle, out length);
            Throw();

            if (length != 4) throw new Exception("Key is not an integer");

            return Marshal.ReadInt32(key);
        }

        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public string KeyAsString()
        {
            return Encoding.ASCII.GetString(this.Key());
        }

        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Key()
        {
            int length;
            var key = LevelDBInterop.leveldb_iter_key(this.Handle, out length);
            Throw();

            var bytes = new byte[length];
            Marshal.Copy(key, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public int[] ValueAsInts()
        {
            int length;
            var value = LevelDBInterop.leveldb_iter_value(this.Handle, out length);
            Throw();

            var bytes = new int[length/4];
            Marshal.Copy(value, bytes, 0, length/4);
            return bytes;
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public string ValueAsString()
        {
            return Encoding.ASCII.GetString(this.Value());
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Value()
        {
            int length;
            var value = LevelDBInterop.leveldb_iter_value(this.Handle, out length);
            Throw();
            
            var bytes = new byte[length];
            Marshal.Copy(value, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// If an error has occurred, throw it.  
        /// </summary>
        void Throw()
        {
            Throw(msg => new Exception(msg));
        }

        /// <summary>
        /// If an error has occurred, throw it.  
        /// </summary>
        void Throw(Func<string, Exception> exception)
        {
            IntPtr error;
            LevelDBInterop.leveldb_iter_get_error(this.Handle, out error);
            if (error != IntPtr.Zero) throw exception(Marshal.PtrToStringAnsi(error));
        }

        protected override void FreeUnManagedObjects()
        {
            LevelDBInterop.leveldb_iter_destroy(this.Handle);
        }
    }
}