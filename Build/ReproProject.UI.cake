using System.Text.RegularExpressions;

DirectoryPath ClientUIPath;
DirectoryPath ClientUIBuildPath;
DirectoryPath BuildOutput;
var files = new Dictionary<FilePath, DirectoryPath>();
var scriptFileName = "ReproProject.UI";

//////////////////////////////////////////////////////////////////////
// Arguments
//////////////////////////////////////////////////////////////////////
var projectName = Argument("projectName", scriptFileName);
var tmpDirName = Argument("tmpDirName", "tmp");
var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// Variables
//////////////////////////////////////////////////////////////////////
var root = "../";
var projectRoot =  MakeAbsolute(Directory(root));
var targetOutput = Directory(root) + Directory("./artifacts");
var tmpDir = projectRoot + Directory($"/{tmpDirName}");
var srcDir = projectRoot + Directory($"/{projectName}");

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Build-Artifact")
    .Does(() =>
{
    // Ensure that we have all required folders.
    Information("Ensuring that we have all the required folders created for us...");

    Information("Ensuring that we have tmp directory: {0}", tmpDir);
    EnsureDirectoryExists(tmpDir);
    CleanDirectory(tmpDir);
    
    Information("Ensuring that we have target output directory: {0}", targetOutput);
    EnsureDirectoryExists(targetOutput);

    // Include other webFiles.
    var webFiles = GetFiles($"{srcDir}/Umbraco/**/*.*") +
    GetFiles($"{srcDir}/Umbraco_Client/**/*.*");

    // This will include all web files.
    CopyFiles(webFiles, tmpDir, true);

    //Console.WriteLine("Look in the tmp folder located at the ReproProj folder:");
    //string line = Console.ReadLine();
});

RunTarget(target);