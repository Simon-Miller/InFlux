namespace BinaryDocumentDb.Tests
{
    [TestClass]
    public class FileStuffTests
    {
        [TestMethod]
        public void Can_scan_file()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0, // wasted 5 bytes on empty entry at beginning of file.
                
                1, 10,0,0,0, 1,0,0,0, 255 // entry of key=1, value = 255 (1 byte)
            });

            var instance = new FileStuff(fs);

            // Act
            var (index, freespace) = instance.ScanFile();

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(1, freespace.Count);

            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(5u, freespace[0].Length);

            Assert.AreEqual(true, index.ContainsKey(1));
            Assert.AreEqual(5u, index[1]);
        }

        [TestMethod]
        public void Can_scan_file_with_2_blobs()
        {
            // trying to ensure that seeking the end of each entry works, and is not off-by-one.

            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
              //0  1  2 3 4  5 6 7 8  9
                1, 10,0,0,0, 1,0,0,0, 255, // entry of key=1, value = 255 (1 byte)

              //10 11121314
                0, 5,0,0,0, // wasted 5 bytes on empty entry at beginning of file.

              //15 16 171819 20212223 24
                1, 10,0,0,0, 2,0,0,0, 254, // entry of key=1, value = 255 (1 byte)
            });

            var instance = new FileStuff(fs);

            // Act
            var (index, freespace) = instance.ScanFile();

            // Assert
            Assert.AreEqual(2, index.Count);
            Assert.AreEqual(1, freespace.Count);

            // Not sure I like that we point to the entity data instead of the entity type.
            // I think the address containing the type byte is part of each entity, and we should include that as the address.
            // The length could even be the entire length of the entity includingf its type byte?  It makes sense we translate that 
            // length to what ever we need by subtracting the byte (1) and subtracting the length (5) to get the actual data length.

            Assert.AreEqual(00u, index[1]);
            Assert.AreEqual(15u, index[2]);
            Assert.AreEqual(10u, freespace[0].Offset);

            Assert.AreEqual(5u, freespace[0].Length); // include type byte
        }

        [TestMethod]
        public void Can_defrag_empty_space_entries()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,    // 00: empty 5 bytes entry
                0, 6,0,0,0,0,  // 05: empty 6 bytes entry
                0, 7,0,0,0,0,0,// 11: empty 7 bytes entry

                1, 10,0,0,0, 1,0,0,0, 123,  // 18: blob entry in middle.

                0, 5,0,0,0,    // 28: empty 5 bytes entry
                0, 6,0,0,0,0,  // 33: empty 6 bytes entry

                1, 12,0,0,0, 2,0,0,0, 1,2,3 // 38: blob entry at end.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            Assert.AreEqual(2, freespace.Count);

            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(18u, freespace[0].Length);
            Assert.AreEqual(28u, freespace[1].Offset);
            Assert.AreEqual(11u, freespace[1].Length);

            Assert.AreEqual(2, index.Count);
            Assert.AreEqual(18u, index[1u]);
            Assert.AreEqual(39u, index[2u]);

            // Gotcha! Off by one?  reports 17 when should be 18?
            // Gotcha! 4th free space entry NOT merged with 5th?
            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0,18,0,0,0, 0,6,0,0,0,0, 0,7,0,0,0,0,0, // 00: defrag'd entry with old data unchanged
                  1, 10,0,0,0, 1,0,0,0, 123,              // 18: existing entry unchanged
                  0, 11,0,0,0, 0,6,0,0,0,0,               // 28: defrag'd entry with old data unchanged
                  1, 12,0,0,0, 2,0,0,0, 1,2,3             // 39: existing entry unchanged
                }));
        }

        [TestMethod]
        public void Can_insert_blob_at_end()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0, // wasted 5 bytes on empty entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(5u, freespace[0].Length);

            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(5u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 0, 5, 0, 0, 0, 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void Can_insert_blob_in_available_space_of_exact_size()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 12,0,0,0,0,0,0,0,0,0,0 // empty 12 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(0, freespace.Count);
            Assert.AreEqual(1, index.Count);

            Assert.AreEqual(0u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void Can_insert_blob_at_end_when_no_space_found()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0 // empty 5 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(5u, freespace[0].Length);

            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(5u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 0, 5, 0, 0, 0, 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void Can_insert_blob_in_big_empty_space()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 20,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 // empty 20 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(12u, freespace[0].Offset);
            Assert.AreEqual(8u, freespace[0].Length);

            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(0u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[] { 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3, 0, 8, 0, 0, 0, 0, 0, 0 }));
        }

        [TestMethod]
        public void Can_insert_blob_but_space_not_at_least_5_bytes_larger()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 14,0,0,0, 0,0,0,0,0,0,0,0,0 // empty 14 bytes entry at beginning of file.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var numberOfFreeSpacesBeforeAct = freespace.Count;
            var numberOfBlobsBeforeAct = index.Count;

            // should generate bytes for type, raw length, and data length.
            var key = instance.InsertBlob(index, freespace, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, numberOfFreeSpacesBeforeAct);
            Assert.AreEqual(0, numberOfBlobsBeforeAct);

            Assert.AreEqual(1, freespace.Count);
            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(14u, freespace[0].Length);

            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(14u, index[key]);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 12, 0, 0, 0, 1, 0, 0, 0, 1, 2, 3 }));
        }

        [TestMethod]
        public void Can_delete_entry_at_beginning_followed_by_empty_entry()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                1, 10,0,0,0, 1,0,0,0, 123, // blob entry in middle.

                0, 5,0,0,0,                // empty 5 bytes entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.DeleteBlob(index, freespace, 1);

            // Assert
            Assert.AreEqual(0, index.Count);
            Assert.AreEqual(1, freespace.Count); // should defrag
            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(15u, freespace[0].Length);

            // GOTCHA!  theory: Looks like empty entry got overwritten instead of the blob entry?
            //          NOTE: walking through the code, I can see its actually the defragment routine?

            // GOTCHA!  theory: Looks like the entry to be updated has the wrong offset? Or not being called?
            //          NOTE: Appears the free space entry doesn't appear to have updated?
            //              theory: Wrong index updated, and later gets deleted?

            //              NOTE: turns out there was confusion between original list, and a sorted one, where we need
            //                    indexes into the original list, not the sorted list that gets thrown away.

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 15,0,0,0, 1,0,0,0, 123,   0, 5,0,0,0 }));
        }

        [TestMethod]
        public void Can_delete_entry_in_middle_of_two_empty_entries()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                // empty 5 bytes entry

                1, 10,0,0,0, 1,0,0,0, 123, // blob entry in middle.

                0, 5,0,0,0,                // empty 5 bytes entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.DeleteBlob(index, freespace, 1);

            // Assert
            Assert.AreEqual(0, index.Count);

            Assert.AreEqual(1, freespace.Count); // should defrag
            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(20u, freespace[0].Length);

            // GOTCHA! observation: Seeing 2 x deletes both pointing at same index??
            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 20,0,0,0,   1, 10,0,0,0, 1,0,0,0, 123,  0, 5,0,0,0 }));
        }

        [TestMethod]
        public void Can_delete_entry_at_end_preceded_by_empty_entry()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                // empty 5 bytes entry

                1, 10,0,0,0, 1,0,0,0, 123, // blob entry in middle.
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.DeleteBlob(index, freespace, 1);

            // Assert
            Assert.AreEqual(0, index.Count);

            Assert.AreEqual(1, freespace.Count); // should defrag
            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(15u, freespace[0].Length);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 15,0,0,0,   1, 10,0,0,0, 1,0,0,0, 123}));
        }

        [TestMethod]
        public void Can_delete_entry_in_middle_of_two_other_entries()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                1, 12,0,0,0, 1,0,0,0, 1,2,3, // blob entry

                1, 12,0,0,0, 2,0,0,0, 4,5,6, // blob entry in middle to delete.

                1, 12,0,0,0, 3,0,0,0, 7,8,9, // blob entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.DeleteBlob(index, freespace, 2);

            // Assert
            Assert.AreEqual(2, index.Count);
            Assert.AreEqual(00u, index[1]);
            Assert.AreEqual(24u, index[3]);

            Assert.AreEqual(1, freespace.Count); // should defrag
            Assert.AreEqual(12u, freespace[0].Offset);
            Assert.AreEqual(12u, freespace[0].Length);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 1, 12,0,0,0, 1,0,0,0, 1,2,3,   0, 12,0,0,0, 2,0,0,0, 4,5,6,   1, 12,0,0,0, 3,0,0,0, 7,8,9}));
        }

        [TestMethod]
        public void Can_delete_entry_at_beginning_followed_by_another_entry()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                1, 10,0,0,0, 1,0,0,0, 123, // blob entry to delete

                1, 10,0,0,0, 2,0,0,0, 234, // blob entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.DeleteBlob(index, freespace, 1);

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(10u, index[2]);

            Assert.AreEqual(1, freespace.Count); // should defrag
            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(10u, freespace[0].Length);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 10,0,0,0, 1,0,0,0, 123,   1, 10,0,0,0, 2,0,0,0, 234}));
        }

        [TestMethod]
        public void Can_read_existing_blob()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                1, 10,0,0,0, 1,0,0,0, 123, // blob entry to delete

                1, 10,0,0,0, 2,0,0,0, 234, // blob entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            var result = instance.ReadBlob(index, 2);

            Assert.AreEqual(10u, result.offset);
            Assert.AreEqual(2u, result.key);

            // Gotcha!  Code things more bytes should be read than actually did.  Read was 1 byte (correct) expected 6? (wrong)
            // theory:  Must be a calculation for number of bytes to read, and if not, there needs to be one! 
            Assert.IsTrue(IEnumerableComparer.AreEqual(result.data, new byte[] { 234 }));
        }

        [TestMethod]
        public void Can_update_blob_with_exact_size_unchanged()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                // empty 5 bytes entry
                1, 10,0,0,0, 1,0,0,0, 123, // blob entry to delete
                0, 5,0,0,0,                // empty 5 bytes entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.UpdateBlob(index, freespace, 1, new byte[] { 234 });

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(5u, index[1]);

            Assert.AreEqual(2, freespace.Count);
            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(05u, freespace[0].Length);
            Assert.AreEqual(15u, freespace[1].Offset);
            Assert.AreEqual(05u, freespace[1].Length);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 5,0,0,0,   1, 10,0,0,0, 1,0,0,0, 234,   0, 5,0,0,0}));
        }

        [TestMethod]
        public void Can_update_blob_with_smaller_content_creating_an_empty_space_entry()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                        // empty 5 bytes entry
                1, 15,0,0,0, 1,0,0,0, 1,2,3,4,5,6, // blob entry to delete
                0, 5,0,0,0,                        // empty 5 bytes entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // GOTCHA! Did find the filestream was pointing to the wrong position.

            // Act
            instance.UpdateBlob(index, freespace, 1, new byte[] { 7 }); // 5 less bytes, so creates an empty space.  But gets defrag'd?

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(5u, index[1]);

            Assert.AreEqual(2, freespace.Count); // defrag'd?
            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(5u, freespace[0].Length);
            Assert.AreEqual(15u, freespace[1].Offset);
            Assert.AreEqual(10u, freespace[1].Length);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 5,0,0,0,   1, 10,0,0,0, 1,0,0,0, 7,   0, 10,0,0,0,   0, 5,0,0,0}));
        }

        [TestMethod]
        public void Can_update_blob_with_larger_content_into_empty_space_of_exact_same_size()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                        // empty 5 bytes entry
                1, 10,0,0,0, 1,0,0,0, 123,         // blob entry to delete
                0, 12,0,0,0, 0,0,0,0,0,0,0         // empty 5 bytes entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // GOTCHA! Found logic error in picking the routine to handle this scenario.

            // Act
            instance.UpdateBlob(index, freespace, 1, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(15u, index[1]);

            Assert.AreEqual(1, freespace.Count); // defrag'd?
            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(15u, freespace[0].Length);

            // GOTCHA!  Wow!  Not an easy one.  defrag shows 1 entry of 27 bytes.  So it seems to include original free space too?
            //          theory: We don't remove the free space entry from the list of free spaces when we fill it with data.
            //          note:  that worked!

            // note: found an error in the data below, where the original blob entry is turned into an empty space entry, and I'd not
            //       accounted for that.  But that is right, and then the defrag simply expands the first entry into this space.

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 15,0,0,0,   0, 10,0,0,0, 1,0,0,0, 123,   1, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }

        [TestMethod]
        public void Can_update_blob_with_larger_content_into_empty_space_create_a_smaller_empty_space_entry()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                          // empty 5 bytes entry
                1, 10,0,0,0, 1,0,0,0, 123,           // blob entry to delete
                0, 17,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0 // empty 5 bytes entry
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.UpdateBlob(index, freespace, 1, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(15u, index[1]);

            Assert.AreEqual(2, freespace.Count); // defrag'd + new one
            Assert.AreEqual(00u, freespace[0].Offset);
            Assert.AreEqual(15u, freespace[0].Length);
            Assert.AreEqual(27u, freespace[1].Offset);
            Assert.AreEqual(05u, freespace[1].Length);

            // GOTCHA!  Either data error below, or found bug. Ahh (35 bytes free space?), same problem as previous test?
            // theory: Do we not delete the free space entry we're filling from the list of free spaces?

            // NOTE: this seems to have fixed it, but because I did a little DRY refactoring in the process, I need to run previous tests
            // to ensure they still work.

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 15,0,0,0,   0, 10,0,0,0, 1,0,0,0, 123,   1, 12,0,0,0, 1,0,0,0, 1,2,3,   0,5,0,0,0 }));
        }

        [TestMethod]
        public void Can_update_blob_with_larger_content_onto_end_of_stream()
        {
            // Arrange
            var fs = new FakeVirtualFileStream(new byte[]
            {
                0, 5,0,0,0,                          // empty 5 bytes entry
                1, 10,0,0,0, 1,0,0,0, 123,           // blob entry to become space entry, and get defrag'd
            });

            var instance = new FileStuff(fs);
            var (index, freespace) = instance.ScanFile();

            // Act
            instance.UpdateBlob(index, freespace, 1, new byte[] { 1, 2, 3 });

            // Assert
            Assert.AreEqual(1, index.Count);
            Assert.AreEqual(15u, index[1]);

            Assert.AreEqual(1, freespace.Count); // defrag'd
            Assert.AreEqual(0u, freespace[0].Offset);
            Assert.AreEqual(15u, freespace[0].Length);

            Assert.IsTrue(IEnumerableComparer.AreEqual(fs.Data, new byte[]
                { 0, 15,0,0,0,   0, 10,0,0,0, 1,0,0,0, 123,   1, 12,0,0,0, 1,0,0,0, 1,2,3 }));
        }
    }
}
