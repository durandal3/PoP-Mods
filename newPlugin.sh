#!/bin/bash

if [[ -z "$1" ]]; then
  echo "Provide a name!"
  exit 1
fi

dotnet.exe new bepinex5plugin -n "$1" -T net46 -U 2019.4.16
