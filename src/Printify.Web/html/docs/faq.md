# Frequently Asked Questions

Common questions and troubleshooting help for Virtual Printer.

## Getting Started

### How do I create a workspace?

Click "Create or Access Workspace" on the main page, enter your name (optional), and you'll receive a unique workspace token. Save this token securely for future access.

### How do I access my workspace from another device?

Use "Access Workspace" and enter your workspace token. You can use the same workspace on multiple devices simultaneously.

### I lost my workspace token. Can I recover it?

Workspace tokens cannot be recovered. You'll need to create a new workspace. Always save your token in a secure location.

## Printers

### How many printers can I create?

Each workspace can have up to **10 printers**.

### Can I change the printer port?

Port assignment is currently automatic and cannot be customized.

### Why isn't my printer receiving data?

Check that:
1. The printer status shows "Listening" (not "Stopped")
2. Your application is connecting to the correct host and port
3. No firewall is blocking the connection
4. You're using the correct protocol (raw TCP, not HTTP)

### How do I delete a printer?

Click the gear icon next to the printer name, then select "Delete". This will also remove all associated documents.

## Documents

### How many documents can I store?

Each workspace can store up to **10,000 documents** total across all printers.

### How long are documents kept?

Documents are automatically deleted after **30 days**.

### Can I download documents?

Document download functionality is coming in a future update.

### Why aren't my documents appearing?

1. Verify the printer is in "Listening" state
2. Check that your application is sending data to the correct address
3. Try clicking "Clear" then "Start" to reset the printer
4. Check the browser console for errors

## Workspace & Data

### How long does my workspace last?

Workspaces expire **30 days** after the last document is received. Activity resets the expiration timer.

### Can I delete my workspace?

Use the "Exit Workspace" option to log out. To permanently delete workspace data, email [support@virtual-printer.online](mailto:support@virtual-printer.online).

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

### Documents show as "No visual elements detected"

This means no text or images were rendered. Common causes:
- Sending plain text without ESC/POS formatting
- Protocol mismatch (check printer is configured for ESC/POS)
- Enable "Debug" mode to see raw commands

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

We typically respond within 24-48 hours.
