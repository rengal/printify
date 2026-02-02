using System.Reflection;
using DomainCommand = Printify.Domain.Printing.Command;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Tests.Shared;

public static class CommandTestExtensions
{
    /// <summary>
    /// Gets all concrete EscPosCommand types from the assembly.
    /// </summary>
    public static HashSet<Type> GetAllEscPosCommandTypes()
    {
        return typeof(EscPosCommand).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EscPosCommand)) && !t.IsAbstract && !t.IsInterface)
            .ToHashSet();
    }

    /// <summary>
    /// Gets all concrete EplCommand types from the assembly.
    /// </summary>
    public static HashSet<Type> GetAllEplCommandTypes()
    {
        return typeof(EplCommand).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(EplCommand)) && !t.IsAbstract && !t.IsInterface)
            .ToHashSet();
    }

    /// <summary>
    /// Verifies that the tested command list contains all command types from the assembly.
    /// </summary>
    public static void VerifyAllEscPosCommandTypesAreTested(
        List<DomainCommand> testedCommands,
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var allCommandTypes = GetAllEscPosCommandTypes();
        var testedTypes = testedCommands.Select(c => c.GetType()).ToHashSet();

        var missingTypes = allCommandTypes
            .Where(t => !testedTypes.Contains(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        if (missingTypes.Any())
        {
            throw new InvalidOperationException(
                $"The following EscPosCommand types are not tested in {testName}:\n{string.Join("\n", missingTypes)}\n" +
                "Add test instances for these commands to the test list.");
        }
    }

    /// <summary>
    /// Verifies that the tested command list contains all renderable EscPosCommand types from the assembly.
    /// Excludes upload commands which are not meant to be rendered directly.
    /// </summary>
    public static void VerifyAllRenderableEscPosCommandTypesAreTested(
        List<DomainCommand> testedCommands,
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var allCommandTypes = GetAllEscPosCommandTypes();
        var testedTypes = testedCommands.Select(c => c.GetType()).ToHashSet();

        // Upload commands that should not be rendered
        var uploadCommandTypes = new[] { "PrintBarcodeUpload", "PrintQrCodeUpload", "RasterImageUpload" };

        var missingTypes = allCommandTypes
            .Where(t => !testedTypes.Contains(t) && !uploadCommandTypes.Contains(t.Name))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        if (missingTypes.Any())
        {
            throw new InvalidOperationException(
                $"The following EscPosCommand types are not tested in {testName}:\n{string.Join("\n", missingTypes)}\n" +
                "Add test instances for these commands to the test list.");
        }
    }

    /// <summary>
    /// Verifies that the tested command list contains all command types from the assembly.
    /// </summary>
    public static void VerifyAllEplCommandTypesAreTested(
        List<DomainCommand> testedCommands,
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var allCommandTypes = GetAllEplCommandTypes();
        var testedTypes = testedCommands.Select(c => c.GetType()).ToHashSet();

        var missingTypes = allCommandTypes
            .Where(t => !testedTypes.Contains(t))
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        if (missingTypes.Any())
        {
            throw new InvalidOperationException(
                $"The following EplCommand types are not tested in {testName}:\n{string.Join("\n", missingTypes)}\n" +
                "Add test instances for these commands to the test list.");
        }
    }
}
