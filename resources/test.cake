// Install modules
#module nuget:?package=Cake.DotNetTool.Module&version=1.0.1

// Install standard nuget package
#addin "nuget:?package=System.Text.Json&version=5.0.1&loaddependencies=true"

// Install addins.
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Coveralls&version=1.0.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Twitter&version=1.0.0"
#addin "nuget:https://api.nuget.org/v3/index.json?package=Cake.Gitter&version=1.0.1"

// Install tools.
#tool "nuget:https://api.nuget.org/v3/index.json?package=coveralls.io&version=1.4.2"
#tool "nuget:https://api.nuget.org/v3/index.json?package=OpenCover&version=4.7.922"
#tool "nuget:https://api.nuget.org/v3/index.json?package=ReportGenerator&version=4.7.1"
#tool "nuget:https://api.nuget.org/v3/index.json?package=nuget.commandline&version=5.7.0"

// Install .NET Core Global tools.
#tool "dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=5.1.2"
#tool "dotnet:https://api.nuget.org/v3/index.json?package=SignClient&version=1.2.109"
#tool "dotnet:https://api.nuget.org/v3/index.json?package=GitReleaseManager.Tool&version=0.11.0"

// Load other scripts.
#load "./build/parameters.cake"

