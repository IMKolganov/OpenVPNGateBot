#!/bin/bash

# Exit on error
set -e

# Check if IP address is provided
if [ -z "$1" ]; then
  echo "Usage: $0 <IP_ADDRESS>"
  exit 1
fi

IP=$1
CERT_DIR="$(dirname "$0")"
CNF_FILE="$CERT_DIR/datagatetgbot.cnf"
KEY_FILE="$CERT_DIR/datagatetgbot.key"
CRT_FILE="$CERT_DIR/datagatetgbot.crt"

# Generate san.cnf
cat > "$CNF_FILE" <<EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
req_extensions = req_ext
distinguished_name = dn

[dn]
C = CY
ST = Cyprus
L = Nicosia
O = YourOrg
CN = $IP

[req_ext]
subjectAltName = @alt_names

[alt_names]
IP.1 = $IP
EOF

# Generate cert + key
openssl req -x509 -nodes -days 365 \
  -newkey rsa:2048 \
  -keyout "$KEY_FILE" \
  -out "$CRT_FILE" \
  -config "$CNF_FILE" \
  -extensions req_ext

echo "✅ Certificate and key generated:"
echo " - cert: $CRT_FILE"
echo " - key : $KEY_FILE"

# Also export public certificate in .pem format for Telegram webhook
PEM_FILE="$CERT_DIR/datagatetgbot.pem"
cp "$CRT_FILE" "$PEM_FILE"
echo "📦 Public certificate exported for Telegram:"
echo " - PEM : $PEM_FILE"