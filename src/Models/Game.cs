using System;
using System.Collections.Generic;
using LiteDB;

namespace SteamCardExchangeParser.Models
{
    public class Game: LiteDbEntity
    {
        public int SteamAppId { get; set; }
        public string Name { get; set; }
        [BsonRef]
        public List<Card> Cards { get; set; }
    }
}
