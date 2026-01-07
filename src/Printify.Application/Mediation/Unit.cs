namespace Printify.Application.Mediation;

using Mediator.Net.Contracts;

public sealed class Unit : IResponse
{
    public static readonly Unit Value = new();

    private Unit()
    {
    }
}
