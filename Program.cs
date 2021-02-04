using System.ComponentModel.DataAnnotations;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using ViewModelExport.Services;

namespace ViewModelExport
{
    public class Program
    {
        [Required]
        [Option(ShortName = "m", LongName = "model")]
        public string[]? Models { get; }

        [Required]
        [FileOrDirectoryExists]
        [Option(ShortName = "i", LongName = "input-dir")]
        public string? InputDir { get; }

        [Required]
        [FileOrDirectoryExists]
        [Option(ShortName = "o", LongName = "output-dir")]
        public string? OutputDir { get; }

        public static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Program>(args);
        }

        private void OnExecute()
        {
            if (Models == null || InputDir == null || OutputDir == null) return;

            var exporter = new ModelExporter(Models.ToHashSet(), InputDir, OutputDir);
            exporter.Export();
        }
    }
}