#!/bin/bash

# Get the current directory
CURRENT_DIR=$(pwd)

# Change to ../PPgram.Desktop
PPGRAM_DIR="$(dirname "$CURRENT_DIR")/PPgram.Desktop"
cd "$PPGRAM_DIR" || { echo "Failed to change directory to $PPGRAM_DIR"; exit 1; }

# Run dotnet in parallel
(dotnet run &) &

# Wait for 1 second
sleep 5

# Delete files under ~/.local/share/PPgram/*.sesf
SESF_FILES=~/.local/share/PPgram/session.sesf
if compgen -G "$SESF_FILES" > /dev/null; then
  rm "$SESF_FILES"
else
  echo "No .sesf files to delete."
fi

# Launch another instance of dotnet run
(dotnet run &) &
