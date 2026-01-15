using Printify.Domain.Documents;
using Printify.Domain.Documents.View;

namespace Printify.Application.Interfaces;

public interface IViewDocumentConverter
{
    ViewDocument ToViewDocument(Document document);
}
