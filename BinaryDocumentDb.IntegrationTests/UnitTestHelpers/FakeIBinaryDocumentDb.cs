using BinaryDocumentDb.IO;
using InFlux;

namespace BinaryDocumentDb.IntegrationTests.UnitTestHelpers
{
    internal class FakeIBinaryDocumentDb : IBinaryDocumentDb
    {
        private uint nextKey = 1;
        internal readonly Dictionary<uint, uint> fakeKeyToOffset = new Dictionary<uint, uint>();
        internal readonly Dictionary<uint, byte[]> blobsByOffset = new Dictionary<uint, byte[]>();

        public ExecResponse<uint> ReserveNextKey()
        {
            return new ExecResponse<uint>() { Result = nextKey++ };
        }

        public ExecResponse Create(uint reservedKey, byte[] blobData)
        {
            fakeKeyToOffset.Add(reservedKey, reservedKey);
            blobsByOffset.Add(reservedKey, blobData);

            OnCreated.FireEvent(0, reservedKey);

            return new ExecResponse<uint>
            {
                Success = true,
                Result = reservedKey
            };
        }

        public ExecResponse<uint> Create(byte[] blobData)
        {
            var key = nextKey;

            fakeKeyToOffset.Add(nextKey++, key);
            blobsByOffset.Add(key, blobData);

            OnCreated.FireEvent(0, key);

            return new ExecResponse<uint>
            {
                Success = true,
                Result = key
            };
        }

        public ExecResponse Delete(uint key)
        {
            blobsByOffset.Remove(key);
            fakeKeyToOffset.Remove(key);

            OnDeleted.FireEvent(0, key);
            
            return new ExecResponse
            {
                Success = true
            };
        }

        public void Dispose()
        {
        }

        public ExecResponse<bool> Exists(uint blobKey)
        {
            var success = fakeKeyToOffset.ContainsKey(blobKey);
            return new ExecResponse<bool>
            {
                Success = success,
                ErrorCode = success? 0 : 13
            };
        }

        public ExecResponse<IReadOnlyList<byte>> Read(uint blobKey)
        {
            return new ExecResponse<IReadOnlyList<byte>> 
            {
                Success = true, 
                Result = blobsByOffset[blobKey] 
            };
        }

        public ExecResponse Update(uint key, byte[] blobData)
        {
            blobsByOffset[key] = blobData;

            OnUpdated.FireEvent(0, key);

            return new ExecResponse
            {
                Success = true
            };
        }

        public ExecResponse<IEnumerable<uint>> CacheKeys() =>
            new ExecResponse<IEnumerable<uint>> 
            { 
                Success = true, 
                Result = fakeKeyToOffset.Keys.Select(x=>x) 
            };

        public ExecResponse Flush()
        {
            return new ExecResponse { Success= true };
        }

        public QueuedEvent<uint> OnCreated { get; private set; } = new QueuedEvent<uint>();
        public QueuedEvent<uint> OnUpdated { get; private set; } = new QueuedEvent<uint>();
        public QueuedEvent<uint> OnDeleted { get; private set; } = new QueuedEvent<uint>();
    }
}
