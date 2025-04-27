#!/usr/bin/sh

set -eu

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <<identity>>" >&2
    exit 1
fi

docker run --rm -it \
    --name invite \
    -e MONGO_URI=/opt/accounting/atlas/url \
    -e MONGO_CERT=/opt/accounting/atlas/cert.pem \
    -v /data/accounting/atlas:/opt/accounting/atlas:ro \
    -v /data/accounting/config.d:/opt/accounting/config.d:ro \
    b1f6c1c4/accounting-backend --invite "$1"
