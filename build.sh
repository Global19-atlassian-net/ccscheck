#!/bin/bash
rm -rf build lib ccscheck
mkdir -p {build,lib,ccscheck/{bin,lib,libexec}}
cd build

# Check for the platform first
if [[ "$OSTYPE" == "linux-gnu" ]]; then
    # ubuntu
    export MONO_ROOT=/dept/consensus/mono/4.4.2.11
    export MONO_GAC_PREFIX=$MONO_ROOT
    export PATH=$MONO_ROOT/bin:$PATH

elif [[ "$OSTYPE" == "darwin"* ]]; then
    # Mac OSX
    export MONO_ROOT=/Library/Frameworks/Mono.framework/Versions/Current
else
    echo "You can't build the release versions here!"
    # Unknown.
fi

# grab dependencies
if [[ ! -d ../third-party/bio/.git ]] || [[ ! -d ../third-party/BWA-Sharp/.git ]]; then
    git submodule update --init --remote
fi

### Build Bio dependency
## Note this requires having mono with the PCL assemblies installed into:
# MONO_PATH/lib/mono/xbuild-frameworks/
# if they aren't there, you will likely have to copy over from a machine that has them
# on Mac OSX they are located at the location below
# /Library/Frameworks/Mono`framework/Versions/4.0.0/lib/mono/xbuild-frameworks
cp -R ../third-party/bio .
cd bio
nuget restore src/Bio.Mono.sln
xbuild /p:Configuration=Release src/Bio.Mono.sln
cp src/Source/Framework/Bio.Desktop/bin/Release/* ../
cp src/Source/Framework/Shims/Bio.Platform.Helpers.Desktop/bin/Release/Bio.Platform.Helpers.* ../
cd ..

## Build BWA Sharp dependency
cp -R ../third-party/BWA-Sharp .
cd BWA-Sharp
make
# this shouldn't be necessary but is
if [ $? -ne 0 ]; then
    make
fi
cp build/Bio.BWA.dll ../
cp build/libbwacsharp.so ../libbwacsharp.$([[ "$(uname)" = "Darwin" ]] && echo "dylib" || echo "so")
cd ../

# Now make a bundled executable
export PKG_CONFIG_PATH=$MONO_ROOT/lib/pkgconfig/
cd ..
cp build/* lib/
xbuild /p:Configuration=Release src/ccscheck.sln
mv src/bin/Release/* build/
cd build

# This command may need to run in the terminal ahead of time
# You may also need to add -liconv to the CC command and run it in the terminal if you
# see something along the lines of:
# undefined reference to `libiconv_open'
## SEE


## MORE NOTES:
## Mono knows what directory it was originally installed in, as it is a string in the original library.  This can cause
## problems in the re-distributed mono if the configuration file or the other file go missing.  To avoid this, I set
## -config-dir equal to a made up name, and check that MONO_CFG_DIR is null and that MONO_PATH are null on boot.
## Either of these can goof the runtime so that it fails, so we need to ensure there are no conflicts.
## Another option is to edit the temp.c file emitted by mkbundle, os that mono_set_dirs does not point someplace funny.

# Check for the platform first
if [[ "$OSTYPE" == "linux-gnu" ]]; then
    cp ../config_ubuntu ./config
    cp $MONO_ROOT/lib/libMonoPosixHelper.so ./
    mkbundle --keeptemp --static  --deps --config-dir /nothing --config config -o ccscheck ccscheck.exe Bio.BWA.dll Bio.Core.dll Bio.Desktop.dll Bio.Platform.Helpers.dll
elif [[ "$OSTYPE" == "darwin"* ]]; then
    cp ../config_mac ./config
    cp $MONO_ROOT/lib/libMonoPosixHelper.dylib ./
    # Mac OSX
    # CC="cc -framework CoreFoundation -lobjc -liconv" mkbundle --config-dir /nothing --config config --static --deps -o ccscheck ccscheck.exe Bio.BWA.dll Bio.Core.dll Bio.Desktop.dll Bio.Platform.Helpers.dll
    CC="cc -framework CoreFoundation -lobjc -liconv" mkbundle --config-dir /nothing --config config --static --deps -o ccscheck ccscheck.exe Bio.BWA.dll Bio.Core.dll Bio.Desktop.dll Bio.Platform.Helpers.dll
    # Manually rerun to link
fi

rm -f temp.*
cd ..
cp wrapper ccscheck/bin/ccscheck
cp build/ccscheck ccscheck/libexec/
cp build/libbwacsharp.* ccscheck/lib/
cp build/libMonoPosixHelper.* ccscheck/lib/

if [[ "$OSTYPE" == "darwin"* ]]; then
    sed -i '' 's|LD_LIBRARY_PATH|DYLD_LIBRARY_PATH|' ccscheck/bin/ccscheck
fi

tar -czvf ccscheck.tar.gz ccscheck

rm -rf build lib src/{bin,obj}
