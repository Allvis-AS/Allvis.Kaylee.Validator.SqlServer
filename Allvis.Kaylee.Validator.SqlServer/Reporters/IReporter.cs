using Allvis.Kaylee.Analyzer.Models;
using System;

namespace Allvis.Kaylee.Validator.SqlServer.Reporters
{
    public interface IReporter : IDisposable
    {
        void ReportMissingSchema(Schema schema);
        void ReportMissingTable(Entity entity);
        void ReportMissingView(Entity entity);
        void ReportMissingTableColumn(Field field);
        void ReportMissingViewColumn(Field field);
        void ReportMissingUniqueKey(UniqueKey uniqueKey, int index);
        void ReportIncorrectTableColumn(Field field, string hint);
    }
}