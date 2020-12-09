namespace Allvis.Kaylee.Validator.SqlServer.Reporters
{
    public interface IReporter
    {
        void ReportMissingSchema(Analyzer.Models.Schema schema);
        void ReportMissingTable(Analyzer.Models.Entity entity);
        void ReportMissingView(Analyzer.Models.Entity entity);
        void ReportMissingTableColumn(Analyzer.Models.Field field);
        void ReportMissingViewColumn(Analyzer.Models.Field field);
    }
}