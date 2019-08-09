dotnet test -c release
if %ERRORLEVEL% NEQ 0 echo Test failures occurred. Exiting.
dotnet build -c release .\src\DeployToAzure\DeployToAzure.csproj /t:ILMerge


