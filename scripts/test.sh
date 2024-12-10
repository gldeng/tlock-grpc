#!/usr/bin/env bash

encryptionRes=$(grpcurl -plaintext -d '{"chainHash": "52db9ba70e0cc0f6eaf7803dd07447a1f5477735fd3f661792ba94600c84e971", "disclosureTime": "2024-12-10T03:10:00.000Z", "force": true, "data": "MTIz"}' -format json localhost:50051 tlock.TLockService.Encrypt)

echo $encryptionRes | jq .

encrypted=$(echo "$encryptionRes" | jq .encryptedData)

decryptionRes=$(grpcurl -plaintext -d '{"chainHash": "52db9ba70e0cc0f6eaf7803dd07447a1f5477735fd3f661792ba94600c84e971", "encryptedData":'"$encrypted"'}' -format json localhost:50051 tlock.TLockService.Decrypt)

echo $decryptionRes | jq .
