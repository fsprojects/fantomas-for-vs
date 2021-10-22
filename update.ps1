$env:GIT_REDIRECT_STDERR = '2>&1'

pushd ./fantomas-stable

git checkout master

# Get new tags from remote
git fetch --tags

# Get latest tag name
$latestTag=$(git describe --tags $(git rev-list --tags --max-count=1))

# Checkout latest tag
git checkout $latestTag

popd

pushd ./fantomas-latest

git checkout master

git pull

popd