# About Virtual Printer

**Virtual Printer** is a cloud-based printer emulation service that enables developers to test receipt and label printing without physical hardware.

## What is Virtual Printer?

Instead of purchasing and configuring physical network printers, you can create virtual printers that emulate real hardware. Send print commands over TCP connections as you would to a network printer, and view the rendered documents in real-time through our web interface.

Create printers with different paper widths and configurations to test how your documents render across various printer models and hardware specifications.

Virtual Printer supports multiple printer command languages and provides an accessible platform for development, testing, and collaboration.

## Key Features

### Real-Time Document Preview
View print jobs as they're processed with accurate rendering of text, images, and formatting.

### Protocol Emulation Support
Support for multiple printer command languages:
- **ESC/POS Emulation** - Receipt printer commands with basic functionality
- **ZPL Emulation** - (In development) Zebra label printing
- **EPL Emulation** - (In development) Eltron label printing
- **TSPL Emulation** - (In development) TSC label printing

ESC/POS emulation includes basic functionality:
- Text formatting (bold, underline, alignment)
- Character sizing and spacing
- Image printing
- Paper cutting commands

### Multi-Printer Management
Create and manage up to 10 virtual printers per workspace with individual configurations.

### Workspace Sharing
Share your entire workspace across devices and team members using workspace tokens, enabling collaborative development and document review.

### Buffer Emulation
Simulate real printer behavior with configurable buffer capacity and drain rates.

## Perfect For

- **API Development**: Test printing integrations before deploying to production
- **Hardware-Free Development**: Build point-of-sale systems without physical printers
- **Remote Teams**: Share workspaces with team members to collaborate on printer development and review documents together
- **Debugging**: Inspect printer command sequences and troubleshoot formatting issues
- **Demos**: Showcase printing functionality without hardware setup

## How It Works

1. Create a workspace and virtual printer
2. Get the TCP connection details (host:port)
3. Configure your application to connect to the virtual printer
4. Send print commands over the TCP connection
5. View rendered documents in real-time on the web interface

## Getting Started

Ready to try Virtual Printer? Check out the [Guide](/docs/guide) for quick start instructions and code examples.

## Support

Questions or issues? See our [FAQ](/docs/faq) or email [support@virtual-printer.online](mailto:support@virtual-printer.online).
