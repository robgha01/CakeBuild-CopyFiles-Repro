#addin "nuget:?package=NuGet.Core&version=2.12.0"
#addin "Cake.Powershell"
#addin "Cake.Git"
#addin "nuget:?package=RestSharp&version=105.2.3"
#addin "Cake.Json"
#addin "Cake.DocFx"
#addin "Cake.XdtTransform"
#addin "Cake.EnvXmlTransform"
#addin "BlueLeet.Build"
#tool "docfx.msbuild"
#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.2.0"
#reference "Microsoft.CSharp.dll"
#reference "System.dll"
#reference "mscorlib.dll"
#reference "System.Core.dll"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using RestSharp;
using RestSharp.Authenticators;
using NuGet;

//////////////////////////////////////////////////////////////////////
// Objects
//////////////////////////////////////////////////////////////////////
public enum TagUpdate
{
    Build,
    Minor,
    Major,
    Skip
};

//////////////////////////////////////////////////////////////////////
// Settings
//////////////////////////////////////////////////////////////////////

GitVersion packageVersion = null;
dynamic vsoCredentials = null;
dynamic buildConfig = null;
var ConfigTransformPaths = new List<string>();
var InternalsVisibleTo = new List<string>();
var RequireFolders = new List<DirectoryPath>();
var BuildVariations = new List<string>();
var PushGitFiles = new List<string>();
var UnitTests = new List<string>();
var UnitTestEnabled = true;
var UnitTestAutoResolve = true;
var overwriteExistingArtifact = false;
var root = "../";
var VersionUpdate = TagUpdate.Build;
string projectUrl = "https://blueleet.visualstudio.com/Websites";

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var args = GetCakeOptions().ToDictionary(x => x.Key, x => x.Value);
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
string cakeScript = "build.cake";

if(args.ContainsKey("-script"))
{
    cakeScript = args["-script"];
}

var scriptFileName = System.IO.Path.GetFileNameWithoutExtension(MakeAbsolute(File(cakeScript)).FullPath);
var projectName = Argument("projectName", scriptFileName);

if(projectName.Equals("build"))
{
    // This is the old mode wich resolves the package name using its parent directory.
    projectName = ((DirectoryPath)(MakeAbsolute(Directory(root)).FullPath)).GetDirectoryName();
}

var feedName = Argument("feedName", "master");
var tmpDirName = Argument("tmpDirName", "tmp");
var targetOutput = Directory(root) + Directory("./artifacts");
var configFileName = Argument<string>("configFileName", "build.json");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define Paths & Settings
var reletiveSrcDir = new DirectoryPath($"../{projectName}/");
var projectRoot =  MakeAbsolute(Directory(root));
var tmpDir = projectRoot + Directory($"/{tmpDirName}");
var srcDir = projectRoot + Directory($"/{projectName}");
var binDir =  srcDir + Directory("/bin");
var defaultBuildDir = Directory(binDir) + Directory(configuration);
var buildDir = defaultBuildDir;
var vsoFeed = $"https://blueleet.pkgs.visualstudio.com/_packaging/{feedName}/nuget/v3/index.json";
var gcmDownloadUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows/releases/download/v1.4.0/gcm-v1.4.0.zip";
var gcmDir = $"{projectRoot}/.tools/gcm/";
var configFile = projectRoot + File($"Build/{configFileName}");
var gitRoot = GitFindRootFromPath(projectRoot);
var gitRootName = gitRoot.GetDirectoryName();
var solutionFile = projectRoot + File($"/{gitRootName}.sln");

// Settings Overrides
if (HasArgument("targetOutput"))
{
    targetOutput = Directory(Argument<string>("targetOutput"));
}

if (HasArgument("buildDir"))
{
    buildDir = Directory(Argument<string>("buildDir"));
}

//////////////////////////////////////////////////////////////////////
// Functions
//////////////////////////////////////////////////////////////////////
Func<MSBuildSettings, MSBuildSettings> overrideBuildConfigurator = null;
Func<XBuildSettings, XBuildSettings> overrideXBuildConfigurator = null;

public void WithProjectUrl(string url)
{
    projectUrl = url;
}

