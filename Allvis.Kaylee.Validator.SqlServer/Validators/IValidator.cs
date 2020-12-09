using System.Collections.Generic;
using Allvis.Kaylee.Validator.SqlServer.Models.DB;

namespace Allvis.Kaylee.Validator.SqlServer.Validators
{
    public interface IValidator
    {
        int Issues { get; }

        void Validate(Analyzer.Models.Ast expected, IEnumerable<Schema> actual);
    }
}