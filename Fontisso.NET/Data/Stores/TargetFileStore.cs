using System.Threading.Tasks;
using Fontisso.NET.Data.Models;
using Fontisso.NET.Flux;
using Fontisso.NET.Services;
using OneOf;

namespace Fontisso.NET.Data.Stores;

public record ExtractTargetFileDataAction(string FilePath) : IAction;

public record TargetFileState(OneOf<TargetFileData, ExtractionError> FileData)
{
    public static TargetFileState Default => new(TargetFileData.Default);
}

public class TargetFileStore : Store<TargetFileState>
{
    private readonly IResourceService _resourceService;
    
    public TargetFileStore(IResourceService resourceService) : base(TargetFileState.Default)
    {
        _resourceService = resourceService;
    }

    public override async Task Dispatch(IAction action)
    {
        switch (action)
        {
            case ExtractTargetFileDataAction etfda:
                var targetFileData = await _resourceService.ExtractTargetFileData(etfda.FilePath);
                SetState(state => state with { FileData = targetFileData });
                break;
        }

        await Task.CompletedTask;
    }
}