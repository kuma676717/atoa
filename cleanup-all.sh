#!/bin/bash
set -e

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)

echo "=== Azure Cleanup ==="
"$SCRIPT_DIR/cleanup-azure.sh" "$@"

echo "=== AWS Cleanup ==="
"$SCRIPT_DIR/cleanup-aws.sh" "$@"
