rm -Recurse -Force &quot;$(TargetDir)..\$(TargetName)&quot;
mkdir -p &quot;$(TargetDir)..\$(TargetName)&quot;
cp -Force &quot;$(TargetDir)* $(TargetDir)..\$(TargetName)&quot;
cp -Force -Recurse &quot;$(TargetDir)runtimes\linux-x64 $(TargetDir)..\$(TargetName)\runtimes&quot;
cp -Force -Recurse &quot;$(TargetDir)runtimes\win-x64 $(TargetDir)..\$(TargetName)\runtimes&quot;
Compress-Archive -Force -Path &quot;$(TargetDir)..\$(TargetName)\*&quot; -DestinationPath &quot;$(TargetDir)..\$(TargetName).zip&quot; 

TARGET_NAME="RetakesAllocator"
TARGET_DIR="./bin/Release/net7.0"
NEW_DIR="$TARGET_DIR/../$TARGET_NAME"

#rm -rf $TARGET_DIR

mkdir -p $NEW_DIR
cp -rf "$TARGET_DIR/runtimes/linux-x64" "$NEW_DIR/runtimes"
cp -rf "$TARGET_DIR/runtimes/win-x64" "$NEW_DIR/runtimes"
zip -r "$TARGET_NAME.zip" "$NEW_DIR/"