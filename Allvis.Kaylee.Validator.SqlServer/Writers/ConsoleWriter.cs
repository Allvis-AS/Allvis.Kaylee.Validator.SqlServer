using System;

namespace Allvis.Kaylee.Validator.SqlServer.Writers
{
    public class ConsoleWriter : IWriter
    {
        public void WriteComment(string comment)
        {
            SetColorComment();
            Console.WriteLine(comment);
            Console.ResetColor();
        }

        private static void SetColorComment()
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        public void WriteLine(string line)
        {
            SetColorLine();
            Console.WriteLine(line);
            Console.ResetColor();
        }

        private static void SetColorLine()
        {
            Console.ResetColor();
        }

        #region IDisposable support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
