using System;
using System.Runtime.InteropServices;
using System.Text;
using LevelDB.NativePointer;
using System.Collections.Generic;

namespace LevelDB
{
    public class Comparator : LevelDBHandle
    {
        private sealed class Inner : IDisposable
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void Destructor(IntPtr GCHandleThis);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate int Compare(IntPtr GCHandleThis,
                                         IntPtr data1, IntPtr size1,
                                         IntPtr data2, IntPtr size2);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate IntPtr Name(IntPtr GCHandleThis);


            private static Destructor destructor
                = (GCHandleThis) =>
                      {
                          var h = GCHandle.FromIntPtr(GCHandleThis);
                          var This = (Inner) h.Target;

                          This.Dispose();

                          // TODO: At the point 'Free' is entered, this delegate may become elligible to be GC'd.
                          // TODO:  Need to look whether GC might run between then, and when this delegate returns.
                          h.Free();
                      };

            private static Compare compare =
                (GCHandleThis, data1, size1, data2, size2) =>
                    {
                        var This = (Inner) GCHandle.FromIntPtr(GCHandleThis).Target;
                        return This.cmp(new NativeArray {baseAddr = data1, byteLength = size1},
                                        new NativeArray {baseAddr = data2, byteLength = size2});
                    };

            private static Name nameAccessor =
                (GCHandleThis) =>
                    {
                        var This = (Inner) GCHandle.FromIntPtr(GCHandleThis).Target;
                        return This.NameValue;
                    };

            private Func<NativeArray, NativeArray, int> cmp;
            private GCHandle namePinned;

            public IntPtr Init(string name, Func<NativeArray, NativeArray, int> cmp)
            {
                // TODO: Complete member initialization
                this.cmp = cmp;

                this.namePinned = GCHandle.Alloc(
                    Encoding.ASCII.GetBytes(name),
                    GCHandleType.Pinned);

                var thisHandle = GCHandle.Alloc(this);

                var chandle = LevelDBInterop.leveldb_comparator_create(
                    GCHandle.ToIntPtr(thisHandle),
                    Marshal.GetFunctionPointerForDelegate(destructor),
                    Marshal.GetFunctionPointerForDelegate(compare),
                    Marshal.GetFunctionPointerForDelegate(nameAccessor)
                    );

                if (chandle == default(IntPtr))
                    thisHandle.Free();
                return chandle;
            }

            private unsafe IntPtr NameValue
            {
                get
                {
                    // TODO: this is probably not the most effective way to get a pinned string
                    var s = ((byte[]) this.namePinned.Target);
                    fixed (byte* p = s)
                    {
                        // Note: pinning the GCHandle ensures this value should remain stable 
                        // Note:  outside of the 'fixed' block.
                        return (IntPtr) p;
                    }
                }
            }

            public void Dispose()
            {
                if (this.namePinned.IsAllocated)
                    this.namePinned.Free();
            }
        }
        
        private Comparator(string name, Func<NativeArray, NativeArray, int> cmp)
        {
            var inner = new Inner();
            try
            {
                this.Handle = inner.Init(name, cmp);
            }
            finally
            {
                if (this.Handle == default(IntPtr))
                    inner.Dispose();
            }
        }

        public static Comparator Create(string name, Func<NativeArray, NativeArray, int> cmp)
        {
            return new Comparator(name, cmp);
        }
        public static Comparator Create(string name, IComparer<NativeArray> cmp)
        {
            return new Comparator(name, (a,b)=>cmp.Compare(a,b));
        }

        protected override void  FreeUnManagedObjects()
        {
            if (this.Handle != default(IntPtr))
            {
                // indirectly invoked CleanupInner
                LevelDBInterop.leveldb_comparator_destroy(this.Handle);
            }
        }
    }
}