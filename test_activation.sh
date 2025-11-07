#!/bin/bash

echo "Testing contract activation..."
echo ""

# Contract 1
echo "=== Activating Contract 1 ==="
RESPONSE=$(curl -s -X POST http://localhost:5000/api/purchase-contracts/1018c590-f739-4205-adec-02835745b691/activate \
  -H "Content-Type: application/json" \
  -d '{}')

if [ -z "$RESPONSE" ]; then
  echo "Contract activated successfully (204 No Content)"
else
  echo "Response: $RESPONSE"
fi

# Check status
echo "Checking status..."
curl -s "http://localhost:5000/api/purchase-contracts/1018c590-f739-4205-adec-02835745b691" | grep -o '"status":"[^"]*"'

echo ""
echo "=== Activating Contract 2 ==="
RESPONSE=$(curl -s -X POST http://localhost:5000/api/purchase-contracts/75eb7a3d-04c2-4310-8d39-008d8939d9f5/activate \
  -H "Content-Type: application/json" \
  -d '{}')

if [ -z "$RESPONSE" ]; then
  echo "Contract activated successfully (204 No Content)"
else
  echo "Response: $RESPONSE"
fi

echo ""
echo "=== Activating Contract 3 ==="
RESPONSE=$(curl -s -X POST http://localhost:5000/api/purchase-contracts/cfa420f7-b4af-448d-a60c-baf595c48518/activate \
  -H "Content-Type: application/json" \
  -d '{}')

if [ -z "$RESPONSE" ]; then
  echo "Contract activated successfully (204 No Content)"
else
  echo "Response: $RESPONSE"
fi

echo ""
echo "Done!"
