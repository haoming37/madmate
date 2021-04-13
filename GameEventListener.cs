using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Impostor.Api.Events;
using Impostor.Api.Events.Player;
using Impostor.Api.Innersloth;
using Impostor.Api.Net;
using Microsoft.Extensions.Logging;

namespace Impostor.Plugins.Example.Handlers
{
    /// <summary>
    ///     A class that listens for two events.
    ///     It may be more but this is just an example.
    ///
    ///     Make sure your class implements <see cref="IEventListener"/>.
    /// </summary>
    public class GameEventListener : IEventListener
    {
        private readonly ILogger<ExamplePlugin> _logger;
        readonly string[] _mapNames = Enum.GetNames(typeof(MapTypes));
        private List<byte> madMates = new List<byte>();
        private List<byte> spys = new List<byte>();
        private int numMadMates;
        private int numSpys;

        public GameEventListener(ILogger<ExamplePlugin> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     An example event listener.
        /// </summary>
        /// <param name="e">
        ///     The event you want to listen for.
        /// </param>
        [EventListener]
        public void OnGameStarted(IGameStartedEvent e)
        {
            _logger.LogInformation($"Game is starting.");
            _logger.LogInformation($"Number of MadMate is ${numMadMates}");
            _logger.LogInformation($"Number of Spy is ${numSpys}");
            IEnumerable<IClientPlayer> players = e.Game.Players;
            int numPlayers = players.Count();
            Random rand = new System.Random();

            // MadMatesを追加
            while(madMates.Count < numMadMates)
            {
                int randVal = rand.Next(0, numMadMates);
                IClientPlayer pickedPlayer = players.ElementAt(randVal);
                bool isImpostor = pickedPlayer.Character.PlayerInfo.IsImpostor;
                byte playerId = pickedPlayer.Character.PlayerId;
                if(!isImpostor)
                {
                    madMates.Add(playerId);
                    pickedPlayer.Character.SendChatToPlayerAsync($"you are choosen as a MadMate", pickedPlayer.Character);
                }
            }
            // Spyを追加
            while(spys.Count < numSpys)
            {
                int randVal = rand.Next(0, numSpys);
                IClientPlayer pickedPlayer = players.ElementAt(randVal);
                bool isImpostor = pickedPlayer.Character.PlayerInfo.IsImpostor;
                byte playerId = pickedPlayer.Character.PlayerId;
                if(!isImpostor && !madMates.Contains(playerId))
                {
                    spys.Add(playerId);
                    pickedPlayer.Character.SendChatToPlayerAsync($"you are choosen as a Spy", pickedPlayer.Character);
                    foreach(IClientPlayer player in players )
                    {
                        var info = player.Character.PlayerInfo;
                        if(info.IsImpostor)
                        {
                            pickedPlayer.Character.SendChatToPlayerAsync($"{info.PlayerName} is an impostor", pickedPlayer.Character);
                        }

                    }
                }
            }
        }

        [EventListener]
        public void OnGameEnded(IGameEndedEvent e)
        {
            _logger.LogInformation($"Game has ended.");
            // This prints out for all players if they are impostor or crewmate.
            foreach (var player in e.Game.Players)
            {
                var id = player.Character.PlayerId;
                var info = player.Character.PlayerInfo;
                var isSpy = spys.Contains(id);
                var isMadMate = madMates.Contains(id); 
                var isImpostor = info.IsImpostor;
                if (isImpostor)
                {
                    _logger.LogInformation($"- {info.PlayerName} is an impostor.");
                    player.Character.SendChatAsync($"{info.PlayerName} is an impostor");
                }
                else if (isSpy)
                {
                    _logger.LogInformation($"- {info.PlayerName} is a spy.");
                    player.Character.SendChatAsync($"{info.PlayerName} is a spy");
                }
                else if (isMadMate)
                {
                    _logger.LogInformation($"- {info.PlayerName} is a madmate.");
                    player.Character.SendChatAsync($"{info.PlayerName} is a madmate");
                }
                else
                {
                    _logger.LogInformation($"- {info.PlayerName} is a crewmate.");
                }
            }
        }

        [EventListener]
        public void OnPlayerChat(IPlayerChatEvent e)
        {
            _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} said {e.Message}");
            if (e.Game.GameState != GameStates.NotStarted || !e.Message.StartsWith("/") || !e.ClientPlayer.IsHost)
                return;
            Task.Run(async () => await DoCommands(e));
        }
        private async Task DoCommands(IPlayerChatEvent e)
        {
            _logger.LogDebug($"Attempting to evaluate command from {e.PlayerControl.PlayerInfo.PlayerName} on {e.Game.Code.Code}. Message was: {e.Message}");

            string[] parts = e.Message.ToLowerInvariant()[1..].Split(" ");

            switch (parts[0])
            {
                case "impostors":
                    if (parts.Length == 1)
                    {
                        await e.PlayerControl.SendChatAsync($"Please specify the number of impostors.");
                        return;
                    }

                    if (int.TryParse(parts[1], out int num))
                    {
                        num = Math.Clamp(num, 1, 3);
                        await e.PlayerControl.SendChatAsync($"Setting the number of impostors to {num}");

                        e.Game.Options.NumImpostors = num;
                        await e.Game.SyncSettingsAsync();
                    }
                    else
                        await e.PlayerControl.SendChatAsync($"Unable to convert '{parts[1]}' to a number!");
                    break;
                case "spy":
                    if (parts.Length == 1)
                    {
                        await e.PlayerControl.SendChatAsync($"Please specify the number of spy.");
                        return;
                    }

                    if (int.TryParse(parts[1], out int num_spy))
                    {
                        num_spy = Math.Clamp(num_spy, 1, 3);
                        await e.PlayerControl.SendChatAsync($"Setting the number of spy to {num_spy}");

                        numSpys = num_spy;
                    }
                    else
                        await e.PlayerControl.SendChatAsync($"Unable to convert '{parts[1]}' to a number!");
                    break;
                case "madmate":
                    if (parts.Length == 1)
                    {
                        await e.PlayerControl.SendChatAsync($"Please specify the number of madmate.");
                        return;
                    }

                    if (int.TryParse(parts[1], out int num_madmate))
                    {
                        num_madmate = Math.Clamp(num_madmate, 1, 3);
                        await e.PlayerControl.SendChatAsync($"Setting the number of madmate to {num_madmate}");

                        numMadMates = num_madmate;
                        await e.Game.SyncSettingsAsync();
                    }
                    else
                        await e.PlayerControl.SendChatAsync($"Unable to convert '{parts[1]}' to a number!");
                    break;
                case "map":
                    if (parts.Length == 1)
                    {
                        await e.PlayerControl.SendChatAsync($"Please specify the map. Accepted values: {string.Join(", ", _mapNames)}");
                        return;
                    }

                    if (!_mapNames.Any(name => name.ToLowerInvariant() == parts[1]))
                    {
                        await e.PlayerControl.SendChatAsync($"Unknown map. Accepted values: {string.Join(", ", _mapNames)}");
                        return;
                    }

                    MapTypes map = Enum.Parse<MapTypes>(parts[1], true);

                    await e.PlayerControl.SendChatAsync($"Setting map to {map}");

                    e.Game.Options.Map = map;
                    await e.Game.SyncSettingsAsync();
                    break;
                default:
                    _logger.LogInformation($"Unknown command {parts[0]} from {e.PlayerControl.PlayerInfo.PlayerName} on {e.Game.Code.Code}.");
                    break;
            }
        }
    }
}