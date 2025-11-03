using System;
using Printify.Domain.Documents;

namespace Printify.Application.Printing;

public sealed record DocumentStreamEvent(Document Document);
