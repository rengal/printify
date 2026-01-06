# Terms of Use

By using Virtual Printer, you agree to these terms of service.

## Service Description

Virtual Printer provides cloud-based printer emulation for testing and development purposes. The service is provided "as is" without warranties of any kind.

## Acceptable Use

You may use Virtual Printer to:
- Test and develop printing functionality
- Debug ESC/POS command sequences
- Demonstrate printing capabilities
- Integrate with development and staging environments

You **must not** use Virtual Printer to:
- Print sensitive, confidential, or personal data
- Process production workloads
- Print customer information or payment data
- Violate any applicable laws or regulations
- Attempt to access other users' workspaces
- Interfere with service operation or security

## Service Limits

Each workspace is subject to the following limits:

| Resource | Limit |
|----------|-------|
| Printers per workspace | 10 |
| Total documents per workspace | 10,000 |
| Document retention | 30 days |
| Workspace expiration | 30 days after last document |

We reserve the right to adjust these limits or enforce them to maintain service quality.

## Security Notice

> **WARNING:** Virtual Printer uses unencrypted raw TCP connections.

Data transmitted to virtual printers:
- Is **not encrypted**
- May be **intercepted or modified** in transit
- Should **never** contain sensitive information

See our [Security](/docs/security) page for detailed guidance.

## Account Termination

We reserve the right to suspend or terminate access to Virtual Printer for:
- Violation of these terms
- Abusive behavior or excessive resource usage
- Security concerns
- Any reason at our discretion

## Data Retention

- Documents are automatically deleted after 30 days
- Workspaces expire 30 days after the last document is received
- Expired data may be permanently deleted without notice

See our [Privacy Policy](/docs/privacy) for complete data handling details.

## Disclaimer of Warranties

Virtual Printer is provided "as is" and "as available" without warranties of any kind, either express or implied, including but not limited to:
- Merchantability
- Fitness for a particular purpose
- Non-infringement
- Uninterrupted or error-free service

## Limitation of Liability

In no event shall Virtual Printer be liable for any indirect, incidental, special, consequential, or punitive damages, including but not limited to:
- Loss of data
- Loss of profits
- Business interruption
- Damages resulting from use or inability to use the service

## Service Availability

We strive to provide reliable service but do not guarantee:
- Continuous availability
- Specific uptime percentages
- Data backup or recovery

The service may be interrupted for maintenance, updates, or unforeseen technical issues.

## Changes to Terms

We may modify these terms at any time. Continued use of the service after changes constitutes acceptance of the new terms.

## Contact

Questions about these terms? Email [support@virtual-printer.online](mailto:support@virtual-printer.online).

## Third-Party Licenses

Virtual Printer uses third-party components. Their licenses apply to those components and are provided in the
respective package distributions and source repositories.

Runtime components used by the service:
- Markdig (0.38.0)
- FluentValidation (12.0.0)
- MediatR (13.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.*)
- Microsoft.Data.Sqlite (8.0.10)
- Microsoft.EntityFrameworkCore (8.0.10)
- Microsoft.EntityFrameworkCore.Sqlite (8.0.10)
- Microsoft.Extensions.Configuration (8.0.0)
- Microsoft.Extensions.Configuration.Binder (8.0.0)
- Microsoft.Extensions.Configuration.EnvironmentVariables (8.0.0)
- Microsoft.Extensions.Configuration.Json (8.0.0)
- Microsoft.Extensions.Hosting.Abstractions (8.0.0)
- Microsoft.Extensions.Options (8.0.0)
- System.IdentityModel.Tokens.Jwt (8.14.0)
- System.Text.Encoding.CodePages (10.0.0-rc.2.25502.107)
- SixLabors.ImageSharp (3.1.11, 3.1.12)
- ZXing.Net (0.16.11)
- ZXing.Net.Bindings.ImageSharp (0.16.11)
