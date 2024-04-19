#!/usr/bin/env python3

import os
import shutil
import subprocess
import sys


def doBuild(dir):
    os.chdir(dir)
    subprocess.run(['dotnet.exe', 'build'])
    os.chdir('..')
    print(os.path.join(dir, 'bin', 'Debug', 'net46', os.path.basename(dir) + '.dll'))
    shutil.copy2(os.path.join(dir, 'bin', 'Debug', 'net46', os.path.basename(dir) + '.dll'), 'dist')


os.makedirs('dist', exist_ok=True)

buildDirs = sys.argv if len(sys.argv) > 1 else os.listdir(os.path.curdir)
for dir in buildDirs:
    dir = dir[0:-1] if dir.endswith('/') else dir # Strip a trailing '/'
    if os.path.isdir(dir):
        for file in os.listdir(dir):
            if file.endswith(".csproj"):
                doBuild(dir)
