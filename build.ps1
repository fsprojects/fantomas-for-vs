del .\src\bin\* -Recurse
markdown '.\Release Notes.md' > src/ReleaseNotes.html
msbuild .\src\FantomasVs.sln -p:Configuration=Release
copy .\src\bin\Release\FantomasVs.vsix .\