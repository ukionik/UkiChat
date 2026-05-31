using System.Threading.Tasks;

namespace UkiChat.Services;

public interface IYouTubeApiService
{
    /// <summary>
    /// Возвращает число зрителей текущей трансляции канала, или null если канал не в эфире.
    /// </summary>
    Task<int?> GetViewerCountAsync(string channel);
}
