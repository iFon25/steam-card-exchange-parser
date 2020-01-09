using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using SteamCardExchangeParser.Configuration;
using SteamCardExchangeParser.Models;

namespace SteamCardExchangeParser
{
    public class GameInfoLoader: IGameInfoLoader
    {
        private readonly SteamCardExchangeOptions _steamCardExchangeOptions;
        private readonly HtmlWeb _htmlWeb;

        private const string InventoryGameCardSelector = @"//div[@class='inventory-game-card-item']";

        public GameInfoLoader(IOptions<SteamCardExchangeOptions> steamCardExchangeOptions)
        {
            _steamCardExchangeOptions = steamCardExchangeOptions.Value ?? throw new ArgumentNullException(nameof(steamCardExchangeOptions));
            _htmlWeb = InitializeHtmlWeb();
        }

        private HtmlWeb InitializeHtmlWeb()
        {
            return new HtmlWeb();
        }

        public async Task<Game> GetInfo(int steamAppId)
        {
            var html = await _htmlWeb.LoadFromWebAsync(_steamCardExchangeOptions.BaseUrl + steamAppId.ToString());
            var gameTitle = html.DocumentNode.SelectSingleNode("//span[@class='game-title']").InnerText;
            var inventoryGameCardItems = GetCardItems(html);

            var game = new Game
            {
                Id = Guid.NewGuid(),
                Name = gameTitle,
                SteamAppId = steamAppId,
                Cards = new List<Card>()
            };

            foreach (var cardNode in inventoryGameCardItems)
            {
                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    Name = GetCardName(cardNode),
                    Count = GetCardCount(cardNode),
                    SteamAppId = game.SteamAppId,
                    LastUpdate = DateTime.Now
                };

                game.Cards.Add(card);
            }

            return game;
        }

        public async Task<Game> UpdateInfo(Game game)
        {
            var html = await _htmlWeb.LoadFromWebAsync(_steamCardExchangeOptions.BaseUrl + game.SteamAppId.ToString());
            var inventoryGameCardItems = GetCardItems(html);

            foreach (var cardNode in inventoryGameCardItems)
            {
                var cardName = GetCardName(cardNode);
                var card = game.Cards.FirstOrDefault(c => c.Name == cardName);
                if (card != null)
                {
                    card.Count = GetCardCount(cardNode);
                    card.LastUpdate = DateTime.Now;
                }
            }

            return game;
        }

        private static IEnumerable<HtmlNode> GetCardItems(HtmlDocument html)
        {
            return html.DocumentNode.SelectNodes(InventoryGameCardSelector)
                .Where(n => n.InnerText != "&nbsp;&nbsp;&nbsp;&nbsp;");
        }

        private static string GetCardName(HtmlNode cardNode)
        {
            return cardNode.Descendants("span")
                .FirstOrDefault(d => d.Attributes["class"].Value.Contains("card-name"))?.InnerText;
        }

        private static int GetCardCount(HtmlNode cardNode)
        {
            var cardCountText = cardNode.Descendants("span")
                .FirstOrDefault(d => d.Attributes["class"].Value.Contains("card-amount"))?.InnerText;
            return int.Parse(Regex.Match(cardCountText, @"\d+").Value);
        }
    }
}