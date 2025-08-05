using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;

namespace CharPlugin
{
    public class Char : Plugin<Config>
    {
        public override string Author => "vityanvsk";
        public override string Name => "Char";
        public override string Prefix => "char";
        public override Version Version => new Version(1, 1, 0);
        public override Version RequiredExiledVersion => new Version(9, 7, 0);

        public static Char Instance { get; private set; }

        internal Dictionary<string, string> Descriptions = new Dictionary<string, string>();

        public override void OnEnabled()
        {
            Instance = this;
            Exiled.Events.Handlers.Player.Spawning += OnSpawning;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.Spawning -= OnSpawning;
            Instance = null;
            base.OnDisabled();
        }

        private void OnSpawning(SpawningEventArgs ev)
        {
            if (Descriptions.TryGetValue(ev.Player.UserId, out string desc))
            {
                ev.Player.CustomInfo = desc;
            }
        }
    }

    public class Config : IConfig
    {
        [Description("Включен ли плагин")]
        public bool IsEnabled { get; set; }

        [Description("Режим отладки")]
        public bool Debug { get; set; }

        public Config()
        {
            IsEnabled = true;
            Debug = false;
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class CharCommand : ICommand
    {
        public string Command => "char";
        public string[] Aliases => new string[0];
        public string Description => "Добавляет или устанавливает описание игрока (CustomInfo)";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Использование: char set <playerId> <описание> | char add <playerId> <описание>";
                return false;
            }

            string subcommand = arguments.Array[arguments.Offset + 0].ToLower();
            string playerId = arguments.Array[arguments.Offset + 1];

            Player player = Player.Get(playerId);
            if (player == null)
            {
                response = $"Игрок с ID {playerId} не найден.";
                return false;
            }

            string rawDescription = string.Join(" ", arguments.Array, arguments.Offset + 2, arguments.Count - 2);
            string formattedDescription = rawDescription.Replace("|", "\n");

            string userId = player.UserId;

            switch (subcommand)
            {
                case "set":
                    Char.Instance.Descriptions[userId] = formattedDescription;
                    player.CustomInfo = formattedDescription;
                    response = $"Описание для {player.Nickname} установлено.";
                    return true;

                case "add":
                    string current;
                    Char.Instance.Descriptions.TryGetValue(userId, out current);
                    if (string.IsNullOrWhiteSpace(current))
                        current = "";

                    string combined = string.IsNullOrEmpty(current)
                        ? formattedDescription
                        : current + "\n" + formattedDescription;

                    Char.Instance.Descriptions[userId] = combined;
                    player.CustomInfo = combined;
                    response = $"Описание для {player.Nickname} обновлено.";
                    return true;

                default:
                    response = "Неизвестная подкоманда. Используйте: set или add";
                    return false;
            }
        }
    }
}
