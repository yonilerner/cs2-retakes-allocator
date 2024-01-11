TARGET_NAME="RetakesAllocator"
TARGET_DIR="./bin/Release/net7.0"
NEW_DIR="$TARGET_DIR/../$TARGET_NAME"

#rm -rf $TARGET_DIR

mkdir -p $NEW_DIR
cp -rf "$TARGET_DIR/runtimes/linux-x64" "$NEW_DIR/runtimes"
cp -rf "$TARGET_DIR/runtimes/win-x64" "$NEW_DIR/runtimes"
zip -r "$TARGET_NAME.zip" "$NEW_DIR/"