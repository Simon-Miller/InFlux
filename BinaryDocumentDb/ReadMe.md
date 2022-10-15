# BinaryDocumentDb

Created to enable storing any information no matter how small, as a uniquely 
identifiable and retrievable byte array.  This is really more like an on-disk version
of a `Dictionary<uint, byte[ ]>`

When this project was started, there was thought given to how SSDs have a limited 
capacity to overwrite the same area of the drive again and again.  Therefore, it was
important to find a way of allowing for changes to be stored whilst making as few
changes to the physical NAND Flash memory as possible.  To this end, there is no
index stored.  

We conpensate for this by scanning the database file and creating an in-memory
index.  In reality a Dictionary<uint,uint> where the key is a unique value generated
when you create a record in the store, and the Value part of the dictionary is
a physical Offset into the underlying FileStream.

Entries in the physical file look like this:

##### Empty Entry
- (byte) 0 : meaning an empty space entry.
- (uint) 123 : meaning 123 available bytes including the header byte, and this 
'length' of 4 bytes.

##### Blob Entry
- (byte) 1 : meaning a blob entry.
- (uint) 12 : meaning a length of 12 bytes including header byte, and this 
'length' and 4 bytes.
- (uint) 34 : the KEY that uniquely identified the following data
- (byte\[ ]) : the raw data that was originally asked to be stored.

### I/O Operations
- Deleting or Updating an entry with more or less data that originally was stored may
cause the existing entry to me marked as empty space (changing header byte only).
- If smaller (by 5 or more bytes) will re-use the same entry, but create an empty entry
of the remaining space.
- If larger, even by one byte, will try to find an existing empty space to re-use, 
otherwise the existing entry will be marked as free space, and the new data is added 
to the end of the file.

All operations result in returning an `ExecResponse` object.
These objects encapsulate success or failure, an error code, exception messages,
and a `Result` property (type depends on operation).  

You should ensure the success of the operation with each interaction.

In any operation that may create empty spaces, the free spaces are defragmented, 
so where more than one empty space exist on disk next to each other are merged into
a single entry.

### Events!
BinaryDocumentDb is meant as the basis of other simple stores, or repositories.  We find
it useful know when IO operations that could affect an in-memory cache occurs.
To that end, the **QueuedEvent** type from the InFlux library has been used, because 
if its weakly refernced events that are all queued for processing, givening a more
predictable sequence to event subscription calls.

### Usage

```
var instance = BinaryDocumentDbFactory.Make(new BdDbConfig() 
    { FilePathAndName = "databaseFile.db" }); 
 ```

 - Create a record in the database
 ```
    var createResponse = instance.Create(new byte[]{1,2,3});
    var KEY = createResponse.Result; // such as 123
 ```

 - Read a record from the database
 ```
    var readResult = instance.Read(123);
    byte[] data = readResult.Result.ToArray();
 ```

 - Update an existing record in the database
 ```
    var response = instance.Update(123, new byte[] { 4, 5, 6 });
 ```

 - Delete an existing record in the database
 ```
    var response = instance.Delete(123);
 ```

- Reserve a key in the database before creating an entry 
```
    var instance = new BinaryBlobContext(fs);
    var response = instance.ReserveNextKey();
    if(response.Success)
    {
       var key = response.Result;
    }
```

- Create a new record with a reserved Key
```
    var reservedKey = instance.ReserveNextKey().Result;

    var blob = new byte[] { 1, 2, 3 };
    var response = instance.Create(reservedKey, blob);
```