using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Epl.Renderers;
using Printify.Infrastructure.Printing.EscPos.Renderers;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Factory for obtaining protocol-specific renderers.
/// Uses DI to resolve renderer instances.
/// </summary>
public sealed class RendererFactory : IRendererFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Protocol, Type> _rendererTypes;

    public RendererFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _rendererTypes = new Dictionary<Protocol, Type>
        {
            [Protocol.EscPos] = typeof(EscPosRenderer),
            [Protocol.Epl] = typeof(EplRenderer)
        };
    }

    /// <inheritdoc />
    public IRenderer GetRenderer(Protocol protocol)
    {
        if (!_rendererTypes.TryGetValue(protocol, out var rendererType))
        {
            throw new NotSupportedException($"Protocol '{protocol}' is not supported.");
        }

        return (IRenderer)_serviceProvider.GetRequiredService(rendererType);
    }
}
