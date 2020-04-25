#!/bin/sh

WORK_DIR="$(mktemp -d)"
finish() {
    cd /
    rm -rf "$WORK_DIR"
}
trap finish EXIT

wget -q -O "$WORK_DIR/Parsing.zip" \
    https://ci.appveyor.com/api/projects/b1f6c1c4/ProfessionalAccounting/artifacts/AccountingServer.BLL%2FParsing.zip\?branch\=master

(cd "$WORK_DIR"; unzip Parsing.zip)

DIR="$(realpath "$(dirname "$0")")"

cd "$WORK_DIR"

mv Query.tokens QueryParser.cs \
    QueryLexer.cs QueryLexer.tokens \
    "$DIR/AccountingServer.BLL/Parsing/"

mv Subtotal.tokens SubtotalParser.cs \
    SubtotalLexer.cs SubtotalLexer.tokens \
    "$DIR/AccountingServer.BLL/Parsing/"
