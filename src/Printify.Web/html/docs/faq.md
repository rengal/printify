# Frequently Asked Questions

Common questions about testing receipt printers without hardware and troubleshooting the virtual printer emulator.

## Getting Started

### How do I create a workspace?

Click "Create or Access Workspace" on the main page, enter your name (optional), and you'll receive a unique workspace token. Save this token securely for future access.

### How do I access my workspace from another device?

Use "Access Workspace" and enter your workspace token. You can use the same workspace on multiple devices simultaneously.

### I lost my workspace token. Can I recover it?

Workspace tokens cannot be recovered. You'll need to create a new workspace. Always save your token in a secure location.

## Virtual Printer Setup

### Can I test receipt or label printing without buying a physical printer?

Yes! Virtual Printer provides an online receipt and label printer emulator that lets you test ESC/POS commands, thermal printer output, and label printing without any physical hardware. Perfect for POS development and testing printing APIs.

### Can I create multiple printers?

Yes. You can create multiple virtual printers in one workspace and emulate a real environment for a large site.

### Can I change the printer port?

Port assignment is currently automatic and cannot be customized.

### Why isn't my virtual receipt printer receiving data?

When testing ESC/POS commands or thermal printer output, check that:
1. The virtual printer status shows "Listening" (not "Stopped")
2. Your POS application is connecting to the correct host and port
3. No firewall is blocking the TCP connection
4. You're using the correct protocol (raw TCP, not HTTP)
5. Your receipt printer emulator is configured properly

### How do I delete a printer?

Click the gear icon next to the printer name, then select "Delete". This will also remove all associated documents.

## Documents

### Can I download documents?

Document download functionality is coming in a future update.

### Why aren't my documents appearing?

1. Verify the printer is in "Listening" state
2. Check that your application is sending data to the correct address
3. Try stopping the printer, then starting it again to reset the state
4. Check the browser console for errors

## Workspace & Data

### How long does my workspace last?

Workspaces expire **30 days** after the last document is received. Activity resets the expiration timer.

### Can I delete my workspace?

Yes. Open Workspace Settings, go to **Danger Zone**, and click **Delete Workspace**.

### Is my data secure?

Virtual Printer can operate in two modes:

**Cloud Mode (virtual-printer.online):**
- Raw TCP connections are **not encrypted**
- Data may be intercepted during transmission
- **Only use for testing and development** with non-sensitive data
- Never use for production workloads

**Self-Hosted Mode (local network/machine):**
- Install Virtual Printer on your own infrastructure
- Security is your administrator's responsibility
- Suitable for production use when properly secured
- Can be isolated within private networks or VPNs

See our [Security](/docs/security) page for detailed information about data transmission and [Privacy Policy](/docs/privacy) for data storage details.

## Troubleshooting

### Connection refused errors

Ensure:
- The printer status shows "Listening" (green)
- You're using the correct host (localhost or server IP)
- The port number matches the printer configuration
- Your firewall allows outbound connections

### How do I test ESC/POS commands without a thermal printer?

Use Virtual Printer's ESC/POS emulator to test receipt printer commands. Send ESC/POS data to the virtual thermal printer via TCP connection, and view the rendered output in real-time. Perfect for testing POS printing without physical hardware.

### Documents show as "No visual elements detected"

This means the document contains only non-visual commands (no text or images). Examples:
- Opening the cash drawer connected to the printer
- Polling printer status before continuing a receipt
- Triggering the printer buzzer (internal or external)
- The server received an unrecognized command because of a client/server bug, an unimplemented feature, or a protocol mismatch

If you expected output:
- Check the printer protocol matches your data (ESC/POS vs other)
- Enable "Debug" mode to see the raw commands and responses

### Application can't connect

1. Verify the printer is started (click "Start" if stopped)
2. Check network connectivity
3. Try telnet to test the connection: `telnet localhost 9107`
4. Review your application's error logs

## Still Need Help?

Email us at [support@virtual-printer.online](mailto:support@virtual-printer.online) with:
- Description of the issue
- Steps to reproduce
- Error messages (if any)
- Screenshots (if applicable)
