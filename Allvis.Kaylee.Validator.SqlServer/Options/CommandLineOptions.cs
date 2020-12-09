using CommandLine;

namespace Allvis.Kaylee.Validator.SqlServer.Options
{
    public class CommandLineOptions
    {
        [Option(
            'd',
            "dir",
            Required = false,
            HelpText = "The directory where the *.kay files reside. If not specified, defaults to the current directory.")]
        public string Directory { get; set; } = string.Empty;

        [Option(
            'c',
            "connstr",
            Required = true,
            HelpText = "The connection string of the database to connect to.")]
        public string ConnectionString { get; set; } = string.Empty;

        [Option(
            'o',
            "out",
            Required = false,
            HelpText = "The file to write the output to (throws an error if it already exists). If not specified, outputs to the console.")]
        public string OutFile { get; set; } = string.Empty;

        [Option(
            't',
            "timeout",
            Default = 30,
            Required = false,
            HelpText = "The timeout for connecting to the database.")]
        public int Timeout { get; set; }
    }
}
