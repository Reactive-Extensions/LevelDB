using System;
using System.Threading;

namespace LevelDB
{
    class Program
    {
        static string testPath = @"C:\Temp\Test";
        static string CleanTestDB()
        {
            DB.Destroy(new Options { CreateIfMissing = true }, testPath);
            return testPath;
        }

        static void Main3()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                db.Put(1, new[] {2, 3}, new WriteOptions());
                db.Put(2, new[] {1, 2, 4});
                db.Put(3, new[] {1, 3});
                db.Put(4, new[] {2, 5, 7});
                db.Put(5, new[] {4, 6, 7, 8});
                db.Put(6, new[] {5});
                db.Put(7, new[] {4, 5, 8});
                db.Put(8, new[] {5, 7});

                var a = db.Get(1);
                var b = db.Get(2);
                var c = db.Get(3);
                var d = db.Get(4);
                var e = db.Get(5);
                var f = db.Get(6);
                var g = db.Get(7);
                var h = db.Get(8);

            }
        }

        static void Main()
        {
            var l = new Logger(s => Console.WriteLine(s));
            var x = new Options 
                    { 
                        CreateIfMissing = true, 
                        RestartInterval = 13, 
                        MaxOpenFiles = 100,
                        InfoLog = l
                    };

            var db = new DB(x, @"C:\Temp\A");
            db.Put("hello", "world");
            var world = db.Get("hello");
            Console.WriteLine(world);

            for (var j = 0; j < 5; j++)
            {
                var r = new Random(0);
                var data = "";

                for (int i = 0; i < 1024; i++)
                {
                    data += 'a' + r.Next(26);
                }
                for (int i = 0; i < 5*1024; i++)
                {
                    db.Put(string.Format("row{0}", i), data);
                }
                Thread.Sleep(100);
            }
            Console.WriteLine();

            //using(var logger = new Logger(Console.WriteLine))
            //{
            //    Console.WriteLine("hello");
            //}

            db.Dispose();
            GC.KeepAlive(l);
        }
    }
}
