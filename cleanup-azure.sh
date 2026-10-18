#!/bin/bash
set -e

if [ $# -lt 1 ]; then
  echo "Usage: $0 <resource-group>"
  exit 1
fi

RG="$1"

echo "Listing resources in $RG..."
az resource list --resource-group "$RG" --output table 2>/dev/null || echo "(no resources or RG not found)"

printf '\n削除しますか? [y/N] '
read CONFIRM

if [[ "$CONFIRM" =~ ^[Yy]$ ]]; then
  echo "Deleting resource group $RG..."
  az group delete --name "$RG" --yes --no-wait
  echo "Delete job started (no-wait). Monitor with: az group show --name $RG"
else
  echo "Cancelled."
fi
