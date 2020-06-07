#!/bin/bash

git ls-files '*.cs'   | xargs -n 1 bash -c 'cat header/cs   "$1" > tmp && mv -f tmp "$1"' ''
git ls-files '*.js'   | xargs -n 1 bash -c 'cat header/js   "$1" > tmp && mv -f tmp "$1"' ''
git ls-files '*.html' | xargs -n 1 bash -c 'cat header/html "$1" > tmp && mv -f tmp "$1"' ''