public void Build(string config)
{
    if(IsRunningOnWindows())
    {
      if(overrideBuildConfigurator != null)
      {
          var setting = new MSBuildSettings();
          setting = setting.SetConfiguration(config);
          var settings = overrideBuildConfigurator.Invoke(setting);
          
          MSBuild(solutionFile, settings);
          return;
      }

      // Use MSBuild
      MSBuild(solutionFile, settings => settings.SetConfiguration(config));
    }
    else
    {
      if(overrideXBuildConfigurator != null)
      {
          var setting = new XBuildSettings();
          setting = setting.SetConfiguration(config);
          var settings = overrideXBuildConfigurator.Invoke(setting);
          XBuild(solutionFile, settings);
          return;
      }

      // Use XBuild
      XBuild(solutionFile, settings => settings.SetConfiguration(config));
    }
}

public void AddConfigTransformPath(string path) {
    ConfigTransformPaths.Add(path);
}

public void AddBuildVariation(string name)
{
    BuildVariations.Add(name);
}

public string From(string path, params string[] paths)
{
    foreach (string section in paths)
    {
        path = System.IO.Path.Combine(path, section);
    }
    return path;
}

public string FromBin(params string[] paths)
{
    return From(binDir, paths);
}

public string FromSrc(params string[] paths)
{
    return From(srcDir, paths);
}

public void AddVisibleTo(string name)
{
    if(!InternalsVisibleTo.Contains(name))
    {
        InternalsVisibleTo.Add(name);
    }
}

public void RequireFolder(DirectoryPath path)
{
    if(!RequireFolders.Contains(path))
    {
        RequireFolders.Add(path);
    }
}

public GitVersion UpdateAssemblyInfo(string assemblyFilePath, string url = "", string branch = "", string username = "", string password = "")
{
    PushGitFiles.Add(assemblyFilePath);

    if(string.IsNullOrEmpty(url))
    {
        url = $"{projectUrl}/_git/{gitRootName}";
    }

    if(string.IsNullOrEmpty(branch))
    {
        branch = "master";
    }

    if(string.IsNullOrEmpty(username))
    {
        username = vsoCredentials.Username;
    }

    if(string.IsNullOrEmpty(password))
    {
        password = vsoCredentials.Password;
    }

    return GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFilePath = assemblyFilePath,
            // UserName = username,
            // Password = password,
            // Url = url,
            // Branch = branch
        });
}

public void AddUnitTest(string name)
{
    UnitTests.Add(name);
}

public void UsingDefaultUnitTestProject()
{
    UnitTestAutoResolve = false;
    AddUnitTest(projectName);
}

//////////////////////////////////////////////////////////////////////
// TASKS Dependency Resolved
//////////////////////////////////////////////////////////////////////

Task("Ensure-BuildConfig")
    .Does(() =>
{
    if(FileExists(configFile) && buildConfig == null)
    {
        buildConfig = DeserializeJsonFromFile<dynamic>(configFile);
    }
});

Task("Ensure-Tag")
    .IsDependentOn("Ensure-Vso-Environment")
    .Does(() =>
{
    if(VersionUpdate == TagUpdate.Skip)
    {
        Information("Skipping tag update as TagUpdate is set to 'Skip'.");
        return;
    }

    var currentVersion = GitDescribe(projectRoot, GitDescribeStrategy.Tags);
    if(!currentVersion.Contains("-"))
    {
        Warning("Building using version '{0}' as no tagless commit found, have you committed your changes?", currentVersion);
        return;
    }

    currentVersion = currentVersion.Split('-')[0];
    var version = Version.Parse(currentVersion);
    string newVersion;

    if(VersionUpdate == TagUpdate.Major)
    {
        newVersion = $"{version.Major + 1}.0.0";
    }
    else if(VersionUpdate == TagUpdate.Minor)
    {
        newVersion = $"{version.Major}.{version.Minor + 1}.0";
    }
    else
    {
        newVersion = $"{version.Major}.{version.Minor}.{version.Build + 1}";
    }

    GitTag(projectRoot, newVersion);
    GitPushRef(projectRoot, (string)vsoCredentials.Username, (string)vsoCredentials.Password, "origin", $"refs/tags/{newVersion}");
})
.OnError(exception =>
{
    if(exception.GetType().FullName.StartsWith("LibGit2Sharp"))
    {
        throw exception;
    }
    Error("An error occurred:'{0}'", exception.GetType().FullName);
    Error(exception.ToString());
});

