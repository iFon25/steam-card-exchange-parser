using System.Threading.Tasks;
using SteamCardExchangeParser.Models;

namespace SteamCardExchangeParser
{
    public interface IGameInfoLoader
    {
        Task<Game> GetInfo(int steamAppId);
        Task<Game> UpdateInfo(Game game);
    }
}