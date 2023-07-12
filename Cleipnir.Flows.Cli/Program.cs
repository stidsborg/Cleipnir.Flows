using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Cleipnir.Flows.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        var root = Path.GetFullPath(@"C:\Repos\Cleipnir.Flows");
        var output = Path.GetFullPath(@".\nugets");

        if (Directory.Exists(output))
            Directory.Delete(output, recursive: true);
        
        Directory.CreateDirectory(output);

        Console.WriteLine($"Root path: {root}");
        Console.WriteLine($"Output path: {output}");
        
        Console.WriteLine("Processing projects: ");
        
        //update version
        foreach (var projectPath in FindAllProjects(root).Where(IsPackageProject))
        {
            Console.WriteLine("Updating package version: " + LeafFolderName(projectPath));
            Console.WriteLine("Project path: " + Path.GetDirectoryName(projectPath)!);
            UpdatePackageVersion(projectPath);
        }

        //compile & publish source generator
        PublishSourceGenerator(@$"{root}\Cleipnir.Flows.SourceGenerator", output);
        
        //pack
        foreach (var projectPath in FindAllProjects(root).Where(IsPackageProject))
        {
            Console.WriteLine("Packing nuget package: " + LeafFolderName(projectPath));
            Console.WriteLine("Project path: " + Path.GetDirectoryName(projectPath)!);
            PackProject(Path.GetDirectoryName(projectPath)!, output);
        }

        //add source generator to core flow nuget packages
        var nugetPackages = Directory.GetFiles(output, "*.nupkg");
        var coreProjectNugetPackage = nugetPackages
            .Single(nugetPackage => Regex.IsMatch(nugetPackage, @"Cleipnir.Flows.\d*.\d*.\d*.nupkg"));
        AddSourceGeneratorToNugetPackage(
            coreProjectNugetPackage,
            @$"{output}\Cleipnir.Flows.SourceGenerator.dll"
        );

        return 0;
    }

    private static IEnumerable<string> FindAllProjects(string path)
    {
        var currPathProjects = Directory.GetFiles(path, "*.csproj");
        var subFolders = Directory.GetDirectories(path);
        return currPathProjects.Concat(subFolders.SelectMany(FindAllProjects)).ToList();
    }

    private static bool IsPackageProject(string path)
        => File.ReadAllText(path).Contains("<Version>");

    private static void UpdatePackageVersion(string path)
    {
        var projectFileContent = File.ReadAllText(path);
        projectFileContent = FindAndIncrementVersion(projectFileContent, path);
        File.WriteAllText(path, projectFileContent);
    }

    private static string FindAndIncrementVersion(string projectFileContent, string path)
    {
        var versionStartsAt = projectFileContent.IndexOf("<Version>", StringComparison.OrdinalIgnoreCase) + "<Version>".Length;
        var versionEndsAt = projectFileContent.IndexOf("</Version>", StringComparison.OrdinalIgnoreCase);
        var currentVersionString = projectFileContent[versionStartsAt..versionEndsAt];
        Console.WriteLine($"Current version: '{currentVersionString}' for project: '{path.Split('\\', '/').Last()}'");
        
        var versionArray = currentVersionString.Split(".").Select(int.Parse).ToArray();
        versionArray[^1]++;
        var newVersionString = string.Join(".", versionArray);

        var newProjectFileContent = projectFileContent.Replace(">" + currentVersionString, ">" + newVersionString);
        return newProjectFileContent;
    }

    private static void PackProject(string projectPath, string outputPath)
    {
        var p = new Process();
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.FileName = @"C:\Program Files\dotnet\dotnet.exe";
        p.StartInfo.WorkingDirectory = projectPath;
        p.StartInfo.Arguments = $"dotnet pack -c Release /p:ContinuousIntegrationBuild=true -o {outputPath}";
        p.Start();

        while (!p.StandardOutput.EndOfStream)
            Console.WriteLine(p.StandardOutput.ReadLine());

        p.WaitForExit();
    }
    
    private static void PublishSourceGenerator(string sourceGeneratorProjectPath, string outputPath)
    {
        //dotnet publish -c Release -o Output
        var p = new Process();
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.FileName = @"C:\Program Files\dotnet\dotnet.exe";
        p.StartInfo.WorkingDirectory = sourceGeneratorProjectPath;
        p.StartInfo.Arguments = $"dotnet publish -c Release -o {outputPath}";
        p.Start();

        while (!p.StandardOutput.EndOfStream)
            Console.WriteLine(p.StandardOutput.ReadLine());

        p.WaitForExit();
    }

    private static string LeafFolderName(string path)
        => Path.GetDirectoryName(path)!.Split('\\').Last();

    private static void Unzip(string zipFilePath, string unzipPath) 
        => ZipFile.ExtractToDirectory(zipFilePath, unzipPath);

    private static void Zip(string folderPath, string zipPath) 
        => ZipFile.CreateFromDirectory(folderPath, zipPath);

    private static void AddSourceGeneratorToNugetPackage(string nugetPath, string sourceGeneratorDllPath)
    {
        using var zipArchive = ZipFile.Open(nugetPath, ZipArchiveMode.Update);

        var fileInfo = new FileInfo(sourceGeneratorDllPath);
        zipArchive.CreateEntryFromFile(sourceGeneratorDllPath, @$"analyzers\dotnet\cs\{fileInfo.Name}");
    }
}