using System;
using System.Runtime.InteropServices;

namespace LevelDB
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Log(string msg);

    public class Logger : LevelDBHandle
    {
        public Logger(Log log)
        {
            var p = Marshal.GetFunctionPointerForDelegate(log);
            this.Handle = LevelDBInterop.leveldb_logger_create(p);
        }

        public static implicit operator Logger(Log log)
        {
            return new Logger(log);
        }

        protected override void FreeUnManagedObjects()
        {
            if (this.Handle != default(IntPtr))
                LevelDBInterop.leveldb_logger_destroy(this.Handle);
        }
    }
}