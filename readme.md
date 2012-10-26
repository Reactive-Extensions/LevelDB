# leveldb for Windows and .NET #

----------

[leveldb](http://code.google.com/p/leveldb/) is a fast key-value storage library written at Google that provides an ordered mapping from string keys to string values.

This project aims to provide .NET bindings to LevelDB in addition to making leveldb work well on Windows.

# Building leveldb #

See file "WINDOWS" for instructions on how to build this in Windows.

- You'll need to install some [Boost libraries](www.boost.org) to build against
- You'll need to create a Microsoft Visual C++ project to build this
- The [WINDOWS file](https://github.com/Reactive-Extensions/LevelDB/blob/master/leveldbNative/WINDOWS) explains both of these processes.

We're looking for volunteers to build a true Win32 port of LevelDB for Windows.

# Getting Started #

Here's how you can get started with leveldb and .NET.

## Opening A Database ##

A Leveldb database has a name which corresponds to a directory on the system.  This then stores all files in this particular folder.  In this example, you can create a new database (if missing) in the C:\temp\tempdb directory.

```csharp
// Open a connection to a new DB and create if not found
var options = new Options { CreateIfMissing = true };
var db = new DB(options, @"C:\temp\tempdb");
```

## Closing a Database ##

When you are finished, you can close the database by calling the Close method.

```csharp
// Close the connection
db.Close();
```

The DB class also implements the IDisposable interface which allows you to use the using block:

```csharp
var options = new Options { CreateIfMissing = true };
using (var db = new DB(options, @"C:\temp\tempdb")) 
{
    // Use leveldb
}
```

## Reads and Writes ##

leveldb provides the Get, Put and Delete methods to query, update and delete database objects.

```csharp
const string key = "New York";

// Put in the key value
keyValue.Put(key, "blue");

// Print out the value
var keyValue = db.Get(key);
Console.WriteLine(keyValue); 

// Delete the key
db.Delete(key);
```

## Atomic Updates ##

leveldb also supports atomic updates through the WriteBatch class and the Write method on the DB.  This ensures atomic updates should a process exit abnormally.

```csharp
var options = new Options { CreateIfMissing = true };
using (var db = new DB(options, path))
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
}
```

## Synchronous Writes ##

For performance reasons, by default, every write to leveldb is asynchronous.  This behavior can be changed by providing a WriteOptions class with the Sync flag set to true to a Put method call on the DB instance.

```csharp
// Synchronously write
var writeOptions = new WriteOptions { Sync = true };
db.Put("New York", "blue");
```

The downside of this is that due to a process crash, these updates may be lost.  

As an alternative, atomic updates can be used as a safer alternative with a synchronous write which the cost will be amortized across all of the writes in the batch.

```csharp
var options = new Options { CreateIfMissing = true };
using (var db = new DB(options, path))
{
	db.Put("New York", "blue");

	// Create a batch to set key2 and delete key1
	using (var batch = new WriteBatch())
	{
		var keyValue = db.Get("New York");
		batch.Put("Tampa", keyValue);
		batch.Delete("New York");
		
		// Write the batch
		var writeOptions = new WriteOptions { Sync = true; }
		db.Write(batch, writeOptions);
	}
}
```

## Iteration ##

The leveldb bindings also supports iteration using the standard GetEnumerator pattern.  In this example, we can select all keys in a LINQ expression and then iterate the results, printing out each key.

```csharp
var keys = 
    from kv in db as IEnumerable<KeyValuePair<string, string>>
    select kv.Key;

foreach (var key in keys) 
{
	Console.WriteLine("Key: {0}", key);
}
```

The following example shows how you can iterate all the keys as strings.

```csharp
// Create new iterator
using (var iterator = db.CreateIterator())
{
	// Iterate to print the keys as strings
	for (it.SeekToFirst(); it.IsValid(); it.Next()) 
	{
	    Console.WriteLine("Key as string: {0}", it.KeyAsString());
	}
}
```

The next example shows how you can iterate all the values in the leveldb instance in reverse.

```csharp
// Create new iterator
using (var iterator = db.CreateIterator())
{
	// Iterate in reverse to print the values as strings
	for (it.SeekToLast(); it.IsValid(); it.Prev()) 
	{
	    Console.WriteLine("Value as string: {0}", it.ValueAsString());
	}
}
```

## Snapshots ##

Snapshots in leveldb provide a consistent read-only view of the entire state of the current key-value store.  Note that the Snapshot implements IDisposable and should be disposed to allow leveldb to get rid of state that was being maintained just to support reading as of that snapshot. 

```csharp
var options = new Options { CreateIfMissing = true }
using (var db = new Db(options, path))
{
    db.Put("Tampa", "green");
    db.Put("London", "red");
    db.Delete("New York");

	using (var snapshot = db.CreateSnapshot()) 
	{
		var readOptions = new ReadOptions {Snapshot = snapShot};

		db.Put("New York", "blue");

		// Will return null as the snapshot created before
		// the updates happened
		Console.WriteLine(db.Get("New York", readOptions)); 
	}
}
```

## Comparators ##

The leveldb keystore uses a default ordering function which orders bytes lexicographically, however, you can specify your own custom comparator when opening a database.

To specify a comparator, set the Comparator property on the Options instance by calling Comparator.Create.  In this instance, we will compare both x and y modulo 2.

```csharp
var options = new Options { CreateIfMissing = true };
options.Comparator = Comparator.Create(
    "integers mod 2",
    (xs, ys) => LexicographicalCompare(((NativeArray<int>) xs).Select(x => x % 2),
                                       ((NativeArray<int>) ys).Select(y => y % 2)));

using (var db = new Db(options, path))
{
    db.Put(1, new[] { 1, 2, 3 }, new WriteOptions());
    Console.WriteLine("put 1, [1,2,3]");

    var key = NativeArray.FromArray(new int[] { 3 });
    using (var xs = db.GetRaw<int>(key))
    {
		// Prints 1 2 3
        Console.WriteLine("get {0} => [{1}]", key[0], string.Join(",", xs));
    }
}
```

And the implementation of the Comparator is below:

```csharp
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
```

# LICENSE #

----------

**Copyright 2012 Microsoft Corporation**

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.