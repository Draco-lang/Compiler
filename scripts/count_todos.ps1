git stash
$commits = git rev-list --reverse HEAD
foreach ($commit in $commits) {
    git checkout $commit >$null 2>&1
    $date = git show -s --format='%ci' $commit
    $todos = git ls-files .. | xargs grep -i todo | wc -l
    Write-Output "${commit};${date};${todos}"
}
git stash pop
