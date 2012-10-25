using System;
using System.Collections.Generic;
using System.Linq;
using LevelDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using LevelDB.NativePointer;

namespace LevelDBUnitTests
{
    [TestClass]
    public class Tests
    {
        static string testPath = @"C:\Temp\Test";
        static string CleanTestDB()
        {
            DB.Destroy(new Options { CreateIfMissing = true }, testPath);
            return testPath;
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestOpen()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options { CreateIfMissing = true }, path))
            {
            }

            using (var db = new DB(new Options { ErrorIfExists = true }, path))
            {
            }
        }

        [TestMethod]
        public void TestInts()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                db.Put(1, new[]{1,2,3}, new WriteOptions());
                var xs = db.Get(1, new ReadOptions());
                CollectionAssert.AreEquivalent(new[] {1, 2, 3}, xs);
            }
        }

        [TestMethod]
        public void TestCRUD()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                db.Put("Tampa", "green");
                db.Put("London", "red");
                db.Put("New York", "blue");

                Assert.AreEqual(db.Get("Tampa"), "green");
                Assert.AreEqual(db.Get("London"), "red");
                Assert.AreEqual(db.Get("New York"), "blue");

                db.Delete("New York");

                Assert.IsNull(db.Get("New York"));

                db.Delete("New York");
            }
        }

        [TestMethod]
        public void TestRepair()
        {
            TestCRUD();
            DB.Repair(new Options(), testPath);
        }

        [TestMethod]
        public void TestIterator()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                db.Put("Tampa", "green");
                db.Put("London", "red");
                db.Put("New York", "blue");

                var expected = new[] {"London", "New York", "Tampa"};

                var actual = new List<string>();
                using (var iterator = db.CreateIterator(new ReadOptions()))
                {
                    iterator.SeekToFirst();
                    while (iterator.IsValid())
                    {
                        var key = iterator.KeyAsString();
                        actual.Add(key);
                        iterator.Next();
                    }
                }

                CollectionAssert.AreEqual(expected, actual);

            }
        }

        [TestMethod]
        public void TestEnumerable()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options { CreateIfMissing = true }, path))
            {
                db.Put("Tampa", "green");
                db.Put("London", "red");
                db.Put("New York", "blue");

                var expected = new[] {"London", "New York", "Tampa"};
                var actual = from kv in db as IEnumerable<KeyValuePair<string, string>>
                             select kv.Key;

                CollectionAssert.AreEqual(expected, actual.ToArray());
            }
        }

        [TestMethod]
        public void TestSnapshot()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                db.Put("Tampa", "green");
                db.Put("London", "red");
                db.Delete("New York");

                using(var snapShot = db.CreateSnapshot())
                {
                    var readOptions = new ReadOptions {Snapshot = snapShot};

                    db.Put("New York", "blue");

                    Assert.AreEqual(db.Get("Tampa", readOptions), "green");
                    Assert.AreEqual(db.Get("London", readOptions), "red");

                    // Snapshot taken before key was updates
                    Assert.IsNull(db.Get("New York", readOptions));
                }

                // can see the change now
                Assert.AreEqual(db.Get("New York"), "blue");

            }
        }

        [TestMethod]
        public void TestGetProperty()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                var r = new Random(0);
                var data = "";
                for (var i = 0; i < 1024; i++)
                {
                    data += 'a' + r.Next(26);
                }

                for (int i = 0; i < 5 * 1024; i++)
                {
                    db.Put(string.Format("row{0}",i), data);
                }

                var stats = db.PropertyValue("leveldb.stats");

                Assert.IsNotNull(stats);
                Assert.IsTrue(stats.Contains("Compactions"));
            }
        }

        [TestMethod]
        public void TestWriteBatch()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options { CreateIfMissing = true }, path))
            {
                db.Put("NA", "Na");

                using(var batch = new WriteBatch())
                {
                    batch.Delete("NA")
                         .Put("Tampa", "Green")
                         .Put("London", "red")
                         .Put("New York", "blue");
                    db.Write(batch);
                }

                var expected = new[] { "London", "New York", "Tampa" };
                var actual = from kv in db as IEnumerable<KeyValuePair<string, string>>
                             select kv.Key;

                CollectionAssert.AreEqual(expected, actual.ToArray());
            }
        }

        [TestMethod]
        public void TestLogger()
        {
            var messages = new List<string>();
            var path = CleanTestDB();

            var logger = new Logger(s =>
                                        {
                                            Console.WriteLine("msg:{0}", s);
                                        });

            using (var db = new DB(new Options
                                        {
                                            CreateIfMissing = true,
                                            InfoLog = logger
                                        }, path))
            {
                for (var j = 0; j < 5; j++)
                {
                    var r = new Random(0);
                    var data = "";

                    for (int i = 0; i < 1024; i++)
                    {
                        data += 'a' + r.Next(26);
                    }
                    for (int i = 0; i < 5 * 1024; i++)
                    {
                        db.Put(string.Format("row{0}", i), data);
                    }
                    Thread.Sleep(100);
                }
            }

            //Assert.IsFalse(messages.Count == 0);

            GC.KeepAlive(logger);
        }

        [TestMethod]
        public void TestNativePointers()
        {
            var path = CleanTestDB();

            using (var db = new DB(new Options {CreateIfMissing = true}, path))
            {
                db.Put(1, new[]{1,2,3}, new WriteOptions());
                Console.WriteLine("put 1, [1,2,3]");
                
                var key = NativeArray.FromArray(new int[] {1});
                using (var xs = db.GetRaw<int>(key))
                {
                    Console.WriteLine("get {0} => [{1}]", key[0], string.Join(",", xs));
                    Assert.IsTrue(
                        new[] {1, 2, 3}.Zip(xs, (x, y) => x == y)
                            .All(b => b)
                        );
                }
            }
        }

        [TestMethod]
        public void TestCompare()
        {
            var path = CleanTestDB();

            var options = new Options {CreateIfMissing = true};
            options.Comparator = Comparator.Create(
                "integers mod 2",
                (xs, ys) => LexicographicalCompare(((NativeArray<int>) xs).Select(x => x%2),
                                                   ((NativeArray<int>) ys).Select(y => y%2)));

            using (var db = new DB(options, path))
            {
                db.Put(1, new[] { 1, 2, 3 }, new WriteOptions());
                Console.WriteLine("put 1, [1,2,3]");

                var key = NativeArray.FromArray(new int[] { 3 });
                using (var xs = db.GetRaw<int>(key))
                {
                    Console.WriteLine("get {0} => [{1}]", key[0], string.Join(",", xs));
                    Assert.IsTrue(
                        new[] { 1, 2, 3 }.Zip(xs, (x, y) => x == y)
                            .All(b => b)
                        );
                }
            }
        }

        private int LexicographicalCompare<T>(IEnumerable<T> xs, IEnumerable<T> ys)
        {
            var comparator = System.Collections.Generic.Comparer<T>.Default;

            using(var xe = xs.GetEnumerator())
            using(var ye = ys.GetEnumerator())
            {
                for(;;)
                {
                    var xh = xe.MoveNext();
                    var yh = ye.MoveNext();
                    if (xh != yh)
                        return yh ? -1 : 1;
                    if (!xh)
                        return 0;

                    // more elements
                    int diff = comparator.Compare(xe.Current, ye.Current);
                    if (diff != 0)
                        return diff;
                }
            }
        }
    }
}