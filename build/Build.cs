#nullable enable
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.Tools.SonarScanner;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.SonarScanner.SonarScannerTasks;
using static System.Environment;
using ParameterAttribute = Nuke.Common.ParameterAttribute;

#pragma warning disable S1144   // Unused private types or members should be removed

class Build : NukeBuild
{
    [Solution] Solution Solution;
    [Parameter] string Configuration { get; set; } = "Release";

    static AbsolutePath SourceDirectory => RootDirectory;
    static AbsolutePath ReportTaskPath => SourceDirectory / "report-task.txt";
    static AbsolutePath NugetConfig => SourceDirectory / "NuGet.Config";

    static string ProjectVersion => GetEnvironmentVariable("BUILD_LABEL") ?? GetEnvironmentVariable("APP_VERSION");

    static string InformationalVersion => $"{ProjectVersion}-{GetEnvironmentVariable("GIT_COMMIT")}";
    const string Copyright = "Unlicense.org";
    const string ProductDescription = "Application Template";

    static string SonarProject = GetEnvironmentVariable("PROJECT_NAME") ?? "dotnet-application";
    static string SonarOrganization = GetEnvironmentVariable("SONAR_ORG") ?? "Local";

    private const string SonarCoverageOpenCover = "**/*.opencover.xml";
    private const string SonarUnitTestReports = "**/*UnitTests.xml";

    Target Clean => _ => _
        .Executes(() =>
        {
            DotNetClean(settings => settings
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosity.minimal));

            SourceDirectory
                .GlobFiles("**/*.zip", SonarUnitTestReports, SonarCoverageOpenCover)
                .DeleteFiles();

            RootDirectory // InternalTrace*.log and nunit-agent*.log files
                .GlobFiles("*.log")
                .DeleteFiles();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(settings => settings
                .SetProjectFile(Solution)
                .SetConfigFile(NugetConfig));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(settings => settings
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosity.minimal)
                .SetFileVersion(ProjectVersion)
                .SetAssemblyVersion(ProjectVersion)
                .SetInformationalVersion(InformationalVersion)
                .SetCopyright(Copyright)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(settings => settings
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetResultsDirectory(SourceDirectory)
                .SetRunSetting("NUnit.TestOutputXml", SourceDirectory)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target Publish => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            const string PublishDirName = "publish";

            var configFilesToCopy = new Dictionary<string, string[]>
            {
                ["ApplicationTemplate"] =
                [
                    "appsettings.json",
                ]
            };

            foreach (string projectName in new[] { "ApplicationTemplate" })
            {
                DotNetPublish(settings => settings
                    .SetProject(Solution.GetProject(projectName))
                    .SetConfiguration(Configuration)
                    .SetOutput(SourceDirectory / projectName / PublishDirName)
                    .SetVerbosity(DotNetVerbosity.minimal)
                    .SetFileVersion(ProjectVersion)
                    .SetAssemblyVersion(ProjectVersion)
                    .SetInformationalVersion(InformationalVersion)
                    .SetCopyright(Copyright)
                    .SetDescription(ProductDescription)
                    .EnableNoBuild()
                    .EnableNoRestore());

                Project project = Solution.GetProject(projectName)!;

                foreach (string file in configFilesToCopy[projectName])
                {
                    AbsolutePath sourceFile = project.Directory / file;
                    AbsolutePath targetDirectory = project.Directory / PublishDirName;

                    sourceFile.CopyToDirectory(targetDirectory, ExistsPolicy.FileSkip);
                }
            }
        });

    Target Zip => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            foreach (string projectName in new[] { "ApplicationTemplate" })
                ZipFiles(projectName, targetPath: SourceDirectory / projectName / "publish", patterns: "**/*");

            static void ZipFiles(string projectName, AbsolutePath targetPath, params string[] patterns)
            {
                AbsolutePath archiveFile = targetPath / $"{projectName.ToLower()}_{ProjectVersion}.zip";
                archiveFile.DeleteFile();

                HashSet<AbsolutePath> filesToZip = targetPath.GlobFiles(patterns).ToHashSet();
                targetPath.ZipTo(archiveFile, path => filesToZip.Contains(path));
            }
        });

    Target SonarQubeBegin => _ => _
        .Executes(() =>
        {
            SonarScannerBegin(settings => settings
                .SetProjectKey(SonarProject)
                .SetOrganization(SonarOrganization)
                .SetOpenCoverPaths(SonarCoverageOpenCover)
                .SetNUnitTestReports(SonarUnitTestReports)
                .SetProcessAdditionalArguments(
                    $"/d:sonar.scanner.metadataFilePath={ReportTaskPath}",
                    $"/d:sonar.scanner.scanAll=false")
                .SetVersion(ProjectVersion));
        });

   Target SonarQubeEnd => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            SonarScannerEnd();
        });

    public static void Main() => Execute<Build>(x => x.Zip);
}
