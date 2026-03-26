using ShotCutter.Core.Models;

namespace ShotCutter.Core.Services;

public interface IFFprobeService
{
    Task<VideoInfo> GetVideoInfoAsync(string filePath, CancellationToken ct = default);
}
