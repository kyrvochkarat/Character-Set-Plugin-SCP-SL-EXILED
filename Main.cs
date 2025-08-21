using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using Exiled.Permissions.Extensions;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Player = Exiled.Events.Handlers.Player;

namespace CharSet
{
    public class Main : Plugin<Config>
    {
        public override string Name => "CharSet";
        public override string Author => "vityanvsk";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 8, 1);

        public static Main Instance { get; private set; }
        private Dictionary<string, string> playerDescriptions = new Dictionary<string, string>();

        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Player.Verified += OnVerified;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Player.Verified -= OnVerified;

            Instance = null;

            base.OnDisabled();
        }

        private void OnRoundStarted()
        {
            playerDescriptions.Clear();
        }

        private void OnVerified(VerifiedEventArgs ev)
        {
            if (playerDescriptions.ContainsKey(ev.Player.UserId))
            {
                UpdatePlayerInfo(ev.Player);
            }
        }

        public void SetDescription(Exiled.API.Features.Player player, string description)
        {
            if (player == null) return;

            description = description.Replace("|", "\n");
            playerDescriptions[player.UserId] = description;
            UpdatePlayerInfo(player);
        }

        public void AddDescription(Exiled.API.Features.Player player, string text)
        {
            if (player == null) return;

            text = text.Replace("|", "\n");

            if (playerDescriptions.ContainsKey(player.UserId))
            {
                playerDescriptions[player.UserId] += "\n" + text;
            }
            else
            {
                playerDescriptions[player.UserId] = text;
            }

            UpdatePlayerInfo(player);
        }

        private void UpdatePlayerInfo(Exiled.API.Features.Player player)
        {
            if (player == null || !playerDescriptions.ContainsKey(player.UserId))
                return;

            player.CustomInfo = playerDescriptions[player.UserId];
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class CharCommand : CommandSystem.ICommand
    {
        public string Command => "char";
        public string[] Aliases => new string[] { };
        public string Description => "Управление описанием персонажа игрока";

        public event EventHandler CanExecuteChanged;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("char.use"))
            {
                response = "У вас нет прав на использование этой команды!";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Использование:\n" +
                          "char set <id> <описание> - установить описание\n" +
                          "char add <id> <текст> - добавить текст к описанию\n" +
                          "Используйте | для переноса строки";
                return false;
            }

            string subCommand = arguments.At(0).ToLower();

            switch (subCommand)
            {
                case "set":
                    return ExecuteSet(arguments, out response);
                case "add":
                    return ExecuteAdd(arguments, out response);
                default:
                    response = "Неизвестная подкоманда. Используйте 'set' или 'add'";
                    return false;
            }
        }

        private bool ExecuteSet(ArraySegment<string> arguments, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Использование: char set <id> <описание>";
                return false;
            }

            if (!int.TryParse(arguments.At(1), out int playerId))
            {
                response = "Неверный ID игрока!";
                return false;
            }

            var player = Exiled.API.Features.Player.Get(playerId);
            if (player == null)
            {
                response = $"Игрок с ID {playerId} не найден!";
                return false;
            }

            string description = string.Join(" ", arguments.Array, arguments.Offset + 2, arguments.Count - 2);

            Main.Instance.SetDescription(player, description);

            response = $"Описание для {player.Nickname} установлено!";
            return true;
        }

        private bool ExecuteAdd(ArraySegment<string> arguments, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Использование: char add <id> <текст>";
                return false;
            }

            if (!int.TryParse(arguments.At(1), out int playerId))
            {
                response = "Неверный ID игрока!";
                return false;
            }

            var player = Exiled.API.Features.Player.Get(playerId);
            if (player == null)
            {
                response = $"Игрок с ID {playerId} не найден!";
                return false;
            }

            string text = string.Join(" ", arguments.Array, arguments.Offset + 2, arguments.Count - 2);

            Main.Instance.AddDescription(player, text);

            response = $"Текст добавлен к описанию {player.Nickname}!";
            return true;
        }

        public bool CanExecute(object parameter)
        {
            throw new NotImplementedException();
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}