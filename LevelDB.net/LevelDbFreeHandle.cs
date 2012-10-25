using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace LevelDB
{
    // Wraps pointers to be freed with leveldb_free (e.g. returned by leveldb_get)
    //
    // reference on safe handles: http://blogs.msdn.com/b/bclteam/archive/2006/06/23/644343.aspx
    internal class LevelDbFreeHandle : SafeHandle
    {
        public LevelDbFreeHandle()
            : base(default(IntPtr), true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        override protected bool ReleaseHandle()
        {
            if (this.handle != default(IntPtr))
                LevelDBInterop.leveldb_free(this.handle);
            this.handle = default(IntPtr);
            return true;
        }
        
        public override bool IsInvalid
        {
            get { return this.handle != default(IntPtr); }
        }

        public new void SetHandle(IntPtr p)
        {
            if(this.handle != default(IntPtr))
                ReleaseHandle();
            
            base.SetHandle(p);
        }
    }
}
