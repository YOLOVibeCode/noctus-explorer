namespace NoctusExplorer.Core.Models;

public sealed record TabState(
    int Id,
    PathRef Location,
    ViewMode ViewMode = ViewMode.Details,
    SortField SortField = SortField.Name,
    SortDirection SortDirection = SortDirection.Ascending,
    double? ScrollPosition = null);
