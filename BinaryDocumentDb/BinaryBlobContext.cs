using BinaryDocumentDb.IO;
using System;
using System.Collections.Generic;

namespace BinaryDocumentDb
{

    /// <summary>
    /// Externally, instances of this class should happen via either DI, or a Factory class.
    /// </summary>
    internal class BinaryBlobContext : IBinaryDocumentDb
    {
        #region construct / dispose / destruct

        public BinaryBlobContext(BdDbConfig config)
        {
            this.config = config;

            fs = new VirtualFileStream(config.FilePathAndName);

            FileOperations = new FileStuff(fs);

            (KeyToOffsetDictionary, FreeSpaceEntries) = FileOperations.ScanFile();
        }

        private bool isDisposed = false;

        public void Dispose()
        {
            if (isDisposed == false)
            {
                fs.Close();
            }
        }


        ~BinaryBlobContext()
        {
            Dispose();
        }

        #endregion

        private readonly BdDbConfig config;
        private readonly IVirtualFileStream fs;
        private readonly FileStuff FileOperations;

        private readonly Dictionary<uint, uint> KeyToOffsetDictionary;
        private readonly List<FreeSpaceEntry> FreeSpaceEntries;


        public ExecResponse<uint> Create(byte[] blobData) =>
            TryCatch.Wrap(() =>
                FileOperations.InsertBlob(KeyToOffsetDictionary, FreeSpaceEntries, blobData));

        public ExecResponse<IReadOnlyList<byte>> Read(uint blobKey) =>
            TryCatch.Wrap(() => 
                FileOperations.ReadBlob(KeyToOffsetDictionary, blobKey).Data);

        public ExecResponse Update(uint key, byte[] blobData) =>
            TryCatch.Wrap(()=>
                FileOperations.UpdateBlob(KeyToOffsetDictionary, FreeSpaceEntries, key, blobData));

        public ExecResponse Delete(uint key) =>
            TryCatch.Wrap(() =>
                FileOperations.DeleteBlob(KeyToOffsetDictionary, FreeSpaceEntries, key));
    }
}
