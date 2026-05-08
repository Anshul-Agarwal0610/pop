#!/usr/bin/env bash
# Tears down all demo Azure resources after the demo is done.
# Use the same env vars you passed to setup.sh.
#
# Usage:
#   chmod +x demo/teardown.sh
#   ./demo/teardown.sh
#
# Override via env vars:
#   RESOURCE_GROUP=my-rg ./demo/teardown.sh

set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:-revision-demo-rg}"

echo "================================================"
echo "  ACA Revision Demo – Teardown"
echo "================================================"
echo "  This will DELETE resource group: $RESOURCE_GROUP"
echo "  and everything inside it (ACR, ACA, env)."
echo "================================================"
read -rp "Type the resource group name to confirm: " confirm
[[ "$confirm" == "$RESOURCE_GROUP" ]] || { echo "Name mismatch. Aborting."; exit 1; }

echo ""
echo ">>> Deleting resource group $RESOURCE_GROUP ..."
az group delete \
  --name "$RESOURCE_GROUP" \
  --yes \
  --no-wait

echo ""
echo "Deletion queued. Resources will be gone within a few minutes."
