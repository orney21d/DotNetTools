#!/usr/bin/env bash
set -e

cwd="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
pushd $cwd > /dev/null

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_HOME="$cwd/.dotnet"
export PATH="$DOTNET_HOME:$PATH"
mkdir -p $DOTNET_HOME

channel=$(cat $cwd/cli.yml | grep 'channel' | awk '{print $2}')
export DotnetCliVersion=$(cat $cwd/cli.yml | grep 'version' | awk '{print $2}')

if test ! -x $DOTNET_HOME/dotnet || test "$($DOTNET_HOME/dotnet --version)" != $DotnetCliVersion ; then
    rm -rf $DOTNET_HOME/sdk # clear out potentially bad version of CLI
    curl -sSL https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh \
        | bash -s -- -i $DOTNET_HOME --version $DotnetCliVersion
fi

$DOTNET_HOME/dotnet msbuild build.proj /nologo /v:m "$@"
