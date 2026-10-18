#!/bin/bash
set -e

if [ $# -lt 2 ]; then
  echo "Usage: $0 <manual-stack> <transform-stack>"
  exit 1
fi

MANUAL_STACK="$1"
TRANSFORM_STACK="$2"

for STACK in "$MANUAL_STACK" "$TRANSFORM_STACK"; do
  echo ""
  echo "=== Stack: $STACK ==="
  echo "Stack resources:"
  aws cloudformation list-stack-resources --stack-name "$STACK" --output table 2>/dev/null || echo "(stack not found)"
done

printf '\n削除しますか? [y/N] '
read CONFIRM

if [[ "$CONFIRM" =~ ^[Yy]$ ]]; then
  for STACK in "$MANUAL_STACK" "$TRANSFORM_STACK"; do
    echo ""
    echo "Processing stack: $STACK"

    # Find S3 bucket from stack resources
    BUCKET=$(aws cloudformation list-stack-resources \
      --stack-name "$STACK" \
      --query "StackResourceSummaries[?ResourceType=='AWS::S3::Bucket'].PhysicalResourceId" \
      --output text 2>/dev/null || true)

    if [ -n "$BUCKET" ] && [ "$BUCKET" != "None" ]; then
      echo "Emptying S3 bucket: $BUCKET"
      aws s3 rm "s3://$BUCKET" --recursive 2>/dev/null || echo "(bucket already empty or not found)"
      aws s3 rb "s3://$BUCKET" 2>/dev/null || echo "(bucket removal skipped)"
    else
      echo "(no S3 bucket found in stack $STACK)"
    fi

    echo "Deleting stack: $STACK"
    aws cloudformation delete-stack --stack-name "$STACK" 2>/dev/null || echo "(stack not found or already deleted)"
    echo "Delete initiated for $STACK"
  done
else
  echo "Cancelled."
fi
