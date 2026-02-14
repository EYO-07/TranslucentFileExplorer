# TranslucentFileExplorer
Minimalist Translucent File Explorer based on Tree View (windows 10, 11).

1. Download .NET 10 at https://dotnet.microsoft.com/download
2. Install .NET SDK
3. Download this repository
4. Create a folder to put TranslucentFileExplorer
5. Extract the contents of this repository inside that folder
6. Run the batch script release.bat to compile 
7. go to binary folder and open the program 

To use a different version of .NET you will need to modify the csproj file on TargetFramework tag to the desired .NET version.

```html
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>

	<ApplicationIcon>FileExplorer.ico</ApplicationIcon>	
	
  </PropertyGroup>
  
<ItemGroup> 
	<EmbeddedResource Include="FileExplorer.ico" /> 
</ItemGroup>  

</Project>
```
