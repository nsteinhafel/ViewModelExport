using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using ViewModelExport.Services;

namespace ViewModelExport;

public class Program
{
    [Required]
    [Option(ShortName = "m", LongName = "model")]
    public string[]? Models { get; set; }

    [Required]
    [FileOrDirectoryExists]
    [Option(ShortName = "i", LongName = "input-dir")]
    public string? InputDir { get; set; }

    [Required]
    [FileOrDirectoryExists]
    [Option(ShortName = "o", LongName = "output-dir")]
    public string? OutputDir { get; set; }

    public static async Task<int> Main(string[] args)
    {
        return await CommandLineApplication.ExecuteAsync<Program>(args);
    }

    private async Task OnExecuteAsync()
    {
        if (Models == null || InputDir == null || OutputDir == null) return;

        var exporter = new ModelExporter(Models.ToHashSet(), InputDir, OutputDir);
        await exporter.ExportAsync();
    }
}