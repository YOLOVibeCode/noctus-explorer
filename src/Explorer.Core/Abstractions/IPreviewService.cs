using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Generates preview content for the preview pane.
/// Returns platform-opaque data that the UI layer knows how to render.
/// </summary>
public interface IPreviewService
{
    bool CanPreview(PathRef item);
    Task<object> GetPreviewAsync(PathRef item, PreviewSize maxSize, CancellationToken ct = default);
}

public readonly record struct PreviewSize(int Width, int Height);
