using Allvis.Kaylee.Analyzer.Models;
using System;

namespace Allvis.Kaylee.Validator.SqlServer.Reporters
{
    public interface IReporter : IDisposable
    {
        void ReportMissingSchema(Schema schema);
        void ReportMissingTable(Entity entity);
        void ReportMissingView(Entity entity);
        void ReportMissingTableColumn(Entity entity, Field field);
        void ReportMissingViewColumn(Field field);
        void ReportIncorrectTableColumn(Entity entity, Field field, string hint);
        void ReportMissingUniqueKey(UniqueKey uniqueKey, int index);
        void ReportMissingParentForeignKey(Entity entity, int index);
        void ReportMissingForeignKey(Reference reference, int index);
    }
}