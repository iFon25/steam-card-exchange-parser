using System;
using LiteDB;

namespace SteamCardExchangeParser.Models
{
    public class LiteDbEntity
    {
        [BsonId]
        public Guid Id { get; set; }
    }
}
