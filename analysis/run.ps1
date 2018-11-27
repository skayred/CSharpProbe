﻿param (
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
foreach ($commit in $(git rev-list master | select -first 100))
{
    git checkout -f $commit;
	$rev = git rev-parse HEAD
	$comDate = git log -1 --date='format-local:%Y%m%dT%H%M%SZ' --format=%cd

	$executable = $oldPath + "\bin\Release\analysis.exe -p $solution -H $mhost -P 80 -T $token -N $probe -R $rev -D $comDate -I $proj"

	iex $executable
}

cd $oldPath