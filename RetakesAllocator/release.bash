#!/usr/bin/env bash

TARGET_NAME="RetakesAllocator"
TARGET_DIR="./bin/Release/net7.0"
NEW_DIR="./bin/Release/RetakesAllocator"

echo $TARGET_NAME
echo $TARGET_DIR
echo $NEW_DIR

ls $TARGET_DIR/**

#echo mkdir -p $NEW_DIR
#mkdir -p $NEW_DIR
echo cp -r $TARGET_DIR $NEW_DIR
cp -r $TARGET_DIR $NEW_DIR
echo rm -rf "$NEW_DIR/runtimes"
rm -rf "$NEW_DIR/runtimes"
echo mkdir "$NEW_DIR/runtimes"
mkdir "$NEW_DIR/runtimes"
echo cp -rf "$TARGET_DIR/runtimes/linux-x64" "$NEW_DIR/runtimes"
cp -rf "$TARGET_DIR/runtimes/linux-x64" "$NEW_DIR/runtimes"
echo cp -rf "$TARGET_DIR/runtimes/win-x64" "$NEW_DIR/runtimes"
cp -rf "$TARGET_DIR/runtimes/win-x64" "$NEW_DIR/runtimes"
#echo zip -r "./bin/Release/$TARGET_NAME.zip" "$NEW_DIR/"
#zip -r "./bin/Release/$TARGET_NAME.zip" "$NEW_DIR/"

tree ./bin