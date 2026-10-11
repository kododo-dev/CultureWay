using Kododo.CultureWay.UI.DTO;
using Kododo.CultureWay.UI.ImportExport;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.PreviewImport;

internal sealed class PreviewImportHandler(ImportPlanner planner) : IRequestHandler<PreviewImport, ImportPreviewDto>
{
    public async Task<ImportPreviewDto> HandleAsync(PreviewImport request, CancellationToken cancellationToken)
    {
        var input = new ImportInput(request.FileName, request.ContentBase64, request.Culture, request.OnlyMissing);
        var plan = await planner.PlanAsync(input, cancellationToken);
        return plan.ToPreview();
    }
}
