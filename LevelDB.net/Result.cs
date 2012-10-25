using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace LevelDB
{
    //public class Result : LevelDBHandle, IEnumerable<byte>, IEnumerable<int>
    //{
    //    private int length;
    //    public Result(IntPtr handle, int length)
    //    {
    //        this.Handle = handle;
    //        this.length = length;

    //        BitConverter.ToInt32(null, 0);
    //    }


    //    public byte[] Get(byte[] key, ReadOptions options)
    //    {
    //        IntPtr error;
    //        int length;
    //        var v = LevelDBInterop.leveldb_get(this.Handle, options.Handle, key, key.Length, out length, out error);
    //        Throw(error);

    //        if (v != IntPtr.Zero)
    //        {
    //            var bytes = new byte[length];
    //            Marshal.Copy(v, bytes, 0, length);
    //            Marshal.FreeHGlobal(v);
    //            return bytes;
    //        }
    //        return null;
    //    }
    //}
}