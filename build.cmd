@cls
@rd /S /Q "src/Dapper.AOT/bin/Release/" >NUL 2>NUL

@dotnet clean src/Dapper.AOT.Analyzers
@dotnet clean src/Dapper.AOT

@dotnet restore src/Dapper.AOT.Analyzers
@dotnet restore src/Dapper.AOT

@dotnet build src/Dapper.AOT.Analyzers -c Release
@dotnet build src/Dapper.AOT -c Release

@dotnet pack src/Dapper.AOT -c Release

@dotnet test test/Dapper.AOT.Test -c Release

@echo .
@echo Deployment package is in: src/Dapper.AOT/bin/Release/
@echo .
@dir src\Dapper.AOT\bin\Release\*.nupkg /B