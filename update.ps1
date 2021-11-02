$env:GIT_REDIRECT_STDERR = '2>&1'

function getTag {
    $latestTag=$(git describe --tags $(git rev-list --tags --max-count=1))
    $latestTag = $latestTag.Trim('v', '.')
    [Version]::Parse($latestTag)
}

pushd ./fantomas-stable

git checkout master

git pull

# Get new tags from remote
git fetch --tags

# Get latest tag name
$latestTag = getTag

# Checkout latest tag
git checkout tags/v$latestTag

popd

pushd ./fantomas-latest

#git checkout master

#git pull

popd


function update-version {
param($dir)

pushd $dir
$template = cat -Raw .\template.vsixmanifest
$ver = getTag
$nextVersion = [Version]::new($ver.Major, $ver.Minor, $ver.Build + 1)
$manifest = $template.Replace("{{latestTag}}", $latestTag).Replace("{{version}}", $nextVersion)
$manifest | Out-File -FilePath .\source.extension.vsixmanifest -Encoding utf8
popd

}

update-version .\src\FantomasVs.VS2019
update-version .\src\FantomasVs.VS2022

