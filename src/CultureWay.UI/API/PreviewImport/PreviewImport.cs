using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.PreviewImport;

/// <summary>Reads an import file and reports what importing it would change, without writing anything.</summary>
internal record PreviewImport(string FileName, string ContentBase64, string? Culture, bool OnlyMissing)
    : IRequest<ImportPreviewDto>;
