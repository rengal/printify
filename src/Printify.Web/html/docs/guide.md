# Getting Started

Get up and running with Virtual Printer in minutes.

## Quick Start

1. **Create a Workspace**: Click "Create or Access Workspace" and enter a workspace nickname
2. **Save Your Token**: Store your workspace token securely for future access
3. **Add a Printer**: Click "New Printer" and configure your virtual printer
4. **Get the Address**: Note the TCP address (e.g., `virtual-printer.online:9107`)
5. **Send Test Data**: Point your application to the printer address

## Connecting Your Application

Configure your application to send print data to the TCP address shown in the printer details:

```
Host: virtual-printer.online (or your server IP if self-hosted)
Port: 9107 (or assigned port)
Protocol: Raw TCP
```

## Example Code

### C# Example
```csharp
using System.Net.Sockets;
using System.Text;

var client = new TcpClient("virtual-printer.online", 9107);
var stream = client.GetStream();
var data = Encoding.GetEncoding(437).GetBytes("Hello from Virtual Printer!\n");
stream.Write(data, 0, data.Length);
client.Close();
```

### Node.js Example
```javascript
const net = require('net');

const client = net.connect({ host: 'virtual-printer.online', port: 9107 }, () => {
  const buffer = Buffer.from('Hello from Virtual Printer!\n', 'latin1');
  client.write(buffer);
  client.end();
});
```

### Python Example
```python
import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('virtual-printer.online', 9107))
data = 'Hello from Virtual Printer!\n'.encode('cp437')
sock.sendall(data)
sock.close()
```

## Next Steps

- Review **Security** guidelines for safe usage
- Check the **FAQ** page for troubleshooting
