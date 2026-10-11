using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Kododo.CultureWay.UI.DTO;
using Kododo.CultureWay.UI.ImportExport;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.ApplyImport;

internal sealed class ApplyImportHandler(
    ImportPlanner planner,
    IStore store,
    TranslationCache cache) : IRequestHandler<ApplyImport, ImportResultDto>
{
    public async Task<ImportResultDto> HandleAsync(ApplyImport request, CancellationToken cancellationToken)
    {
        var input = new ImportInput(request.FileName, request.ContentBase64, request.Culture, request.OnlyMissing);
        var plan = await planner.PlanAsync(input, cancellationToken);
        if (plan.Error is not null)
            return new ImportResultDto(plan.Error, 0, 0);

        if (plan.Changes.Count > 0)
        {
            // One call, so a store that records history sees the whole import as one change.
            await store.SetAsync(plan.ToTranslations(), cancellationToken);
            cache.Load(await store.GetAllAsync(cancellationToken));
        }

        var added = plan.Changes.Count(c => c.OldValue is null);
        return new ImportResultDto(null, added, plan.Changes.Count - added);
    }
}
