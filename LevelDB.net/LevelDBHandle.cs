using System;

namespace LevelDB
{
    /// <summary>
    /// Base class for all LevelDB objects
    /// Implement IDisposable as prescribed by http://msdn.microsoft.com/en-us/library/b1yfkh5e.aspx by overriding the two additional virtual methods
    /// </summary>
    public abstract class LevelDBHandle : IDisposable
    {
        public IntPtr Handle { protected set; get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void FreeManagedObjects()
        {
        }

        protected virtual void FreeUnManagedObjects()
        {
        }

        bool _disposed = false;
        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    FreeManagedObjects();
                }
                if (this.Handle != IntPtr.Zero)
                {
                    FreeUnManagedObjects();
                    this.Handle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        ~LevelDBHandle()
        {
            Dispose(false);
        }
    }
}