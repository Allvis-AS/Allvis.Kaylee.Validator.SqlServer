using System;

namespace Allvis.Kaylee.Validator.SqlServer.Writers
{
    public interface IWriter : IDisposable
    {
        void WriteComment(string comment);
        void WriteLine(string line);
    }
}