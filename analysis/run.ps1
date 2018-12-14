param (
	[string]$path = ".",
	[string]$solution,
	[string]$mhost = "maintain.it.lut.fi",
	[string]$port = "80",
	[string]$token,
	[string]$probe,
	[string]$proj
)

$oldPath = (Get-Location).Path
cd $path

$curDate = (Get-Date)

while ($curDate -gt (Get-Date).AddYears(-1)) {
	$newDate = $curDate.AddDays(-7)
	$minTimestamp = [int64]($curDate-(get-date "1/1/1970")).TotalSeconds
	$maxTimestamp = [int64]($newDate-(get-date "1/1/1970")).TotalSeconds
	$curDate = $curDate.AddDays(-7)

	$commit = git rev-list --max-age=$maxTimestamp --min-age=$minTimestamp --max-count=1 --all
	
	git checkout -f $commit;
	$rev = git rev-parse HEAD
	$comDate = git log -1 --date='format-local:%Y%m%dT%H%M%SZ' --format=%cd

	$executable = $oldPath + "\bin\Release\analysis.exe -p $solution -H $mhost -P 80 -T $token -N $probe -R $rev -D $comDate -I $proj"

	iex $executable
}

cd $oldPath