//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool "nuget:?package=GitVersion.CommandLine"

using Path = System.IO.Path;

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

GitVersion gitVersionInfo;
string nugetVersion;

var pathToLib = "./source/Dgraph-dotnet";
var pathToTests = "./source/Dgraph-dotnet.tests";
var artifactsPath = "./artifacts";
var localPackages = "../../LocalPackages";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////


Setup(ctx =>
{
    EnsureDirectoryExists(artifactsPath);

    // gitVersionInfo = GitVersion(new GitVersionSettings {
    //     OutputType = GitVersionOutput.Json
    // });

    // nugetVersion = gitVersionInfo.NuGetVersion;
    nugetVersion = "0.0.2";

    // Information("Building DgraphDotNet v{0}", nugetVersion);
    // Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});


Teardown(ctx =>
{
    CleanDirectory(artifactsPath);
});


///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////


Task("Clean")
    .Does(() => 
    { 
        CleanDirectory(artifactsPath);

        DotNetCoreClean(".", 
            new DotNetCoreCleanSettings
            {
                Configuration = configuration
            }
        );
    });


Task("Build")
    .IsDependentOn("Clean")
    .Does(() => 
    {
        //ReplaceRegexInFiles("...file name...", "version = \"[^\"]+", "version = \"" + nugetVersion);

        DotNetCoreBuild(".", 
            new DotNetCoreBuildSettings
            {
                Configuration = configuration,
                ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
            });
    });


Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreTest(pathToTests, 
            new DotNetCoreTestSettings
            {
                Configuration = configuration,
                NoBuild = true,
                // ArgumentCustomization = args => args.Append("-l trx")
            });
    });


Task("Publish")
    .IsDependentOn("Test")
    .Does(() => 
    {
        DotNetCorePack(pathToLib, 
            new DotNetCorePackSettings
            {
                Configuration = configuration,
                OutputDirectory = artifactsPath,
                NoBuild = true,
                ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
            });

        // don't clear the local dev version unless everything built and tested ok.
        //int nugteClear = StartProcess("nuget", "delete Dgraph-dotnet " + nugetVersion + " -Source ../../LocalPackages");
        CleanDirectory(Path.Combine(localPackages, "dgraph-dotnet", nugetVersion));

        NuGetAdd(Path.Combine(artifactsPath, $"Dgraph-dotnet.{nugetVersion}.nupkg"), 
            new NuGetAddSettings
            {
                Source = localPackages
            });

    // Not really working, so doing by hand as above
    // DotNetCorePublish(pathToLib, new DotNetCorePublishSettings
    // {
    //     Configuration = configuration,
    //     ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}"),
    //     OutputDirectory = "../../LocalPackages",
    //     // NoBuild = true .... there's currently no NoBuild option so everything gets rebuilt for publish :-(  
    // });
    });


Task("Default")
    .IsDependentOn("Publish");


///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);

// ./build.sh -Target=Clean