Task("Ensure-Vso-Environment")
    .Does(() =>
{
    if(vsoCredentials != null)
    {
        return;
    }

    // Ensure we bypass execution policy
    StartPowershellScript("Set-ExecutionPolicy", args =>
    {
        args.Append("Bypass -Scope Process");
    });

    // Ensure we have a user for sending commands to vso feed.
    StartPowershellScript($"{projectRoot}/init.ps1");
    var gcmFile = $"{gcmDir}git-credential-manager.exe";
    if (!FileExists(gcmFile))
    {
        var resource = DownloadFile(gcmDownloadUrl);
        Unzip(resource, gcmDir);
    }

    var result = StartPowershellScript(projectRoot + "/Build/ResolveCredentials.ps1", args =>
    {
        args.Append($"-GcmDir {gcmDir}")
            .Append("-GcmProtocol https")
            .Append("-GcmHost blueleet.visualstudio.com")
            .Append($"-GcmPath Main/_git/{projectName}");
    });

    foreach(var item in result)
    {
        var obj = DeserializeJson<JToken>(item.ToString()) as JObject;
        
        JToken v;
        if(obj != null && obj.TryGetValue("Username", out v))
        {
            vsoCredentials = obj;
			break;
        }
    }   
    
    if(vsoCredentials == null)
    {
        // If we get here someting went wrong with parsing the vso credentials.
        throw new Exception("Someting went wrong with parsing the vso credentials");
    }
});

Task("Ensure-GitVersion")
    .IsDependentOn("Ensure-Vso-Environment")
    .IsDependentOn("Ensure-Tag")
    .Does(() =>
{
    if(packageVersion != null)
    {
        return;
    }

    var assemblyFilePath = $"{projectRoot}/{projectName}/Properties/AssemblyInfo.cs";
    packageVersion = UpdateAssemblyInfo(assemblyFilePath);
});

Task("Ensure-AssemblyInfos-Pushed")
    .Does(() =>
{
    var diffs = GitDiff(gitRoot).Where(x => x.Status == GitChangeKind.Modified && x.Path.EndsWith("AssemblyInfo.cs"));

    foreach(var file in diffs)
    {
        Information("puching:'{0}'", file);
        GitAdd(gitRoot, file.Path);
    }
        
    GitCommit(gitRoot, $"BuildScript", "NoReplay@BuildScript.local", "Puching AssemblyInfo.cs");
    GitPush(gitRoot, (string)vsoCredentials.Username, (string)vsoCredentials.Password);
});

Task("Ensure")
    .IsDependentOn("Ensure-GitVersion");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Cleanup")
    .Does(() =>
{
    // Cleanup any used temps.
    Information("Cleaning up...");

    if(DirectoryExists(buildDir) && buildDir != defaultBuildDir)
    {
        CleanDirectory(buildDir);
    }

    if(DirectoryExists(tmpDir))
    {
        CleanDirectory(tmpDir);
    }

    foreach(var path in RequireFolders)
    {
        if(DirectoryExists(path))
        {
            CleanDirectory(path);
        }
    }

    Information("Done, its all fresh and clean.");
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
    NuGetRestore(solutionFile);
});

Task("Before-Build")
    .Does(() =>
{
    // Ensure that we have all required folders.
    Information("Ensuring that we have all the required folders created for us...");
    
    Information("Ensuring that we have build directory: {0}", buildDir);
    EnsureDirectoryExists(buildDir);
    CleanDirectory(buildDir);

    Information("Ensuring that we have tmp directory: {0}", tmpDir);
    EnsureDirectoryExists(tmpDir);
    CleanDirectory(tmpDir);
    
    Information("Ensuring that we have target output directory: {0}", targetOutput);
    EnsureDirectoryExists(targetOutput);

    foreach(var path in RequireFolders)
    {
        Information("Ensuring that we have: {0}", path);
        EnsureDirectoryExists(path);
        CleanDirectory(path);
    }
    Information("Done, its all good to go!");
});

