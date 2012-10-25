using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LevelDB;

namespace LevelDB.NativePointer
{
    // note: sizeof(Ptr<>) == sizeof(IntPtr) allows you to create Ptr<Ptr<>> of arbitrary depth and "it just works"
    // IntPtr severely lacks appropriate arithmetic operators; up-promotions to ulong used instead.
    public struct Ptr<T> 
        where T : struct
    {
        private IntPtr addr;

        public Ptr(IntPtr addr)
        {
            this.addr = addr;
        }

        // cannot use 'sizeof' operator on generic type parameters
        public static readonly uint sizeof_T = (uint)Marshal.SizeOf(typeof(T));
        private static readonly IDeref<T> deref = getDeref();

        private static IDeref<T> getDeref()
        {
            if (typeof(T) == typeof(int))
                return (IDeref<T>) new IntDeref();

            // TODO: other concrete implementations of IDeref.
            // (can't be made generic; will not type check)

            // fallback
            return new MarshalDeref<T>();
        }

        public static explicit operator Ptr<T>(IntPtr p)
        {
            return new Ptr<T>(p);
        }
        public static explicit operator IntPtr(Ptr<T> p)
        {
            return p.addr;
        }

        // operator Ptr<U>(Ptr<T>)
        public static Ptr<U> Cast<U>(Ptr<T> p)
            where U : struct
        {
            return new Ptr<U>(p.addr);
        }

        public void Inc() { Advance((IntPtr)1); }
        public void Dec() { Advance((IntPtr)(- 1)); }
        
        public void Advance(IntPtr d)
        {
            addr = (IntPtr)((ulong)addr + sizeof_T*(ulong)d);
        }
        public IntPtr Diff(Ptr<T> p2)
        {
            var diff = (long)(((ulong)addr) - ((ulong)p2.addr));
            Debug.Assert(diff % sizeof_T == 0);

            return (IntPtr)(diff / sizeof_T);
        }
        public T Deref()
        {
            return deref.Deref(addr);
        }
        public void DerefWrite(T newValue)
        {
            deref.DerefWrite(addr, newValue);
        }

        // C-style pointer arithmetic. IntPtr is used in place of C's ptrdiff_t
        #region pointer/intptr arithmetic
        public static Ptr<T> operator++(Ptr<T> p)
        {
            p.Inc();
            return p;
        }
        public static Ptr<T> operator --(Ptr<T> p)
        {
            p.Dec();
            return p;
        }
        public static Ptr<T> operator +(Ptr<T> p, IntPtr offset)
        {
            p.Advance(offset);
            return p;
        }
        public static Ptr<T> operator +(IntPtr offset, Ptr<T> p)
        {
            p.Advance(offset);
            return p;
        }
        public static Ptr<T> operator -(Ptr<T> p, IntPtr offset)
        {
            p.Advance((IntPtr)(0 - (ulong)offset));
            return p;
        }
        public static IntPtr operator -(Ptr<T> p, Ptr<T> p2)
        {
            return p.Diff(p2);
        }
        public T this[IntPtr offset]
        {
            get { return (this + offset).Deref(); }
            set { (this + offset).DerefWrite(value); }
        }
        #endregion

        #region comparisons
        public override bool Equals(object obj)
        {
            if (!(obj is Ptr<T>))
                return false;
            return this == (Ptr<T>) obj;
        }
        public override int GetHashCode()
        {
            return ((int)addr ^ (int)(IntPtr)((long) addr >> 6));
        }
        public static bool operator==(Ptr<T> p, Ptr<T> p2)
        {
            return (IntPtr) p == (IntPtr) p2;
        }
        public static bool operator !=(Ptr<T> p, Ptr<T> p2)
        {
            return (IntPtr)p != (IntPtr)p2;
        }
        public static bool operator <(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p < (ulong)(IntPtr)p2;
        }
        public static bool operator >(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p > (ulong)(IntPtr)p2;
        }
        public static bool operator <=(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p <= (ulong)(IntPtr)p2;
        }
        public static bool operator >=(Ptr<T> p, Ptr<T> p2)
        {
            return (ulong)(IntPtr)p >= (ulong)(IntPtr)p2;
        }
        #endregion
        
        #region pointer/int/long arithmetic (convenience)
        public static Ptr<T> operator +(Ptr<T> p, long offset)
        {
            return p + (IntPtr)offset;
        }
        public static Ptr<T> operator +(long offset, Ptr<T> p)
        {
            return p + (IntPtr)offset;
        }
        public static Ptr<T> operator -(Ptr<T> p, long offset)
        {
            return p - (IntPtr)offset;
        }
        public T this[long offset]
        {
            get { return this[(IntPtr)offset]; }
            set { this[(IntPtr) offset] = value; }
        }
        #endregion
    }
    
