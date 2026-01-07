namespace Printify.Application.Features.Media.GetMedia;

using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Interfaces;
using Printify.Domain.Services;

public sealed class GetMediaHandler : IRequestHandler<GetMediaQuery, MediaDownloadResult?>
{
    private readonly IDocumentRepository documentRepository;
    private readonly IMediaStorage mediaStorage;

    public GetMediaHandler(IDocumentRepository documentRepository, IMediaStorage mediaStorage)
    {
        this.documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        this.mediaStorage = mediaStorage ?? throw new ArgumentNullException(nameof(mediaStorage));
    }

    public async Task<MediaDownloadResult?> Handle(IReceiveContext<GetMediaQuery> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        // Media is workspace-scoped; do not allow anonymous downloads.
        var media = await documentRepository
            .GetMediaByIdAsync(request.MediaId, cancellationToken)
            .ConfigureAwait(false);
        if (media is null || media.IsDeleted)
        {
            return null;
        }

        var stream = await mediaStorage.OpenReadAsync(request.MediaId, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        return new MediaDownloadResult(stream, media.ContentType, media.Sha256Checksum);
    }
}

