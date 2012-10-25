#if false
namespace LevelDB
{
    public class LeastRecentlyUsedSet<T> : IEnumerable<T>
    {
        public delegate void WriteAction(Action<T> add, Action<T> remove);

        readonly LinkedList<T> inOrder = new LinkedList<T>();
        readonly int maxEntries;
        readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
        readonly HashSet<T> set;

        public LeastRecentlyUsedSet(IEqualityComparer<T> comparer)
            : this(comparer, 100)
        {
        }

        public LeastRecentlyUsedSet(int maxEntries)
            : this(EqualityComparer<T>.Default, maxEntries)
        {
            this.maxEntries = maxEntries;
        }

        public LeastRecentlyUsedSet(IEqualityComparer<T> comparer, int maxEntries)
        {
            set = new HashSet<T>(comparer);
            this.maxEntries = maxEntries;
        }

        public LeastRecentlyUsedSet()
            : this(EqualityComparer<T>.Default)
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return set.ToList().GetEnumerator();
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Write(WriteAction action)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                action(Add, Remove);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        private void Add(T item)
        {
            set.Add(item);
            inOrder.AddLast(item);
            if (inOrder.Count <= maxEntries)
                return;

            set.Remove(inOrder.First.Value);
            inOrder.RemoveFirst();
        }

        public bool Contains(T item)
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return set.Contains(item);
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        private void Remove(T item)
        {
            set.Remove(item);
        }
    }
}
#endif