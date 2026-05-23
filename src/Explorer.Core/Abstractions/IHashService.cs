using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Computes cryptographic hashes for files.
/// </summary>
public interface IHashService
{
    Task<string> ComputeHashAsync(
        PathRef file,
        HashAlgorithmType algorithm,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
