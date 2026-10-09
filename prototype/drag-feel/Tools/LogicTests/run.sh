#!/bin/bash
# Runs the EditMode logic tests outside Unity with mono (no Unity install needed).
# Builds Content, Gameplay and the tests as separate assemblies, as Unity does, against a minimal NUnit stand-in.
# Unity-only tests (inside #if UNITY_5_3_OR_NEWER, e.g. the zero-allocation check) run only in the Unity Test Runner.
set -e
HERE=$(cd "$(dirname "$0")" && pwd)
P=$HERE/../../Assets/SortEverything/Prototype
O=$(mktemp -d)
CONTENT=$(ls $P/Scripts/Content/*.cs | grep -v -E 'ObjectArt.cs|ContentSettings.cs')   # the two UnityEngine-only files
mcs -langversion:7.2 -target:library -out:$O/SortEverything.Content.dll $CONTENT
mcs -langversion:7.2 -target:library -r:$O/SortEverything.Content.dll -out:$O/SortEverything.Gameplay.dll $P/Scripts/Gameplay/*.cs
mcs -langversion:7.2 -target:library -out:$O/nunit.framework.dll $HERE/NUnitShim.cs
mcs -langversion:7.2 -target:library -r:$O/SortEverything.Content.dll -r:$O/SortEverything.Gameplay.dll -r:$O/nunit.framework.dll \
  -out:$O/SortEverything.Tests.EditMode.dll $P/Tests/EditMode/*.cs
mcs -langversion:7.2 -r:$O/nunit.framework.dll -out:$O/runner.exe $HERE/Runner.cs
cd $O && SE_CAMPAIGN_DIR=$P/Resources/Campaign mono runner.exe $O/SortEverything.Tests.EditMode.dll
