using Printify.Domain.Documents;
using Printify.Domain.Documents.View;

namespace Printify.Application.Features.Printers.Documents.View;

public interface IViewDocumentConverter
{
    ViewDocument ToViewDocument(Document document);
}
