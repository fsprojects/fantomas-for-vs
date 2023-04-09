del .\src\bin\* -Recurse
# npm i -g markdown-to-html-cli
markdown-to-html-cli --source '.\Release Notes.md' --output src/Resources/ReleaseNotes.html 
msbuild .\src\FantomasVs.sln -p:Configuration=Release
copy .\src\FantomasVs.VS2019\bin\Release\FantomasVs.vsix .\FantomasVs.VS2019.vsix
copy .\src\FantomasVs.VS2022\bin\Release\FantomasVs.vsix .\FantomasVs.VS2022.vsix