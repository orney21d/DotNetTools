$ErrorActionPreference = "Stop"

cd $PSScriptRoot

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$env:DOTNET_HOME = "$PSScriptRoot/.dotnet"
$env:PATH += $env:DOTNET_HOME
mkdir $env:DOTNET_HOME -ErrorAction Ignore | Out-Null

$channel=($(sls 'channel' $PSScriptRoot/cli.yml | select -exp line) -split ': ')[1]
$env:DotnetCliVersion=($(sls 'version' $PSScriptRoot/cli.yml | select -exp line) -split ': ')[1]

if ( !(Test-Path $env:DOTNET_HOME/dotnet.exe) -or "$(& $env:DOTNET_HOME/dotnet.exe --version)" -ne $env:DotnetCliVersion) {
    rm -Recurse -Force $env:DOTNET_HOME/sdk -ErrorAction Ignore
    $target = "$env:DOTNET_HOME/dotnet-install.ps1"
    if (!(test-path $target)) {
        Invoke-WebRequest https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1 -OutFile $target
    }
    & $env:DOTNET_HOME/dotnet-install.ps1 -InstallDir $env:DOTNET_HOME -Version $env:DotnetCliVersion
}

& $env:DOTNET_HOME/dotnet.exe msbuild build.proj /nologo /v:m $args
