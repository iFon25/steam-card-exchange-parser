using System;

namespace SteamCardExchangeParser.Models
{
    public class Card: LiteDbEntity
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public int SteamAppId { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}