using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SP.Tools;

namespace GameCore.High
{
    public static class TalkMessageCenter
    {
        /// <summary>
        /// 可存入模组自定义执行的指令, string[] 即为分割好的命令
        /// </summary>
        public static event Action<string[]> onExecuteCommand = parts =>
        {
            Speed(parts);
        };
        public static string commandContent;

        public const string escapeCharacter = "*";
        public const string escapeCharacterPlayers = escapeCharacter + "players";


        /// <summary>
        /// 执行指令
        /// </summary>
        public static void ExecuteCommand()
        {
            if (commandContent.IsNullOrEmpty())
                return;

            string[] splitted = commandContent.Split(' ');

            Debug.Log($"执行了一条指令, 长度为 {splitted.Length}, 具体内容为<' {commandContent} '>");
            MethodAgent.TryRun(() => onExecuteCommand(splitted));
        }

        static void Speed(string[] command)
        {
            if (command[0] != "/speed")
                return;

            if (command.Length == 1)
            {
                var p = Player.local;

                if (p == null)
                    return;

                p.velocityFactor = p.velocityFactor.Multiply(2);
            }
            else if (command.Length == 2)
            {
                List<Player> ps = command[1] switch
                {
                    escapeCharacterPlayers => PlayerCenter.all,
                    _ => PlayerCenter.all.Where(x => x.playerName == command[1]).ToList()
                };

                foreach (Player p in ps)
                    p.velocityFactor = () => command[1].ToFloat();
            }
        }
    }
}
