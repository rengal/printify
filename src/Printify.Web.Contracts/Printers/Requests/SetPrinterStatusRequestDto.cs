namespace Printify.Web.Contracts.Printers.Requests;

public sealed class SetPrinterStatusRequestDto
{
    public required string TargetStatus { get; init; }
}
