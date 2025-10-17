using System.Collections.Generic;
using System.Linq;
using Printify.Domain.Printers;
using Printify.Domain.Users;
using Printify.Web.Contracts.Printers.Responses;
using Printify.Web.Contracts.Users.Responses;

namespace Printify.Web.Mapping;

internal static class UserMapper
{
    internal static UserDto ToDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserDto(user.Id, user.DisplayName);
    }

    internal static IReadOnlyList<UserDto> ToDtos(this IEnumerable<User> users)
    {
        ArgumentNullException.ThrowIfNull(users);
        return users.Select(ToDto).ToList();
    }

    internal static PrinterDto ToPrinterDto(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return new PrinterDto(
            printer.Id,
            printer.DisplayName,
            printer.Protocol,
            printer.WidthInDots,
            printer.HeightInDots);
    }

    internal static IReadOnlyList<PrinterDto> ToPrinterDtos(IEnumerable<Printer> printers)
    {
        ArgumentNullException.ThrowIfNull(printers);
        return printers.Select(ToPrinterDto).ToList();
    }
}
