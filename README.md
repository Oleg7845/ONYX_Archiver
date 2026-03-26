# ONYX Archiver
**Secure Data Exchange and Cryptographic Archiving System**

---

## Description

ONYX Archiver is a desktop application designed for creating secure archives with end-to-end encryption support.  
Only an authorized recipient who has completed the key exchange process can access the data.

The project addresses the problem of secure file transfer without relying on third-party services or infrastructure.

---

## Key Features

### Custom Format (.onx)
- Hybrid encryption of files and folders  
- Protection against interception and traffic analysis  

### Peer-to-Peer Handshake (.onxk)
- Public key exchange without servers  
- True end-to-end encryption  

### Secure User Profile
Implementation based on:
- Local database (SQLite)  
- Argon2id for password protection  
- X25519 for key exchange  
- XChaCha20-Poly1305 for encryption  

### Contacts (Peers) System
Trusted recipient management:
- Add  
- Verify  
- Remove  

---

## Security Features

### Zero Trust Architecture
- Data is encrypted on the sender’s side  
- Never transmitted in plaintext  

### Hybrid Cryptography
- X25519 (ECDH)  
- HKDF (key derivation)  
- XChaCha20-Poly1305 (encryption)  
- Ed25519 (digital signatures)  

### Master Key Protection
- Derivation using Argon2id  
- Encryption before storage  
- Use of Windows DPAPI during active session  
- Secure cleanup on logout  

### Metadata Protection
- Dynamic Key ID based on SHA-512  
- Prevents correlation and tracking of user interactions  

---

## Architecture

The project is built using modern development approaches:

- **MVVM** — separation of UI and business logic  
- **Dependency Injection** — flexibility and testability  
- **Asynchronous processing** — UI remains responsive during file and encryption operations  
- **Modular design** — easy extensibility of algorithms and features  

---

## Update System

ONYX Archiver includes a full-featured secure update infrastructure consisting of multiple components:

### 1. UpdateServer
- Central server providing application updates  
- Stores versions, metadata, and digital signatures  

### 2. ONYX Archiver
- Checks for new versions on UpdateServer  
- Prompts the user if a newer version is available  
- Downloads the update package  
- Verifies integrity using digital signatures (UpdateSigner)  

### 3. UpdateAgent
- Separate application responsible for installing updates  
- Performs:
  - Backup of the current version  
  - Safe file replacement  
  - Restarting ONYX Archiver  

### 4. UpdateSigner
- Console tool for generating digital signatures  
- Produces metadata:
  - File hash  
  - Digital signature  
  - Public key  
  - Archive size  

**Implementation highlights:**
- Clear separation of responsibilities  
- Full integrity and signature verification  
- Reduced risk of corruption during updates  

---

## Technologies

- C# / .NET 8  
- WPF  
- Entity Framework Core  
- SQLite  
- ZStandard (compression)  

### Cryptography (NSec + Bouncy Castle)
- X25519  
- XChaCha20-Poly1305  
- Ed25519  
- Argon2id  
- HKDF  

---

## Implementation Highlights

- Asynchronous processing of large files  
- Real-time progress reporting  
- Flexible key and session management system  
- Extensible architecture  

---

## Summary

ONYX Archiver is not just an archiver, but a complete secure data exchange system demonstrating the practical application of modern cryptographic techniques and architectural patterns in desktop development.

---

## Getting Started

1. Download the latest version from **Releases**  
2. Launch the application  
3. Create a profile  
4. Perform key exchange  

---

## Screenshots

_Add screenshots here_
