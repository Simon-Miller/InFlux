using System;

namespace BinaryDocumentDb
{
    public class BdDbConfig
    {
        /// <summary>
        /// If no document exists, one will be created, and should consume an initial amount fo space to save data.
        /// </summary>
        public int StartingFileSize { get; set; } = 4096; // size of a block on disk.

        /// <summary>
        /// When there is no free space, this will be the excess percentage the database should have available, after growing
        /// enough to allow for the new data to be stored.
        /// </summary>
        public float FileGrowthPercentage { get; set; } = 0.25f;

        /// <summary>
        /// name of data file on disk to either open or create.
        /// </summary>
        public string FilePathAndName { get; set; } = "binaryDb.bin";

        /// <summary>
        /// If a file is created, then the index for blobs is presumed to be this length.
        /// </summary>
        public ushort InitialIndexLength { get; set; } = 10;
    }
}
