using System.CommandLine;
using Vsix2Zed.Core;

var vsixArg = new Argument<FileInfo>("vsix", "Path to a .vsix file");
var outOption = new Option<DirectoryInfo>(
    name: "--out",
    description: "Output directory for the Zed extension")
{
    IsRequired = true,
};
outOption.AddAlias("-o");
var forceOption = new Option<bool>(
    name: "--force",
    description: "Overwrite non-empty output directory");

var convert = new Command("convert", "Rewrite a VS Code VSIX into a Zed extension scaffold + GAP_REPORT.md");
convert.AddArgument(vsixArg);
convert.AddOption(outOption);
convert.AddOption(forceOption);
convert.SetHandler((FileInfo vsix, DirectoryInfo output, bool force) =>
{
    try
    {
        var result = ConversionPipeline.Run(new ConversionOptions
        {
            VsixPath = vsix.FullName,
            OutputDirectory = output.FullName,
            Force = force,
        });

        Console.WriteLine($"Converted {result.ExtensionId}");
        Console.WriteLine($"Output: {result.OutputDirectory}");
        foreach (var c in result.Converted)
            Console.WriteLine($"  + {c}");
        Console.WriteLine($"Gaps: {result.Gaps.Count} (see GAP_REPORT.md)");
        Environment.ExitCode = result.WroteAnything ? 0 : 2;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}, vsixArg, outOption, forceOption);

var analyze = new Command("analyze", "Classify a VSIX and print JSON (strategy + gaps, no write)");
analyze.AddArgument(vsixArg);
analyze.SetHandler((FileInfo vsix) =>
{
    try
    {
        var report = AnalysisPipeline.Analyze(vsix.FullName);
        Console.WriteLine(AnalysisPipeline.ToJson(report));
        Environment.ExitCode = 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}, vsixArg);

var root = new RootCommand("Vsix2Zed — VS Code VSIX → Zed extension transformer");
root.AddCommand(convert);
root.AddCommand(analyze);
return await root.InvokeAsync(args);
