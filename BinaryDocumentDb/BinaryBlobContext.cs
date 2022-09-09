using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BinaryDocumentDb
{
    internal class BinaryBlobContext : IBinaryDocumentDb
    {
        #region construct / dispose / destruct

        public BinaryBlobContext(BdDbConfig config)
        {
            this.config = config;

            this.fs = new VirtualFileStream(config.FilePathAndName);

            this.FileOperations = new FileStuff(this.fs);

            this.FileOperations.ScanFile();
        }

        private bool isDisposed = false;

        public void Dispose()
        {
            if (this.isDisposed == false)
            {
                this.fs.Close();
            }
        }

        ~BinaryBlobContext()
        {
            this.Dispose();
        }

        #endregion


        private readonly BdDbConfig config;
        private readonly IVirtualFileStream fs;

        private readonly FileStuff FileOperations;

    }
}
