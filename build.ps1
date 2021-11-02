del .\src\bin\* -Recurse
markdown '.\Release Notes.md' > src/Resources/ReleaseNotes.html
msbuild .\src\FantomasVs.sln -p:Configuration=Release
copy .\src\FantomasVs.VS2019\bin\Release\FantomasVs.vsix .\FantomasVs.VS2019.vsix
copy .\src\FantomasVs.VS2022\bin\Release\FantomasVs.vsix .\FantomasVs.VS2022.vsix