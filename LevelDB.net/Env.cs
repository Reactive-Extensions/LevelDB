namespace LevelDB
{
    /// <summary>
    /// A default environment to access operating system functionality like 
    /// the filesystem etc of the current operating system.
    /// </summary>
    public class Env : LevelDBHandle
    {
        public Env()
        {
            this.Handle = LevelDBInterop.leveldb_create_default_env();
        }  
 
        protected override void FreeUnManagedObjects()
        {
            LevelDBInterop.leveldb_env_destroy(this.Handle);
        }
    }
}