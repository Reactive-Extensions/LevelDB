namespace LevelDB
{
    /// <summary>
    /// A Cache is an interface that maps keys to values.  It has internal
    /// synchronization and may be safely accessed concurrently from
    /// multiple threads.  It may automatically evict entries to make room
    /// for new entries.  Values have a specified charge against the cache
    /// capacity.  For example, a cache where the values are variable
    /// length strings, may use the length of the string as the charge for
    /// the string.
    ///
    /// A builtin cache implementation with a least-recently-used eviction
    /// policy is provided.  Clients may use their own implementations if
    /// they want something more sophisticated (like scan-resistance, a
    /// custom eviction policy, variable cache sizing, etc.)
    /// </summary>
    public class Cache : LevelDBHandle
    {
        /// <summary>
        /// Create a new cache with a fixed size capacity.  This implementation
        /// of Cache uses a LRU eviction policy.
        /// </summary>
        public Cache(int capacity)
        {
            this.Handle = LevelDBInterop.leveldb_cache_create_lru(capacity);
        }

        protected override void FreeUnManagedObjects()
        {
            LevelDBInterop.leveldb_cache_destroy(this.Handle);
        }
    }
}