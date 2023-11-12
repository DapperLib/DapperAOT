@cls
@rd /S /Q "src/Dapper.AOT.Analyzers/bin/Release/" >NUL 2>NUL
@rd /S /Q "src/Dapper.AOT/bin/Release/" >NUL 2>NUL
@rd /S /Q "src/Dapper.Advisor/bin/Release/" >NUL 2>NUL

@dotnet clean src/Dapper.AOT.Analyzers
@dotnet clean src/Dapper.AOT
@dotnet clean src/Dapper.Advisor

@dotnet restore src/Dapper.AOT.Analyzers
@dotnet restore src/Dapper.AOT
@dotnet restore src/Dapper.Advisor

@dotnet build src/Dapper.AOT.Analyzers -c Release
@dotnet build src/Dapper.AOT -c Release
@dotnet build src/Dapper.Advisor -c Release

@dotnet pack src/Dapper.AOT -c Release
@dotnet pack src/Dapper.Advisor -c Release

@dotnet test test/Dapper.AOT.Test -c Release

@echo .
@echo Deployment package is in: src/Dapper.AOT/bin/Release/
@echo and: src/Dapper.Advisor/bin/Release/
@echo .
@dir src\Dapper.AOT\bin\Release\*.nupkg /B
@dir src\Dapper.Advisor\bin\Release\*.nupkg /B