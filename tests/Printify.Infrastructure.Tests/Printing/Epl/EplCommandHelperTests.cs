using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl;
using Printify.Tests.Shared;

namespace Printify.Infrastructure.Tests.Printing.Epl;

public sealed class EplCommandHelperTests
{
    [Fact]
    public void GetDescription_ReturnsNonEmptyDescription_ForAllEplCommandTypes()
    {
        // Create sample commands to test
        var commands = EplTestCommandFactory.CreateSampleEplCommands(withUploadCommands: false);

        // Verify the list is complete via reflection (excluding upload commands)
        CommandTestExtensions.VerifyAllRenderableEplCommandTypesAreTested(commands);

        // Test each command's description
        var failures = new List<string>();

        foreach (var command in commands)
        {
            try
            {
                var description = EplCommandHelper.GetDescription(command);

                if (description == null || description.Count == 0)
                {
                    failures.Add($"{command.GetType().Name}: GetDescription returned null or empty");
                }
                else
                {
                    var emptyLines = description.Where(string.IsNullOrWhiteSpace).ToList();
                    if (emptyLines.Any())
                    {
                        failures.Add($"{command.GetType().Name}: Contains empty/whitespace lines");
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{command.GetType().Name}: Exception - {ex.Message}");
            }
        }

        if (failures.Any())
        {
            Assert.Fail($"GetDescription test failed for {failures.Count} commands:\n{string.Join("\n", failures)}");
        }
    }
}
