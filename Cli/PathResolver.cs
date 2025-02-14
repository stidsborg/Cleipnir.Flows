namespace Cleipnir.Flows.Cli;

public class PathResolver
{
    public static RepoAndDotnetPaths ResolvePaths(OperatingSystem operatingSystem)
    {
        return operatingSystem switch
        {
            OperatingSystem.Windows => 
                new RepoAndDotnetPaths(
                    RepoPath: @"C:\Repos\Cleipnir.Flows",
                    DotnetPath: @"C:\Program Files\dotnet\dotnet.exe"
                ),
            OperatingSystem.Linux => 
                new RepoAndDotnetPaths(
                    RepoPath: $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Repos/Cleipnir.Flows",
                    DotnetPath: "/usr/bin/dotnet"
                ),
            OperatingSystem.MacOs => 
                new RepoAndDotnetPaths(
                    RepoPath: $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Repos/Cleipnir.Flows",
                    DotnetPath: "/usr/local/share/dotnet/dotnet"
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null)
        };
    }
}

public record RepoAndDotnetPaths(string RepoPath, string DotnetPath);
public enum OperatingSystem
{
    Windows,
    Linux,
    MacOs
}