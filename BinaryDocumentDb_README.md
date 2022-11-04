# Binary Document Database

A what? 
Ever wanted to store a few bits of information, but finds the idea of storing lots of small
files alongside your project leaves you feeling like something isn't right, and you know it!

How about you're generating data that doesn't fit well with a bit of JSON or XML, so
the effort to craft such structures to house different bits of data starts getting ugly,
really quickly with Base64 encoded strings, and it all seems like such a lot of work -
and the whole reason you're even contemplating it, is because a database just to store
some configuration is over-kill?

What if you could have something more akin to a dictionary of objects where you're given a
key for each object you want to add?  In essence, that's what the Binary Document Database is
all about.  Its more accurately considered as: 

``` Dictionary<uint, byte[]> ``` 

But in reality, it also stores the binary in a single file.


#### Solid State Drives and Nand flash memory

One of the things I was thinking about when I created this, was how to write to the disk
less often, and wondered if there was a way to avoid hitting the same area of the disk many
times with ever changing data.  

I've no idea what full database technologies do to avoid killing an SSD by continuously
writing again and again to the same file with slightly different data (over time) - you could 
find your drive failing despite it being just weeks old!  
I believe there is tech in the drives themselves to keep data moving around and exercising
more of the disk where necessary, but ultimately, there are things that I've done to try
and help in this scenario too.

#### No Indexes?
Nope.  No index!  This is something that would change a lot!  Its also getting into the
relational database side of things, and this is meant to be a simple key-value store.
How then do I know what's stored in the file?

The database file is broken down into 1 or more entries.
An "entry" in the database, is either some data, or an empty space.

- Empty space?  If you delete an entry, the database will mark it as an empty space.
 This avoids writing anything more than a header byte to convert an entry to an empty space. 
   
- Entry?  A header byte indicates that this following bytes are an entry in the database.
  -  Each entry has a 4 x byte (uint) primary key.
  -  Each entry has a 4 x byte (uint) indicator or size (in bytes) of the data stored.
  -  Each entry contains the desired data.

Technically, I suppose, it would be possible to undo a delete as only one byte changes.
However, when you next add or update an entry, if the data will fit into the available space,
then it re-uses the available space, and any remaining space is marked as empty.

#### BinaryDb Start up (indexing)

When you create an instance of the BinaryDocumentDb, you're also ask for the file path and name
of where you want to store or load an existing file.  If the file exists, the code will parse
over the entire database looking for both entries and empty spaces, and keeping an in-memory
collection of both.
In this way, when you interact with the BinaryDocumentDb you provide a key, and it knows 
the offset within the file to find the object.  It reads the entry, and extracts the data 
you requested as a ``` byte[] ```

#### Features:

- IBinaryDocumentDb
   - The only public type you can see!  The concrete implementation is marked ``` internal ```
- IBinaryDocumentDbFactory
   - The factory from which you can create instances of an ``` IBinaryDocumentDb ```  
- DependencyInjectionRegistration
   - registration code for the concrete implementation of the factory you'll need. 

#### Why a factory?
When you create a concrete instance of the IBinaryDocumentDb, you have to provide a file name.
The concrete implementation doesn't just read the file, but keeps it open 
for read/write access.

This effectively means there can only be one instance of the interface for any one database
file name.

The factory keeps track of instances, so if you do ask for the same file again, it will give
you the existing instance instead of failing when attempting to create a new instance.

#### Example walkthrough:

``` 
var factory = DIContainer.DI.GetService<IBinaryDocumentDbFactory>();
var instance = factory!.Make(new BdDbConfig() { FilePathAndName = "staticTest.db" });
```

The project contains an integration test that creates an instance of the factory,
then uses the factory to create a physical database file.

```
var createResult = instance.Create(blob);
var key = createResult.Result;
```

Asking the instance to ```Create``` and entry will cause the data to be physically stored,
and the key you'll need to retrieve that data is returned as part of an ```ExecResponse```.

The ExecResponse represents the result of calling any method.  Primarily it contains
a "Success" field that is true if no Exceptions are thrown.  In other words, if we didn't
run out of disk space or some other catastrophic reason, it will be "true".

It there was a problem, it will return an ErrorCode of 13.  Unlucky!
The ExecResponse also has a property that is an IEnumerable&lt;string&gt; for messages.
This may contain something useful.  Its is likely the message property of an Exception.

Finally, the ExecResponse has a "Result" property.  This is the result you're looking for!

```
var readResult = instance.Read(key);
```
Have a wild guess..  This is how you read data out of the database by the storage key.
It too returns an ExecResponse so you know the key was not invalid, and it succeeded in
reading data.  The data is retured in the "Result" property as ```IReadOnlyList<byte>```.

So, that should keep you happy!
I hope this little project finds a home in your code base.
