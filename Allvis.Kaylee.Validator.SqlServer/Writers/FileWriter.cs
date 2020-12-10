using System;
using System.IO;
using System.Text;

namespace Allvis.Kaylee.Validator.SqlServer.Writers
{
    public class FileWriter : IWriter
    {
        private readonly TextWriter writer;

        public FileWriter(string file)
        {
            writer = new StreamWriter(File.Open(file, FileMode.CreateNew), Encoding.UTF8, leaveOpen: false);
        }

        public void WriteComment(string comment)
        {
            writer.WriteLine($"-- {comment}");
        }


        public void WriteLine(string line)
        {
            writer.WriteLine(line);
        }

        #region IDisposable support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
