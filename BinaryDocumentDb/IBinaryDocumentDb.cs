using BinaryDocumentDb.IO;
using InFlux;
using System;
using System.Collections.Generic;

namespace BinaryDocumentDb
{
    /// <summary>
    /// Blob CRUD. Repository style interface.
    /// </summary>
    public interface IBinaryDocumentDb : IDisposable
    {
        ExecResponse<uint> Create(byte[] blobData);

        ExecResponse<IReadOnlyList<byte>> Read(uint blobKey);

        ExecResponse Update(uint key, byte[] blobData);

        ExecResponse Delete(uint key);


        QueuedEvent<uint> OnCreated { get; }

        QueuedEvent<uint> OnUpdated { get; }

        QueuedEvent<uint> OnDeleted { get; }
    }
}
