# Security

Understanding the security implications of Virtual Printer is essential for safe usage.

## Important Security Notice

⚠️ **Virtual Printer uses raw TCP connections without encryption.**

Data transmitted to virtual printers is sent over unencrypted TCP connections and may be:
- **Intercepted** by third parties on the network
- **Modified** through man-in-the-middle attacks

## Recommended Usage

Virtual Printer is designed for **testing and development purposes only**.

### ✅ Safe Use Cases
- Local development and testing
- Debugging print functionality
- Demo and presentation environments
- Non-sensitive test data

### ❌ Unsafe Use Cases
- Printing customer data
- Processing payment receipts
- Handling personal information
- Production environments over the internet

## Production Deployment

For production use, we recommend:
- Deploy on your local network only
- Use VPN for remote access
- Never expose TCP endpoints to the public internet
- Consider implementing TLS/SSL encryption layer

## Questions?

If you have security questions or concerns, please contact us at [support@virtual-printer.online](mailto:support@virtual-printer.online).
