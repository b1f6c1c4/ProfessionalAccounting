#!/usr/bin/bash

set -euxo pipefail

if ! docker buildx inspect accounting-builder > /dev/null; then
    docker buildx create --name accounting-builder --use
else
    docker buildx use accounting-builder
fi

dotnet build -c Release \
    && docker buildx build --load -t b1f6c1c4/accounting-backend AccountingServer/bin/Release/net9.0 \
    &

git archive --format=tar HEAD | xz > nginx/archive.tar.xz
rm -rf nginx/dist
npm run --prefix nginx build

docker buildx build --load -t b1f6c1c4/accounting-frontend nginx &
docker buildx build --load -t b1f6c1c4/accounting-frontend:local -f Dockerfile.local nginx &

wait
