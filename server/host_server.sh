#!/bin/bash
# CatCube Server Watchdog Script
PORT=${1:-60000}
MAP_NAME=${2:-"Testing_Area"}
MAP_FILE=${3:-"../maps/TestingArea.lua"}

echo "=========================================="
echo "      CATCUBE SERVER IS NOW LIVE          "
echo "=========================================="
echo "ENDPOINT: 127.0.0.1:$PORT"
echo "MAP: $MAP_NAME"
echo "LOGS: Outputting to terminal..."
echo "PRESS CTRL+C TO SHUTDOWN SERVER"
echo "=========================================="

while true; do
    ../build/CatCube --server --port $PORT --mapname $MAP_NAME --map $MAP_FILE
    EXIT_CODE=$?
    if [ $EXIT_CODE -eq 0 ]; then
        echo "Server shut down normally."
        break
    else
        echo "Server crashed with exit code $EXIT_CODE. Rebooting in 3 seconds..."
        sleep 3
    fi
done
