# About Virtual Printer

**Virtual Printer** is a cloud-based receipt printer emulator and thermal printer simulator that enables developers to test receipt and label printing without physical hardware.

## What is Virtual Printer?

Virtual Printer is an online ESC/POS emulator and virtual receipt/label printer for development and testing. Instead of purchasing physical network printers, you can create virtual thermal printers that emulate real hardware for testing POS printing applications.

Send print commands over TCP connections as you would to a network printer, and view the rendered documents in real-time through our web interface. Test ESC/POS commands, ZPL printer output, and other printer protocols without requiring physical devices.

Create printers with different paper widths and configurations to test how your documents render across various printer models and hardware specifications. Perfect for developers building point-of-sale systems, receipt/label printing APIs, or any application requiring thermal printer testing.

## Key Features

### Real-Time Document Preview
View print jobs as they're processed with accurate rendering of text, images, and formatting.

### Printer Protocol Emulation
Virtual printer simulator supporting multiple printer command languages:
- **ESC/POS Emulator** - Test receipt printer commands with basic functionality
- **ZPL Printer Simulator** - (In development) Test Zebra label printing
- **EPL Emulator** - (In development) Eltron label printer testing
- **TSPL Emulator** - (In development) TSC printer command testing

ESC/POS thermal printer emulation includes:
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

- **POS Development**: Test receipt printing without physical thermal printers
- **API Testing**: Develop and test printing integrations without hardware
- **ESC/POS Development**: Debug receipt printer commands and formatting
- **Label Printer Testing**: Test ZPL and other label printing protocols
- **Mock Printer Testing**: Simulate network printers for development environments
- **Remote Development**: Share virtual printers with distributed teams
- **Demo Environments**: Showcase printing functionality without hardware setup

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
