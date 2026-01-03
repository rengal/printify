# Getting Started

Get up and running with Virtual Printer in minutes.

## Quick Start

1. **Create a Workspace**: Click "Create or Access Workspace" and enter your name
2. **Save Your Token**: Store your workspace token securely for future access
3. **Add a Printer**: Click "New Printer" and configure your virtual printer
4. **Get the Address**: Note the TCP address (e.g., `localhost:9107`)
5. **Send Test Data**: Point your application to the printer address

## Connecting Your Application

Configure your application to send print data to the TCP address shown in the printer details:

```
Host: localhost (or your server IP)
Port: 9107 (or assigned port)
Protocol: Raw TCP
```

## Example Code

### Node.js Example
```javascript
const net = require('net');

const client = net.connect({ host: 'localhost', port: 9107 }, () => {
  client.write('Hello from Virtual Printer!\n');
  client.end();
});
```

### Python Example
```python
import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('localhost', 9107))
sock.sendall(b'Hello from Virtual Printer!\n')
sock.close()
```

## Next Steps

- Explore the **Features** page to learn about advanced capabilities
- Review **Security** guidelines for safe usage
- Check the **Help** page for troubleshooting
