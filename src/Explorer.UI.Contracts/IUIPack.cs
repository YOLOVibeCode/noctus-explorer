using NoctusExplorer.Core.ViewModels;

namespace NoctusExplorer.UI.Contracts;

public interface IUIPack
{
    string Name { get; }
    string PlatformRequirement { get; }
    void Run(MainViewModel rootVm, IServiceProvider services);
}
