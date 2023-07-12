: for best results, you currently need to use the experimental "features/interceptors" branch
: of Roslyn; the easiest way to do this is in devenv; see
: https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md
:
:
:
: git clone https://github.com/dotnet/roslyn.git
: cd roslyn
: git checkout features/interceptors
: .\Restore.cmd
: eng\enable-long-paths.reg
: .\Build.cmd -Configuration Release -deployExtensions -launch
:
: 
: once it has built, you can just use:

@devenv Dapper.AOT.sln /rootSuffix RoslynDev