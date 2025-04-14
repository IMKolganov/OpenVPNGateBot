# 🔐 Self-signed Certificate with SAN for Kestrel (IP-based)

This script (`generate-cert.sh`) generates a self-signed certificate with a Subject Alternative Name (SAN) containing a specified IP address. It is useful for HTTPS hosting on Kestrel when using an IP instead of a domain.

## 📁 Folder Contents

- `generate-cert.sh` — Bash script to generate the certificate;
- `san.cnf` — generated dynamically on script execution;
- `cert.crt` — generated SSL certificate;
- `cert.key` — generated private key.

## 🚀 Usage

> ⚠️ Requires `openssl` and a Unix-like shell (Linux, macOS, or WSL on Windows).

1. Make the script executable:

```bash
chmod +x generate-cert.sh
```

2. Run the script and pass your public IP address:

```bash
./generate-cert.sh 213.133.91.43
```

The `cert.crt` and `cert.key` files will be created in the same folder.

## 📌 Use in .NET Kestrel Configuration

In `appsettings.json`:

```jsonc
"Kestrel": {
  "Endpoints": {
    "Https": {
      "Url": "https://0.0.0.0:88",
      "Certificate": {
        "Path": "openssl/cert.crt",
        "KeyPath": "openssl/cert.key"
      }
    }
  }
}
```

## ❓ Why SAN is Important

Modern browsers and the Telegram API require the certificate to include `subjectAltName`, even for IP addresses. Without it, you might get errors like:

```
Certificate lacks the subjectAlternativeName (SAN) extension and may not be accepted by browsers.
```

---

## 🪟 On Windows?

If you prefer a PowerShell version of this script, just ask — it can be added easily.
