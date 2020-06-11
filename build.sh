#!/bin/bash

if [ ! -f .paket/paket ]; then
    echo installing paket
    dotnet tool install Paket --version 5.242.2 --tool-path .paket
fi

if [ ! -f paket.lock ]; then
    echo 'running paket install (no paket.lock found)'
    .paket/paket install
fi

if [ ! -f packages/build/fake-cli/tools/netcoreapp2.1/any/fake-cli.dll ]; then
    echo running paket restore
    .paket/paket install
fi

dotnet packages/build/fake-cli/tools/netcoreapp2.1/any/fake-cli.dll build $@ 
