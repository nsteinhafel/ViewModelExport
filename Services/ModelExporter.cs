using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EnsureThat;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ViewModelExport.Writers;

namespace ViewModelExport.Services
{
    public class ModelExporter
    {
        private static readonly ICollection<string> FilePaths = new List<string>();

        private readonly string _inputDir;
        private readonly ISet<string> _models;

        private readonly string _outputDir;

        public ModelExporter(ISet<string> models, string inputDir, string outputDir)
        {
            _models = EnsureArg.HasItems(models);
            _inputDir = EnsureArg.IsNotEmpty(inputDir);
            _outputDir = EnsureArg.IsNotEmpty(outputDir);
        }

        public void Export()
        {
            var dir = new DirectoryInfo(_inputDir);
            Search(dir);

            // Iteratively build the list of required types and their associated syntax trees.
            var requiredTypes = new HashSet<string>(_models);
            var trees = new List<SyntaxTree>();
            var treeFiles = new HashSet<string>();
            int startCount;
            do
            {
                // Set the starting count of our class definitions.
                startCount = trees.Count;

                // Only process files we haven't already included.
                foreach (var file in FilePaths.Where(x => !treeFiles.Contains(x)))
                {
                    var model = File.ReadAllText(file);

                    // Parse with Roslyn.
                    var tree = CSharpSyntaxTree.ParseText(model);

                    var classNameVisitor = new ClassNameVisitor(requiredTypes);
                    classNameVisitor.Visit(tree.GetCompilationUnitRoot());

                    if (!classNameVisitor.RequiredTypes.Any()) continue;

                    // Add required types into our set.
                    foreach (var requiredType in classNameVisitor.RequiredTypes)
                    {
                        requiredTypes.Add(requiredType);
                    }

                    trees.Add(tree);
                    treeFiles.Add(file);
                }
            } while (trees.Count > startCount);
            // If we added any class definitions, iterate again.

            // Trusted platform assemblies are all framework and NuGet assemblies.
            // We use all here but we could export from project definitions in the future.
            const string trustedPlatformAssemblies = "TRUSTED_PLATFORM_ASSEMBLIES";
            var trustedAssembliesPaths =
                ((string?) AppContext.GetData(trustedPlatformAssemblies))?.Split(Path.PathSeparator);
            if (trustedAssembliesPaths == null)
                throw new InvalidOperationException($"Could not find paths for '{trustedPlatformAssemblies}'.");

            var references = trustedAssembliesPaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            // Make a new compilation unit with our gathered syntax trees and references.
            var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString("N"), trees, references, options);

            Assembly generatedAssembly;
            using (var ms = new MemoryStream())
            {
                // Compile in memory.
                var result = compilation.Emit(ms);

                // If not successful, print errors and quit.
                if (!result.Success)
                {
                    foreach (var error in result.Diagnostics)
                    {
                        Console.WriteLine(error.GetMessage());
                    }

                    return;
                }

                // Load into memory.
                generatedAssembly = Assembly.Load(ms.ToArray());
            }

            // For now just write into a big file.
            var sb = new StringBuilder(Environment.NewLine);
            var tsWriter = (IWriter) new TypeScriptWriter();
            foreach (var type in generatedAssembly.GetTypes())
            {
                sb.AppendLine(tsWriter.Process(type));
            }

            File.WriteAllText(Path.Join(_outputDir, Path.ChangeExtension("SharedModels", tsWriter.Extension)),
                sb.ToString());
        }

        /// <summary>Recursively gather file paths from the given root.</summary>
        /// <param name="root">Root directory.</param>
        private static void Search(DirectoryInfo root)
        {
            EnsureArg.IsNotNull(root, nameof(root));

            // Add file paths.
            foreach (var file in root.GetFiles())
            {
                FilePaths.Add(file.FullName);
            }

            // Recurse.
            foreach (var dir in root.GetDirectories())
            {
                Search(dir);
            }
        }
    }
}