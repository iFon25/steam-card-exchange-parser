using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SteamCardExchangeParser.Configuration;
using SteamCardExchangeParser.Models;

namespace SteamCardExchangeParser
{
    public class Worker : BackgroundService
    {
        private readonly IGameInfoLoader _gameInfoLoader;
        
        private readonly ServiceOptions _serviceOptions;
        private readonly LiteRepository _repository;
        private readonly ConnectionString _connectionString;

        public Worker(IOptions<ServiceOptions> serviceOptions, IGameInfoLoader gameInfoLoader)
        {
            _serviceOptions = serviceOptions?.Value ?? throw new ArgumentNullException(nameof(serviceOptions));
            _gameInfoLoader = gameInfoLoader ?? throw new ArgumentNullException(nameof(gameInfoLoader));
            _connectionString = CreateConnectionString();
            _repository = new LiteRepository(_connectionString);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var allTimeUpdate = new List<decimal>();
            while (!stoppingToken.IsCancellationRequested)
            {
                var stopWatch = new Stopwatch();
                var singleTimeUpdate = new List<decimal>();
                using (_repository)
                {
                    var requiredGames = _repository.Query<RequiredGame>().ToList();
                    Console.WriteLine($"[{DateTime.Now:O}] Будет обновлено {requiredGames.Count} игр.");
                    foreach (var requiredGame in requiredGames)
                    {
                        stopWatch.Restart();
                        var game = _repository.Query<Game>()
                            .Include(g => g.Cards)
                            .Where(g => g.SteamAppId == requiredGame.SteamAppId)
                            .FirstOrDefault();
                        if (game is null)
                        {
                            game = await _gameInfoLoader.GetInfo(requiredGame.SteamAppId);
                        }
                        else
                        {
                            game = await _gameInfoLoader.UpdateInfo(game);
                        }

                        game.Cards.ForEach(card => _repository.Upsert(card));
                        _repository.Upsert(game);
                        stopWatch.Stop();
                        singleTimeUpdate.Add((decimal)stopWatch.Elapsed.TotalMilliseconds);
                        Console.WriteLine($"Игра {game.Name} обновлена за {stopWatch.Elapsed:g}.");
                    }
                }

                if (singleTimeUpdate.Count > 0)
                {
                    var singleAvgUpdateTime = (double) singleTimeUpdate.Average();
                    Console.WriteLine(
                        $"Среднее время обновления игр за последний заход составило {TimeSpan.FromMilliseconds(singleAvgUpdateTime):g}.");

                    allTimeUpdate.AddRange(singleTimeUpdate);
                    var avgUpdateTime = (double) allTimeUpdate.Average();
                    Console.WriteLine(
                        $"Общее среднее время обновления игр составляет {TimeSpan.FromMilliseconds(avgUpdateTime):g}.");
                }
                else
                {
                    Console.WriteLine("В списке нет игр на обновление.");
                }

                await Task.Delay(TimeSpan.FromMinutes(_serviceOptions.UpdateDelay), stoppingToken);
            }
        }

        private ConnectionString CreateConnectionString()
        {
            var dataPath = Path.Combine(_serviceOptions.AppData);
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            return new ConnectionString
            {
                Filename = Path.Combine(dataPath, "SteamCardExchange.db"),
                Mode = ConnectionMode.Shared,
                Upgrade = true
            };
        }

    }
}
