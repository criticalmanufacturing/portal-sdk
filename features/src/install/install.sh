#!/bin/sh
set -e

echo "Activating feature '@criticalmanufacturing/portal-sdk/install'"
echo "The requested @criticalmanufacturing/portal version is: ${VERSION}"

npm install --global @criticalmanufacturing/portal@$VERSION
