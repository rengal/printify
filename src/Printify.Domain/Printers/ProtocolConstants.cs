namespace Printify.Domain.Printers;

/// <summary>
/// Protocol string constants used for API communication with the frontend.
/// These values match the lowercase protocol strings sent from the JavaScript client.
///
/// TODO: REFACTOR CONSIDERATIONS
/// ==============================
/// This class contains API contract strings that are NOT domain logic.
/// Current usage analysis shows these constants are only used by:
/// - Infrastructure layer (DomainMapper.cs) - for string-to-enum mapping
/// - Web layer (CommandMapper.cs) - for API string mapping
/// - Test layer - for testing protocol string values
///
/// OPTIONS FOR REFACTORING:
/// 1. Move to src/Printify.Web.Contracts/Printers/ProtocolConstants.cs
///    - Pros: Keeps API contracts with API layer
///    - Cons: Infrastructure layer would need to reference Web.Contracts
///
/// 2. Move to src/Printify.Infrastructure/Printing/Protocols/ProtocolStrings.cs
///    - Pros: Keeps mapping logic close to usage
///    - Cons: Infrastructure shouldn't define API contracts
///
/// 3. Keep here but rename to ProtocolApiStrings to clarify intent
///    - Pros: Minimal change, clear name
///    - Cons: Domain layer still contains API concerns
///
/// RECOMMENDATION: Option 1 - Move to Web.Contracts and update Infrastructure
/// to use the Protocol enum directly for internal logic, only mapping at the API boundary.
/// </summary>
public static class ProtocolConstants
{
    /// <summary>ESC/POS protocol (Epson-compatible) - lowercase API value.</summary>
    public const string EscPos = "escpos";

    /// <summary>EPL protocol (Eltron Programming Language) - lowercase API value.</summary>
    public const string Epl = "epl";

    /// <summary>ZPL protocol (Zebra Programming Language) - lowercase API value.</summary>
    public const string Zpl = "zpl";

    /// <summary>TSPL protocol (TSC Printer Language) - lowercase API value.</summary>
    public const string Tspl = "tspl";

    /// <summary>SLCS protocol (Sato Barcode Printer Language) - lowercase API value.</summary>
    public const string Slcs = "slcs";
}