    public struct NativeArray
        : IDisposable
    {
        public IntPtr baseAddr;
        public IntPtr byteLength;

        public SafeHandle handle;

        public void Dispose()
        {
            if (handle != null)
                handle.Dispose();
        }

        public static NativeArray<T> FromArray<T>(T[] arr, long start=0, long count = -1)
            where T : struct
        {
            if (count < 0) count = arr.LongLength - start;

            var h = new PinnedSafeHandle<T>(arr);
            return new NativeArray<T> {baseAddr = h.Ptr + start, count = (IntPtr)count, handle = h};
        }
    }

    public struct NativeArray<T>
        : IEnumerable<T> 
        , IDisposable
        where T : struct
    {
        public Ptr<T> baseAddr;
        public IntPtr count;

        public SafeHandle handle;
        
        public static implicit operator NativeArray(NativeArray<T> arr)
        {
            return new NativeArray
                       {
                           baseAddr = (IntPtr) arr.baseAddr,
                           byteLength = (IntPtr)((ulong)(IntPtr)(arr.baseAddr+arr.count) - (ulong)(IntPtr)(arr.baseAddr)),
                           handle = arr.handle
                       };
        }
        public static explicit operator NativeArray<T>(NativeArray arr)
        {
            var baseAddr = (Ptr<T>) arr.baseAddr;
            var count = ((Ptr<T>) (IntPtr) ((ulong) arr.baseAddr + (ulong) arr.byteLength)) - baseAddr;

            return new NativeArray<T> { baseAddr = baseAddr, count = count, handle = arr.handle };
        }

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(baseAddr, baseAddr+count, handle);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private class Enumerator : IEnumerator<T>
        {
            private Ptr<T> current;
            private Ptr<T> end;
            private int state;
            private SafeHandle handle;

            public Enumerator(Ptr<T> start, Ptr<T> end, SafeHandle handle)
            {
                this.current = start;
                this.end = end;
                state = 0;
                this.handle = handle;
            }

            public void Dispose()
            {
                GC.KeepAlive(handle);
            }

            public T Current
            {
                get
                {
                    if (handle != null && handle.IsClosed)
                        throw new InvalidOperationException("Dereferencing a closed handle");
                    if (state != 1)
                        throw new InvalidOperationException("Attempt to invoke Current on invalid enumerator");
                    return current.Deref();
                }
            }

            public bool MoveNext()
            {
                switch (state)
                {
                    case 0:
                        state = 1;
                        return current != end;
                    case 1:
                        ++current;
                        if (current == end)
                            state = 2;
                        return current != end;
                    case 2:
                    default:
                        return false;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }
        }

        #endregion

        public T this[IntPtr offset]
        {
            get
            {
                if ((ulong)offset >= (ulong)count)
                    throw new IndexOutOfRangeException("offest");
                var val = baseAddr[offset];
                GC.KeepAlive(this);
                return val;
            }
            set
            {
                if ((ulong)offset >= (ulong)count)
                    throw new IndexOutOfRangeException("offest");
                baseAddr[offset] = value;
                GC.KeepAlive(this);
            }
        }
        public T this[long offset]
        {
            get { return this[(IntPtr) offset]; }
            set { this[(IntPtr) offset] = value; }
        }

        public void Dispose()
        {
            if (handle != null)
                handle.Dispose();
        }
    }
    
    #region dereferencing abstraction
    interface IDeref<T>
    {
        T Deref(IntPtr addr);
        void DerefWrite(IntPtr addr, T newValue);
    }
    internal unsafe class IntDeref : IDeref<int>
    {
        public int Deref(IntPtr addr)
        {
            int* p = (int*)addr;
            return *p;
        }

        public void DerefWrite(IntPtr addr, int newValue)
        {
            int* p = (int*)addr;
            *p = newValue;
        }
    }
    internal class MarshalDeref<T> : IDeref<T>
    {
        public T Deref(IntPtr addr)
        {
            return (T)Marshal.PtrToStructure(addr, typeof(T));
        }

        public void DerefWrite(IntPtr addr, T newValue)
        {
            Marshal.StructureToPtr(newValue, addr, false);
        }
    }
    #endregion    
}