Task("Build")
    .IsDependentOn("Before-Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(BuildVariations.Any())
    {
        foreach(var variation in BuildVariations)
        {
            Build($"{configuration}-{variation}");
        }
    }
    else
    {
        Build(configuration);
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    if(UnitTestEnabled == false)
    {
        Information("Skipping unit testing as it is disabled by the build script.");
        return;
    }

    var files = new List<FilePath>();
    if(UnitTestAutoResolve == false && UnitTests.Any())
    {
        foreach(var name in UnitTests)
        {
            var unitTestFiles = GetFiles(projectRoot + "/**/bin/" + configuration + $"/{name}.Tests.dll");
            files.AddRange(unitTestFiles);
        }
    }
    else
    {
        // Automaticly search for any unit test files.
        var unitTestFiles = GetFiles(projectRoot + "/**/bin/" + configuration + "/*.Tests.dll");
        files.AddRange(unitTestFiles);
    }

    if(files.Any() == false)
    {
        Information("Skipping unit testing as no unit tests where found.");
        return;
    }

    XUnit2(files, new XUnit2Settings {
        Parallelism = ParallelismOption.All,
        NoAppDomain = false
    });
});

Task("Patch-Assembly-Info")
    .IsDependentOn("Ensure")
    .Does(() =>
{
    var assemblyInfoFile = srcDir + File("/Properties/AssemblyInfo.cs");
    var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile);
    
    CreateAssemblyInfo(assemblyInfoFile, new AssemblyInfoSettings {
        InternalsVisibleTo = InternalsVisibleTo,
        Product = assemblyInfo.Product,
        Version = assemblyInfo.AssemblyVersion,
        FileVersion = assemblyInfo.AssemblyFileVersion,
        InformationalVersion = assemblyInfo.AssemblyInformationalVersion,
        Copyright = assemblyInfo.Copyright,
        CLSCompliant = assemblyInfo.ClsCompliant,
        Company = assemblyInfo.Company,
        ComVisible = assemblyInfo.ComVisible,
        Configuration = assemblyInfo.Configuration,
        Description = assemblyInfo.Description,
        Guid = assemblyInfo.Guid,
        Title = assemblyInfo.Title,
        Trademark = assemblyInfo.Trademark
    });
});

Task("Apply-Config-Transformations")
  .Does(() => {
    foreach(var path in ConfigTransformPaths)
    {
        Information("Transforming '{0}'", path);
        ConfigTransform.ApplyTransformations(path, configuration);
    }    
});

Task("Run-Pre-Build")
  .Does(() => {
      if(Tasks.Any(x => x.Name == "Pre-Build"))
      {
          RunTarget("Pre-Build");
      }
});

Task("Build-Artifact")
    .IsDependentOn("Build-Project")
    .WithCriteria(() => Tasks.Any(x => x.Name == "Build-Project"))
    .Does(() =>
{   
    var version = packageVersion.NuGetVersion;
    var zipFile = File($"{targetOutput}/{projectName}.{version}.zip");
    
    if(FileExists(zipFile))
    {
        if(overwriteExistingArtifact == false)
        {
            Information("Skipping: An artifact already found with the version {0} in '{1}', remove this file before building or set overwriteExistingArtifact to true.", packageVersion.NuGetVersion, zipFile);
            return;
        }
        else
        {
            Information("Artifact already found deleting it: {0}", packageVersion.NuGetVersion);
            DeleteFile(zipFile);
        }    
    }
    
    Zip(tmpDir, zipFile);

    // Cleanup this mess of temp files. 
    RunTarget("Cleanup");
});

Task("Make-Doc")
    .Does(() => {
        var file = projectRoot + File("/build/docfx_project/docfx.json");
        DocFxBuild(file, new DocFxBuildSettings() {
            ToolPath = projectRoot + File("/.tools/docfx.msbuild/tools/docfx.exe"),
            OutputPath = projectRoot + "/artifacts/docs"
        });
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Run-Unit-Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);