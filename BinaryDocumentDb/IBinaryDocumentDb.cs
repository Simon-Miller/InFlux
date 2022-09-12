using BinaryDocumentDb.IO;
using System.Collections.Generic;

namespace BinaryDocumentDb
{
    /// <summary>
    /// Blob CRUD. Repository style interface.
    /// </summary>
    public interface IBinaryDocumentDb
    {
        ExecResponse<uint> Create(byte[] blobData);

        ExecResponse<IReadOnlyList<byte>> Read(uint blobKey);

        ExecResponse Update(uint key, byte[] blobData);

        ExecResponse Delete(uint key);

    }
}
