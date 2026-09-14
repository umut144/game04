#!/bin/sh
set -eu

# Routine validation for game04.
#
#   ./scripts/check.sh            build and run every test project
#   ./scripts/check.sh --tests    the same; the flag is accepted and ignored
#
# Unlike the sibling repositories there is no cheap path here that skips the
# tests. The rules of this game live in a Godot-free Core (see AGENTS.md), the
# Core is small, and its tests are the first half of every gate — a default run
# that did not execute them would check almost nothing. `--tests` is still
# accepted because the shared agent runner passes it by default.
#
# The .NET steps are skipped while the projects do not exist yet. Until G00 has
# created them this script checks whitespace and reports what it did not find,
# rather than failing on an absence that is expected.

for argument in "$@"; do
  case "$argument" in
    --tests) ;;
    *)
      echo "usage: $0 [--tests]" >&2
      exit 2
      ;;
  esac
done

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project_directory=$(dirname -- "$script_directory")
cd -- "$project_directory"

DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT DOTNET_NOLOGO

# Trailing whitespace and conflict markers, in what is staged and unstaged.
# Costs nothing and catches what a formatter would otherwise smuggle in.
git diff --check
git diff --cached --check

built=0

# TreatWarningsAsErrors is set for every project in Directory.Build.props, so
# one warning fails the build.
for solution in ./*.sln; do
  [ -f "$solution" ] || continue
  dotnet build "$solution"
  built=1
done

if [ "$built" -eq 0 ]; then
  for project in ./*.csproj; do
    [ -f "$project" ] || continue
    dotnet build "$project"
    built=1
  done
fi

tested=0
for test_project in tests/*/*.Tests.csproj; do
  [ -f "$test_project" ] || continue
  dotnet test "$test_project"
  tested=1
done

if [ "$built" -eq 0 ]; then
  echo 'check: no solution or project at the repository root yet — nothing built.'
fi
if [ "$tested" -eq 0 ]; then
  echo 'check: no tests/*/*.Tests.csproj yet — nothing tested.'
fi
