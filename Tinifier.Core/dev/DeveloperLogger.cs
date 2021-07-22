using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;

namespace Tinifier.Core.Controllers
{
    public interface IDeveloperLogger
    {
        void WriteLog(string message);
    }

    public class DeveloperLogger : IDeveloperLogger
    {
        IFileSystem _fileSystem;
        private string fileName = "log.txt";

        public DeveloperLogger(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void WriteLog(string message)
        {
            var text1 = "";

            using (Stream oldFileVersion = _fileSystem.OpenFile(fileName))
            {

                var bytes1 = new byte[oldFileVersion.Length];
                oldFileVersion.Read(bytes1, 0, bytes1.Length);

                text1 = Encoding.ASCII.GetString(bytes1);
            }

            var bytes = Encoding.ASCII.GetBytes(message + "\n" + text1);
            using (Stream stream = new MemoryStream(bytes))
            {
                _fileSystem.AddFile(fileName, stream, true);
            }
        }
    }
}
