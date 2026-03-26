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
<img width="888" height="597" alt="new-version-dialog-message" src="https://github.com/user-attachments/assets/9ad5056f-1a28-4b8d-bf68-696042ad6341" />
<img width="589" height="158" alt="update-agent-window-with-new-version-installing" src="https://github.com/user-attachments/assets/1ca2cbef-9a84-4e07-8532-16a17f6b6205" />
<img width="889" height="597" alt="registration-form" src="https://github.com/user-attachments/assets/7646ebe1-a5e0-4a14-b884-e5c817f5dad0" />
<img width="888" height="596" alt="login-form" src="https://github.com/user-attachments/assets/317d51d9-3fcc-4e23-b740-04c2fc918e73" /><img width="887" height="595" alt="create-archive-section" src="https://github.com/user-attachments/assets/3ab28ddf-d3d8-43ec-a64a-366768f60632" />
<img width="888" height="596" alt="open-archive-section" src="https://github.com/user-attachments/assets/6e8f280a-ab6b-4c11-9f6b-6df071a601fd" />
<img width="889" height="596" alt="peers-list-section" src="https://github.com/user-attachments/assets/b2e2a3d5-d17b-4fca-a1bf-3b25a8fc870b" />
<img width="888" height="596" alt="settings-section" src="https://github.com/user-attachments/assets/91755c0b-ec3d-4220-8<img width="889" height="599" alt="recipient-selection-dialog" src="https://github.com/user-attachments/assets/e2d9535e-9e5e-4a30-bc9f-316e2667edda" />
4e7-9c7b583ea605" />
<img width="888" height="596" alt="archive-creation-process-dialog" src="https://github.com/user-atta<img width="890" height="597" alt="opened-archive-files-selection" src="https://github.com/user-attachments/assets/0a1658a7-a746-48b5-95d6-0acc50de7be9" />
chments/assets/6adfef95-c08f-422f-8d82-99204e9264a2" />
<img width="890" height="597" alt="archive-files-extraction-process-dialog" src="https://github.com/user-attachments/assets/6599f1c6-9c9e-439b-ac7c-20c38273b5dd" />

The full set of screenshots is available in the `screenshots` folder.
