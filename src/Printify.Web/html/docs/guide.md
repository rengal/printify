# Getting Started

Learn how to test receipt and label printing without a physical printer using Virtual Printer's online emulator.

## Quick Start: Virtual Receipt Printer Setup

1. **Create a Workspace**: Click "Create or Access Workspace" and enter a workspace nickname
2. **Save Your Token**: Store your workspace token securely for future access
3. **Add a Virtual Printer**: Click "New Printer" and configure your ESC/POS emulator
4. **Get the Address**: Note the TCP address (e.g., `virtual-printer.online:9107`)
5. **Test Print Commands**: Point your application to the virtual printer address

## Connecting to the Virtual Thermal Printer

Configure your POS application to send ESC/POS commands to the virtual receipt printer:

```
Host: virtual-printer.online (or your server IP if self-hosted)
Port: 9107 (or assigned port)
Protocol: Raw TCP
```

## Example Code

<div class="code-tabs">
  <div class="code-tabs-header">
    <button class="code-tab-btn" data-tab="csharp">C#</button>
    <button class="code-tab-btn" data-tab="cpp">C++</button>
    <button class="code-tab-btn" data-tab="nodejs">Node.js</button>
    <button class="code-tab-btn" data-tab="python">Python</button>
  </div>
  <div class="code-tabs-content">
    <div class="code-tab-panel" data-tab="csharp">

```csharp
using System.Net.Sockets;
using System.Text;

var client = new TcpClient("virtual-printer.online", 9107);
var stream = client.GetStream();
var data = Encoding.GetEncoding(437).GetBytes("Hello from Virtual Printer!\n");
stream.Write(data, 0, data.Length);
client.Close();
```

</div>
    <div class="code-tab-panel" data-tab="cpp">

```cpp
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#pragma comment(lib, "Ws2_32.lib")

int main() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2, 2), &wsaData);

    SOCKET sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

    sockaddr_in addr;
    addr.sin_family = AF_INET;
    addr.sin_port = htons(9107);
    inet_pton(AF_INET, "virtual-printer.online", &addr.sin_addr);

    connect(sock, (sockaddr*)&addr, sizeof(addr));

    const char* data = "Hello from Virtual Printer!\n";
    send(sock, data, strlen(data), 0);

    closesocket(sock);
    WSACleanup();
    return 0;
}
```

</div>
    <div class="code-tab-panel" data-tab="nodejs">

```javascript
const net = require('net');

const client = net.connect({ host: 'virtual-printer.online', port: 9107 }, () => {
  const buffer = Buffer.from('Hello from Virtual Printer!\n', 'latin1');
  client.write(buffer);
  client.end();
});
```

</div>
    <div class="code-tab-panel" data-tab="python">

```python
import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.connect(('virtual-printer.online', 9107))
data = 'Hello from Virtual Printer!\n'.encode('cp437')
sock.sendall(data)
sock.close()
```

</div>
  </div>
</div>

## Next Steps

- Review **Security** guidelines for safe usage
- Check the **FAQ** page for troubleshooting
