#tool nuget:?package=NUnit.ConsoleRunner&version=3.7.0

//////////////////////////////////////////////////////////////////////
// PROJECT-SPECIFIC
//////////////////////////////////////////////////////////////////////

// When copying the script to support a different extension, the
// main changes needed should be in this section.

var SOLUTION_FILE = "nunit-tm4j-result-writer.sln";
var UNIT_TEST_ASSEMBLY = "nunit-tm4j-result-writer.tests.dll";
var GITHUB_SITE = "https://github.com/Ganshorn-Medizin-Electronic/nunit-tm4j-result-writer";
var README = "https://github.com/Ganshorn-Medizin-Electronic/nunit-tm4j-result-writer/blob/master/README.md";
var NUGET_ID = "NUnit.Extension.TM4JResultWriter";
var VERSION = "0.4.0";

// Metadata used in the nuget and chocolatey packages
var TITLE = "NUnit 3 - NUnit TM4J Result Writer Extension";
var AUTHORS = new [] { "Ganshorn Medizin Electronic GmbH" };
var OWNERS = new [] { "Ganshorn Medizin Electronic GmbH" };
var DESCRIPTION = "This extension allows NUnit to create result files in JSON formats for sending via the Test Management for Jira REST API.";
var SUMMARY = "NUnit Engine extension for writing test result files in JSON formats for sending via the Test Management for Jira REST API.";
var COPYRIGHT = "Copyright (c) 2020 Ganshorn Medizin Electronic GmbH";
//var RELEASE_NOTES = new [] { "See ..../CHANGES.txt" };
var RELEASE_NOTES = new [] { "https://github.com/Ganshorn-Medizin-Electronic/nunit-tm4j-result-writer/blob/master/README.md" };
var TAGS = new [] { "nunit", "test", "testing", "tdd", "runner" };

////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

// Special (optional) arguments for the script. You pass these
// through the Cake bootscrap script via the -ScriptArgs argument
// for example: 
//   ./build.ps1 -t RePackageNuget -ScriptArgs --nugetVersion="3.9.9"
//   ./build.ps1 -t RePackageNuget -ScriptArgs '--binaries="rel3.9.9" --nugetVersion="3.9.9"'
var nugetVersion = Argument("nugetVersion", (string)null);
var chocoVersion = Argument("chocoVersion", (string)null);
var binaries = Argument("binaries", (string)null);

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = VERSION + dbgSuffix;

if (BuildSystem.IsRunningOnAppVeyor)
{
	var tag = AppVeyor.Environment.Repository.Tag;

	if (tag.IsTag)
	{
		packageVersion = tag.Name;
	}
	else
	{
		var buildNumber = AppVeyor.Environment.Build.Number.ToString("00000");
		var branch = AppVeyor.Environment.Repository.Branch;
		var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

		if (branch == "master" && !isPullRequest)
		{
			packageVersion = VERSION + "-dev-" + buildNumber + dbgSuffix;
		}
		else
		{
			var suffix = "-ci-" + buildNumber + dbgSuffix;

			if (isPullRequest)
				suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
			else
				suffix += "-" + branch;

			// Nuget limits "special version part" to 20 chars. Add one for the hyphen.
			if (suffix.Length > 21)
				suffix = suffix.Substring(0, 21);

                        suffix = suffix.Replace(".", "");

			packageVersion = VERSION + suffix;
		}
	}

	AppVeyor.UpdateBuildVersion(packageVersion);
}

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

// Directories
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var BIN_SRC = BIN_DIR; // Source of binaries used in packaging
var OUTPUT_DIR = PROJECT_DIR + "output/";

// Adjust BIN_SRC if --binaries option was given
if (binaries != null)
{
	BIN_SRC = binaries;
	if (!System.IO.Path.IsPathRooted(binaries))
	{
		BIN_SRC = PROJECT_DIR + binaries;
		if (!BIN_SRC.EndsWith("/"))
			BIN_SRC += "/";
	}
}

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
	{
		"https://www.nuget.org/api/v2",
		"https://www.myget.org/F/nunit/api/v2"
	};

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(BIN_DIR);
});


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("NuGetRestore")
    .Does(() =>
{
    NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings()
	{
		Source = PACKAGE_SOURCE
	});
});

//////////////////////////////////////////////////////////////////////
// BUILD
//////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("NuGetRestore")
    .Does(() => 
	{
		if (binaries != null)
		    throw new Exception("The --binaries option may only be specified when re-packaging an existing build.");

		if(IsRunningOnWindows())
		{
			MSBuild(SOLUTION_FILE, new MSBuildSettings()
				.SetConfiguration(configuration)
				.SetMSBuildPlatform(MSBuildPlatform.Automatic)
				.SetVerbosity(Verbosity.Minimal)
				.SetNodeReuse(false)
				.SetPlatformTarget(PlatformTarget.MSIL)
			);
		}
		else
		{
			XBuild(SOLUTION_FILE, new XBuildSettings()
				.WithTarget("Build")
				.WithProperty("Configuration", configuration)
				.SetVerbosity(Verbosity.Minimal)
			);
		}
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{
		NUnit3(BIN_DIR + UNIT_TEST_ASSEMBLY);
	});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

// Additional package metadata
var PROJECT_URL = new Uri(GITHUB_SITE);
var LICENSE_URL = new Uri("https://github.com/Ganshorn-Medizin-Electronic/nunit-tm4j-result-writer/blob/master/LICENSE.txt");
var PROJECT_SOURCE_URL = new Uri( GITHUB_SITE );
var PACKAGE_SOURCE_URL = new Uri( GITHUB_SITE );
var BUG_TRACKER_URL = new Uri(GITHUB_SITE + "/issues");
var DOCS_URL = new Uri(README);

Task("RePackageNuGet")
	.Does(() => 
	{
		CreateDirectory(OUTPUT_DIR);

        NuGetPack(
			new NuGetPackSettings()
			{
				Id = NUGET_ID,
				Version = nugetVersion ?? packageVersion,
				Title = TITLE,
				Authors = AUTHORS,
				Owners = OWNERS,
				Description = DESCRIPTION,
				Summary = SUMMARY,
				ProjectUrl = PROJECT_URL,
				LicenseUrl = LICENSE_URL,
				RequireLicenseAcceptance = false,
				Copyright = COPYRIGHT,
				ReleaseNotes = RELEASE_NOTES,
				Tags = TAGS,
				//Language = "en-US",
				OutputDirectory = OUTPUT_DIR,
				Files = new [] {
					new NuSpecContent { Source = PROJECT_DIR + "LICENSE.txt" },
					// new NuSpecContent { Source = PROJECT_DIR + "CHANGES.txt" },
					new NuSpecContent { Source = BIN_SRC + "nunit-tm4j-result-writer.dll", Target = "tools" }
				}
			});
	});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .IsDependentOn("Clean")
	.IsDependentOn("Build");

Task("Package")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("RePackage");

Task("RePackage")
	.IsDependentOn("RePackageNuGet");

Task("Appveyor")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

Task("Travis")
	.IsDependentOn("Build")
	.IsDependentOn("Test");

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
