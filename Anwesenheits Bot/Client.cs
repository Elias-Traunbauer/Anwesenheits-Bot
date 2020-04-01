using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Anwesenheits_Bot
{
    public class Client
    {
        public DiscordSocketClient _client;

        public string _token;

        public List<Student> _students;

        public bool listOpen;

        char prefix = '+';

        IUserMessage list;

        public Client(string token)
        {
            _client = new DiscordSocketClient();

            _token = token;
        }

        public async Task InitializeAsync()
        {
            await _client.StartAsync();

            await _client.LoginAsync(TokenType.Bot, _token);

            await SetupEventHandler();

            await Task.Delay(-1);
        }

        private async Task SetupEventHandler()
        {
            _client.MessageReceived += client_MessageReceived;

            _client.ReactionAdded += client_ReactionAdded;

            _client.Log += client_Log;
        }

        private async Task client_Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message + arg.Exception?.Message);
        }

        private async Task client_MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot)
            {
                return;
            }

            try
            {
                var msg = message as SocketUserMessage;

                var context = new SocketCommandContext(_client, msg);

                var text = msg.Content;

                if (text[0] != prefix) return;

                text = text.Substring(1);

                await ExecuteCommandAsync(text, context);

                await message.DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task client_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (listOpen)
            {
                if (reaction.Emote.Name.Equals("✅"))
                {
                    string nickname = ((IGuildUser)reaction.User.Value).Nickname;

                    int i = 0;

                    foreach (var item in _students)
                    {
                        if (nickname == item._name)
                        {
                            _students[i]._veryfied = true;

                            await UpdateList();

                            break;
                        }

                        i++;
                    }
                }
            }
        }

        private async Task ExecuteCommandAsync(string command, SocketCommandContext context)
        {
            switch (command)
            {
                case "ls":
                    await StartList(context);
                    break;

                case "a":
                    if (listOpen)
                    {
                        //if (((SocketGuildUser)context.Message.Author).Roles)
                        //{

                        //}
                    }

                    break;

                case "ende":
                    await list.DeleteAsync();

                    listOpen = false;
                    break;

                default:
                    break;
            }
        }

        private async Task StartList(SocketCommandContext context)
        {
            if (!listOpen)
            {
                List<SocketUser> students = new List<SocketUser>();

                foreach (var user in context.Guild.Users)
                {
                    if (user.Status != UserStatus.Offline)
                    foreach (var role in user.Roles)
                    {
                        if (role.Name == "Schüler")
                        {
                            students.Add(user);

                            break;
                        }
                    }
                }

                _students = new List<Student>();

                foreach (var user in students)
                {
                    _students.Add(new Student((user as IGuildUser).Nickname));
                }

                list = await context.Channel.SendMessageAsync("", false, GetEmbed().Build());

                listOpen = true;

                await list.AddReactionAsync(new Emoji("✅"));
            }
        }

        private async Task UpdateList()
        { 
            await list.ModifyAsync(m => m.Embed = GetEmbed().Build());
        }

        private EmbedBuilder GetEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();

            e.WithTitle("Anwesenheits Liste");

            e.WithDescription($"Verifiziere dich indem du auf den Häckchen button drückst! Melde dich ggf. mit {prefix}a an!");

            string verified = "";

            string notverified = "";

            Student[] students = _students.ToArray();

            Array.Sort(students, (x, y) => String.Compare(x._name, y._name));

            foreach (var student in students)
            {
                if (student._veryfied)
                {
                    verified += student._name + "\n";
                }
                else
                {
                    notverified += student._name + "\n";
                }
            }

            if (verified != "")
            {
                e.AddField("Verifiziert:", verified);
            }
            else
            {
                e.AddField("Verifiziert:", "-");
            }

            if (notverified != "")
            {
                e.AddField("Nicht verifiziert:", notverified);
            }
            else
            {
                e.AddField("Nicht verifiziert:", "-");
            }

            e.WithColor(Color.DarkRed);

            return e;
        }
    }

    public class Student
    {
        public string _name;
        public bool _veryfied;

        public Student(string name)
        {
            _name = name;

            _veryfied = false;
        }
    }
}
