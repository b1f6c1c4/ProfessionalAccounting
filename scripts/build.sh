#!/usr/bin/sh

set -eux

(git archive --format=tar HEAD | xz > nginx/archive.tar.xz) \
    && rm -rf nginx/dist \
    && npm run --prefix nginx build \
    && docker build -t b1f6c1c4/accounting-frontend:latest nginx \
    && docker build -t b1f6c1c4/accounting-frontend:local -f Dockerfile.local nginx \
    &

dotnet build -c Release
(cd AccountingServer/bin/Release/net9.0 \
    && docker build -t b1f6c1c4/accounting-backend:latest .)

wait
