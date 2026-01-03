# Security

Understanding the security implications of Virtual Printer is essential for safe usage.

## Deployment Modes

Virtual Printer can operate in two distinct modes with different security considerations:

### Cloud Mode (virtual-printer.online)

⚠️ **Data transmitted to virtual-printer.online uses raw TCP connections without encryption.**

When using printers hosted on virtual-printer.online:
- Raw TCP connections are **not encrypted**
- Data may be **intercepted** by third parties on the network
- Data may be **modified** through man-in-the-middle attacks
- **Only use for testing and development** with non-sensitive data
- **Never use for production workloads**

### Self-Hosted Mode (Local Network/Machine)

When you host Virtual Printer on your own infrastructure:
- Security is your administrator's responsibility
- Can be deployed within private networks
- Suitable for production use when properly secured
- Can be isolated using VPNs or network segmentation
- You control encryption and access policies

## Recommended Usage

### ✅ Safe Use Cases (Cloud Mode)
- Local development and testing
- Debugging print functionality
- Demo and presentation environments
- Non-sensitive test data only

### ❌ Unsafe Use Cases (Cloud Mode)
- Printing customer data
- Processing payment receipts
- Handling personal information
- Production environments
- Any sensitive or confidential data

### ✅ Production Deployment (Self-Hosted Only)

For production use, you must self-host Virtual Printer:
- Deploy on your local network (not public cloud)
- Use VPN for remote access if needed
- Never expose TCP endpoints to the public internet
- Implement proper network security and access controls
- Follow your organization's security policies

## Questions?

If you have security questions or concerns, please contact us at [support@virtual-printer.online](mailto:support@virtual-printer.online).
