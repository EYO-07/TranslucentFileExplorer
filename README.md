# TranslucentFileExplorer
Minimalist Translucent File Explorer based on Tree View (windows 10, 11).

1. Download .NET 10 at https://dotnet.microsoft.com/download
2. Install .NET SDK
3. Download this repository
4. Run the batch script release.bat to compile 
5. go to binary folder and open the program 

To use a different version of .NET you will need to modify the cdproj files
1. Open cmd or powershell inside a folder
2. Execute `dotnet new winforms --name FileExplorer`
3. Execute `cd FileExplorer`
4. Copy the files FileExplorer.csproj e FileExplorer.csproj.user to the TranslucentFileExplorer folder which is the ones necessary to modify
5. Run the batch script release.bat to compile
6. go to binary folder and open the program 
