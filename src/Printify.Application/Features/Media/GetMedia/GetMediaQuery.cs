namespace Printify.Application.Features.Media.GetMedia;

using System.IO;
using Mediator.Net.Contracts;

public sealed record GetMediaQuery(Guid MediaId) : IRequest;

public sealed record MediaDownloadResult(Stream Content, string ContentType, string? Checksum) : IResponse;

