#!/bin/bash

# CatStudio Launcher
echo "--- Starting CatStudio (Unity Dark Edition) ---"
dotnet build src/CatCube.Studio/CatCube.Studio.csproj -c Release
dotnet run --project src/CatCube.Studio/CatCube.Studio.csproj -c Release
