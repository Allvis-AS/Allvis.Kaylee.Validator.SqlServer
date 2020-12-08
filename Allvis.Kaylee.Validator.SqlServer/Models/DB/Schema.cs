using System.Collections.Generic;

namespace Allvis.Kaylee.Validator.SqlServer.Models.DB
{
    public class Schema
    {
        public string Name { get; set; } = string.Empty;

        public List<Table> Tables { get; } = new List<Table>();
    }
}
