using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.CreateWorkspace;

public sealed class CreateWorkspaceHandler(IWorkspaceRepository workspaceRepository) : IRequestHandler<CreateWorkspaceCommand, Workspace>
{
    public async Task<Workspace> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            // Simplified idempotency: return the existing entity without reapplying side effects.
            return existing;
        }

        // NOTE: Simplified idempotency – we only rely on the supplied identifier.
        // We do not store the original response payload, so repeated calls could observe new fields if contracts evolve.
        var token = GenerateRandomToken();

        var workspace = new Workspace(
            request.WorkspaceId,
            request.OwnerName,
            token,
            DateTimeOffset.UtcNow,
            request.Context.IpAddress,
            false);

        // Persist immediately so the workspace becomes visible for authentication flows.
        await workspaceRepository.AddAsync(workspace, cancellationToken).ConfigureAwait(false);

        return workspace;
    }

    private string GenerateRandomToken()
    {
        var adjectives = new[]
        {
            "brave", "quick", "clever", "fierce", "mighty",
            "swift", "bold", "wise", "noble", "bright",
            "silent", "wild", "calm", "proud", "keen",
            "agile", "sturdy", "sharp", "loyal", "daring",
            "gentle", "ancient", "mystic", "royal", "hidden",
            "sacred", "golden", "silver", "cosmic", "stellar",
            "crimson", "azure", "emerald", "radiant", "shadow",
            "thunder", "crystal", "iron", "marble", "velvet",
            "frost", "flame", "storm", "wind", "ocean",
            "mountain", "forest", "lunar", "solar", "arctic",
            "tropic", "desert", "jungle", "savage", "primal",
            "electric", "magnetic", "atomic", "quantum", "stellar",
            "heroic", "legendary", "epic", "majestic", "sovereign",
            "vigilant", "stealthy", "cunning", "fearless", "relentless",
            "eternal", "infinite", "timeless", "ancient", "primordial",
            "blazing", "frozen", "molten", "tempered", "forged"
        };
        
        var animals = new[]
        {
            "tiger", "eagle", "shark", "wolf", "lion",
            "falcon", "bear", "fox", "hawk", "panther",
            "raven", "cobra", "lynx", "orca", "jaguar",
            "phoenix", "dragon", "griffin", "owl", "leopard",
            "cheetah", "puma", "cougar", "rhino", "elephant",
            "gorilla", "bison", "moose", "elk", "stag",
            "stallion", "mustang", "viper", "python", "anaconda",
            "scorpion", "mantis", "hornet", "wasp", "spider",
            "octopus", "kraken", "barracuda", "marlin", "swordfish",
            "wolverine", "badger", "otter", "seal", "walrus",
            "penguin", "albatross", "condor", "vulture", "kestrel",
            "sparrow", "finch", "cardinal", "bluejay", "mockingbird",
            "hummingbird", "kingfisher", "osprey", "kite", "harrier",
            "mongoose", "meerkat", "lemur", "gibbon", "orangutan",
            "chimp", "baboon", "macaque", "mandrill", "howler",
            "sloth", "armadillo", "anteater", "aardvark", "wombat",
            "koala", "kangaroo", "wallaby", "platypus", "echidna"
        };
        

        var adjective = adjectives[Random.Shared.Next(adjectives.Length)];
        var animal = animals[Random.Shared.Next(animals.Length)];
        var number = Random.Shared.Next(1000, 9999);
        return $"{adjective}-{animal}-{number}";
    }
}
