using NoctusExplorer.Core.Models;

namespace NoctusExplorer.UI.Contracts;

public interface ISplitLayout
{
    SplitMode Mode { get; set; }
    double SplitRatio { get; set; }
    IPaneView LeftPane { get; }
    IPaneView RightPane { get; }
    PaneSide ActiveSide { get; }
    void TogglePane();
}
