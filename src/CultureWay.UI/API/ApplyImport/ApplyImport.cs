using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.ApplyImport;

/// <summary>
/// Imports a file. Takes the same input as <c>PreviewImport</c>, reads the file again and writes the
/// changes in one store call. Rows the preview listed as problems are skipped.
/// </summary>
internal record ApplyImport(string FileName, string ContentBase64, string? Culture, bool OnlyMissing)
    : IRequest<ImportResultDto>;
