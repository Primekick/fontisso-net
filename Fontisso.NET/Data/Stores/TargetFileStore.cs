using Fontisso.NET.Data.Models;
using Fontisso.NET.Modules.Flux;
using Fontisso.NET.Services;

namespace Fontisso.NET.Data.Stores;

public record struct ExtractTargetFileDataAction(string FilePath) : IAction;

public record struct TargetFileState(TargetFileData FileData)
{
    public static TargetFileState Default => new(default);
}

public class TargetFileStore : Store<TargetFileState>
{
    private readonly IResourceService _resourceService;
    
    public TargetFileStore(IResourceService resourceService) : base(TargetFileState.Default)
    {
        _resourceService = resourceService;
    }

    public override void Dispatch(IAction action)
    {
        switch (action)
        {
            case ExtractTargetFileDataAction etfda:
                SetState(state => state with { FileData = _resourceService.ExtractTargetFileData(etfda.FilePath) });
                break;
        }
    }
}