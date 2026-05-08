#!/usr/bin/env bash
# One-time setup: creates the resource group, ACR, and Container App for the demo.
# Run this once before triggering any GitHub Actions workflows.
#
# Usage:
#   chmod +x demo/setup.sh
#   ./demo/setup.sh
#
# Override any value with environment variables:
#   ACR_NAME=myacr LOCATION=australiaeast ./demo/setup.sh

set -euo pipefail

# ── Config (override via env vars) ────────────────────────────────────────────
LOCATION="${LOCATION:-eastus}"
RESOURCE_GROUP="${RESOURCE_GROUP:-revision-demo-rg}"
ACR_NAME="${ACR_NAME:-revisiondemo}"        # must be globally unique, alphanumeric only
PROJECT_NAME="${PROJECT_NAME:-demo}"
IMAGE_NAME="${IMAGE_NAME:-revision-demo}"
ACA_ENV_NAME="${ACA_ENV_NAME:-revision-demo-env}"
ACA_NAME="${ACA_NAME:-revision-demo}"

echo "================================================"
echo "  ACA Revision Demo – Setup"
echo "================================================"
echo "  Resource Group : $RESOURCE_GROUP"
echo "  Location       : $LOCATION"
echo "  ACR            : $ACR_NAME"
echo "  ACA Env        : $ACA_ENV_NAME"
echo "  Container App  : $ACA_NAME"
echo "================================================"
read -rp "Proceed? [y/N] " confirm
[[ "$confirm" =~ ^[Yy]$ ]] || { echo "Aborted."; exit 1; }

# ── Resource Group ─────────────────────────────────────────────────────────────
echo ""
echo ">>> Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output table

# ── ACR ───────────────────────────────────────────────────────────────────────
echo ""
echo ">>> Creating Azure Container Registry..."
az acr create \
  --name "$ACR_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --sku Basic \
  --admin-enabled true \
  --output table

# ── Build & push initial image (v1) ───────────────────────────────────────────
echo ""
echo ">>> Building and pushing initial demo image (v1)..."
az acr build \
  --registry "$ACR_NAME" \
  --image "$PROJECT_NAME/$IMAGE_NAME:v1_demo" \
  --image "$PROJECT_NAME/$IMAGE_NAME:demo" \
  --file demo/Dockerfile \
  .

# ── Container Apps Environment ────────────────────────────────────────────────
echo ""
echo ">>> Creating Container Apps Environment..."
az containerapp env create \
  --name "$ACA_ENV_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output table

# ── Container App (initial v1 deployment, single revision) ────────────────────
echo ""
echo ">>> Creating Container App (v1 — main revision)..."
ACR_LOGIN_SERVER="$ACR_NAME.azurecr.io"
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)

az containerapp create \
  --name "$ACA_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$ACA_ENV_NAME" \
  --image "$ACR_LOGIN_SERVER/$PROJECT_NAME/$IMAGE_NAME:v1_demo" \
  --registry-server "$ACR_LOGIN_SERVER" \
  --registry-username "$ACR_NAME" \
  --registry-password "$ACR_PASSWORD" \
  --env-vars APP_VERSION=v1 APP_MESSAGE="Production — main revision" \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 1 \
  --revision-suffix v1 \
  --output table

# ── Enable multiple revision mode ──────────────────────────────────────────────
echo ""
echo ">>> Enabling multiple revision mode..."
az containerapp revision set-mode \
  --name "$ACA_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --mode multiple

# ── Label initial revision as main ────────────────────────────────────────────
echo ""
echo ">>> Labelling initial revision as 'main'..."
INITIAL_REVISION=$(az containerapp show \
  --name "$ACA_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.latestRevisionName \
  -o tsv)

az containerapp revision label add \
  --name "$ACA_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --revision "$INITIAL_REVISION" \
  --label main \
  --yes

# ── Print demo URLs ────────────────────────────────────────────────────────────
FQDN=$(az containerapp show \
  --name "$ACA_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.configuration.ingress.fqdn \
  -o tsv)

echo ""
echo "================================================"
echo "  Setup complete!"
echo "================================================"
echo "  App URL    : https://$FQDN"
echo "  Version    : https://$FQDN/version"
echo "  Main label : https://$ACA_NAME---main.$FQDN"
echo "  Pilot label: https://$ACA_NAME---pilot.$FQDN  (after first deploy)"
echo ""
echo "  Next steps:"
echo "    1. Add these secrets to GitHub: TEAM_SP_CLIENT_ID, TEAM_SP_TENANT_ID, TEAM_SP_SUBSCRIPTION_ID"
echo "    2. Run 'Demo – Deploy to Pilot' workflow in GitHub Actions"
echo "    3. Run 'Demo – Promote Pilot to Main' to cut over traffic"
echo "    4. Run 'Demo – Rollback Main' to revert"
echo "================================================"
