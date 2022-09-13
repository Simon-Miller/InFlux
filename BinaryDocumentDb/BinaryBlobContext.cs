using BinaryDocumentDb.IO;
using InFlux;
using System.Collections.Generic;

namespace BinaryDocumentDb
{
    /// <summary>
    /// Externally, instances of this class should happen via either DI, or a Factory class.
    /// They should expose this through the public <see cref="IBinaryDocumentDb"/> interface.
    /// Note the lack of inline documentation? You can find inline documentation in the interface 
    /// </summary>
    internal class BinaryBlobContext : IBinaryDocumentDb
    {
        #region construct / dispose / destruct

        public BinaryBlobContext(BdDbConfig config) 
            : this(new VirtualFileStream(config.FilePathAndName))
        {
        }

        /// <summary>
        /// this constructor for unit testing only.
        /// </summary>
        internal BinaryBlobContext(IVirtualFileStream fs)
        {
            this.fs = fs;
            FileOperations = new FileStuff(fs);

            (KeyToOffsetDictionary, FreeSpaceEntries) = FileOperations.ScanFile();
        }

        private bool isDisposed = false;

        public void Dispose()
        {
            if (isDisposed == false)
            {
                isDisposed = true;
                fs?.Close(); // GOTCHA!  A failed instantiation will try and call Close when there is no instance of fs. 
            }
        }


        ~BinaryBlobContext()
        {
            Dispose();
        }

        #endregion

        private readonly IVirtualFileStream fs;
        private readonly FileStuff FileOperations;

        private readonly Dictionary<uint, uint> KeyToOffsetDictionary;
        private readonly List<FreeSpaceEntry> FreeSpaceEntries;

        public QueuedEvent<uint> OnCreated { get; private set; } = new QueuedEvent<uint>();

        public QueuedEvent<uint> OnUpdated { get; private set; } = new QueuedEvent<uint>();

        public QueuedEvent<uint> OnDeleted { get; private set; } = new QueuedEvent<uint>();

        public ExecResponse<uint> Create(byte[] blobData) =>
            TryCatch.Wrap(() =>
            {
                var result = FileOperations.InsertBlob(KeyToOffsetDictionary, FreeSpaceEntries, blobData);
                OnCreated.FireEvent(0, result);
                return result;
            });

        public ExecResponse<IReadOnlyList<byte>> Read(uint blobKey) =>
            TryCatch.Wrap(() =>
                FileOperations.ReadBlob(KeyToOffsetDictionary, blobKey).Data);

        public ExecResponse Update(uint key, byte[] blobData) =>
            TryCatch.Wrap(() =>
            {
                FileOperations.UpdateBlob(KeyToOffsetDictionary, FreeSpaceEntries, key, blobData);
                OnUpdated.FireEvent(0, key);
            });

        public ExecResponse Delete(uint key) =>
             TryCatch.Wrap(() =>
             {
                 FileOperations.DeleteBlob(KeyToOffsetDictionary, FreeSpaceEntries, key);
                 OnDeleted.FireEvent(0, key);
             });

        public ExecResponse<bool> Exists(uint blobKey) =>
            TryCatch.Wrap(() => KeyToOffsetDictionary.ContainsKey(blobKey));
    }
}
