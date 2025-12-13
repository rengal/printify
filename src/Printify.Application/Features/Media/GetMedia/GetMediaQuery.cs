namespace Printify.Application.Features.Media.GetMedia;

using System.IO;
using MediatR;

public sealed record GetMediaQuery(Guid MediaId) : IRequest<MediaDownloadResult?>;

public sealed record MediaDownloadResult(Stream Content, string ContentType, string? Checksum);
