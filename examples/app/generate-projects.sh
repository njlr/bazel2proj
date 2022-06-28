#! /usr/bin/env bash

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

set -e

(
  cd "$SCRIPT_DIR"
  dotnet run --project ../../bazel2proj -- .
)
