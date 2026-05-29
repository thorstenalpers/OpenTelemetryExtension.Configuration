REM dotnet tool install --global nuget-license 

nuget-license -i ".\src\OpenTelemetryExtension.Configuration\OpenTelemetryExtension.Configuration.csproj" -t -o Markdown --exclude-projects-matching "*Tests*" -fo THIRD_PARTY_LICENSES.md