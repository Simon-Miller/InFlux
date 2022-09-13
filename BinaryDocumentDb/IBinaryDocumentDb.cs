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
        /// <summary>
        /// Create / Insert a record of the <paramref name="blobData"/> in your store.
        /// Fires the <see cref="OnCreated"/> event after the operation completes.
        /// Returns an <see cref="ExecResponse{T}"/> detailing the result of this operation.
        /// The <see cref="ExecResponse{T}.Result"/> will be the KEY you need to retrieve this record in future.
        /// </summary>
        ExecResponse<uint> Create(byte[] blobData);

        /// <summary>
        /// Retrieves the Blob you previously stored, by using the <paramref name="blobKey"/> (KEY) you were given.
        /// Returns an <see cref="ExecResponse{T}"/> detailing the result of this operation. 
        /// </summary>
        ExecResponse<IReadOnlyList<byte>> Read(uint blobKey);

        /// <summary>
        /// Searches for the provided <paramref name="blobKey"/>.
        /// returns an <see cref="ExecResponse{T}"/> where the <see cref="ExecResponse{T}.Result"/> holds the result
        /// of the search.  DON'T CONFUSE THIS WITH THE Success PROPERTY!
        /// </summary>
        ExecResponse<bool> Exists(uint blobKey);

        /// <summary>
        /// Updates an existing record with new data (<paramref name="blobData"/>) which separately you provide
        /// the KEY (<paramref name="key"/>) for.
        /// Fires the <see cref="OnUpdated"/> event after the operation completes.
        /// Returns an <see cref="ExecResponse{T}"/> detailing the result of this operation. 
        /// </summary>
        ExecResponse Update(uint key, byte[] blobData);

        /// <summary>
        /// Deletes a Blob from the underlying store identified by tkey KEY (<paramref name="key"/>) you provide.
        /// Fires the <see cref="OnDeleted"/> event after the operation completes.
        /// Returns an <see cref="ExecResponse{T}"/> detailing the result of this operation. 
        /// </summary>
        ExecResponse Delete(uint key);

        /// <summary>
        /// Fired when a Blob is created in the underlying repository.
        /// </summary>
        QueuedEvent<uint> OnCreated { get; }

        /// <summary>
        /// Fired when a Blob is updated in the underlying repository.
        /// </summary>
        QueuedEvent<uint> OnUpdated { get; }

        /// <summary>
        /// Fired when a Blob is Delete from the underlying repository.
        /// </summary>
        QueuedEvent<uint> OnDeleted { get; }
    }
}
