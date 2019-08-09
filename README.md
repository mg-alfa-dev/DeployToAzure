# DeployToAzure

DeployToAzure is a command-line tool that the we use for command-line deployments of our Azure Cloud Services applications.

The DeployToAzure.ps1 file provides some simple helpers around the DeployToAzure.exe.

## Build & Test

`dotnet build`
`dotnet test`

There is also a `build.cmd` file that runs `dotnet test -c release` and then, on success, produces a single executable using ILMerge.