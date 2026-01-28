#!/bin/bash

# Simple CatCube Server Launcher
# Usage: ./launch_server.sh [MapName]

MAP_NAME=${1:-"Hub"}

echo "--- Building CatCube Server ---"
dotnet build src/CatCube.Server/CatCube.Server.csproj -c Release

echo "--- Starting Server for Map: $MAP_NAME ---"
dotnet run --project src/CatCube.Server/CatCube.Server.csproj -c Release -- --map "$MAP_NAME"
