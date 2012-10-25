# leveldb for Windows and .NET #

----------

[leveldb](http://code.google.com/p/leveldb/) is a fast key-value storage library written at Google that provides an ordered mapping from string keys to string values.

This project aims to provide .NET bindings to LevelDB in addition to making LevelDB work well on Windows.

# Getting Started #

Here's how you can get started with leveldb and .NET.

## Opening A Database ##

A Leveldb database has a name which corresponds to a directory on the system.  This then stores all files in this particular folder.  In this example, you can create a new database (if missing) in the C:\temp\tempdb directory.

```csharp
// Open a connection to a new DB and create if not found
var options = new Options { CreateIfMissing = true; };
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
var options = new Options { CreateIfMissing = true; };
using (var db = new DB(options, @"C:\temp\tempdb")) 
{
    // Use leveldb
}
```

## Reads and Writes ##

leveldb provides the Get, Put and Delete methods to query, update and delete database objects.

```csharp
const string key = "KEY";

// Put in the key value
keyValue.Put(key, "Value");

// Print out the value
var keyValue = db.Get(key);
Console.WriteLine(keyValue); 

// Delete the key
db.Delete(key);
```

## Atomic Updates ##

leveldb also supports atomic updates through the WriteBatch class and the Write method on the DB.  This ensures atomic updates should a process exit abnormally.

```csharp
const string key1 = "KEY1";
const string key2 = "KEY2";

var keyValue = db.Get(key1);

// Create a batch to set key2 and delete key1
var batch = new WriteBatch();
batch.Put(key2, keyValue);
batch.Delete(key1);

// Write the batch
db.Write(batch);
```

## Synchronous Writes ##

For performance reasons, by default, every write to leveldb is asynchronous.  This behavior can be changed by providing a WriteOptions class with the Sync flag set to true to a Put method call on the DB instance.

```csharp
// Synchronously write
var writeOptions = new WriteOptions { Sync = true; };
db.Put("KEY", "Value");
```

The downside of this is that due to a process crash, these updates may be lost.  

As an alternative, atomic updates can be used as a safer alternative with a synchronous write which the cost will be amortized across all of the writes in the batch.

```csharp
// Create a batch to set key2 and delete key1
var batch = new WriteBatch();
batch.Put(key2, keyValue);
batch.Delete(key1);

// Write the batch
var writeOptions = new WriteOptions { Sync = true; }
db.Write(batch, writeOptions);
```

## Iteration ##

The following example shows how you can iterate all the keys as strings.

```csharp
var iterator = db.CreateIterator();

// Iterate to print the keys as strings
for (it.SeekToFirst(); it.IsValid(); it.Next()) 
{
    Console.WriteLine("Key as string: {0}", it.KeyAsString());
}
```

The next example shows how you can iterate all the values in the leveldb instance in reverse.

```csharp
var iterator = db.CreateIterator();

// Iterate in reverse to print the values as strings
for (it.SeekToLast(); it.IsValid(); it.Prev()) 
{
    Console.WriteLine("Value as string: {0}", it.ValueAsString());
}
```

# LICENSE #

----------

**Copyright 2012 Microsoft Corporation**

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.