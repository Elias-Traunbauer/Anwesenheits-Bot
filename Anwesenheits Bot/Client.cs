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

        public List<string> _offline = new List<string>();

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

            _client.GuildMemberUpdated += client_GuildMemberUpdated;

            _client.MessageDeleted += client_MessageDeleted;
        }

        private async Task client_MessageDeleted(Cacheable<IMessage, ulong> msg, ISocketMessageChannel channel)
        {
            if (msg.Id == list?.Id && listOpen)
            {
                list = await channel.SendMessageAsync("Diese Liste nicht löschen!");

                await list.AddReactionAsync(new Emoji("✅"));

                await UpdateList();
            }
        }

        private async Task client_GuildMemberUpdated(SocketGuildUser user, SocketGuildUser none)
        {
            try
            {
                if (listOpen)
                {
                    foreach (var item in user.Roles)
                    {
                        if (item.Name == "Schüler") //jaja geht schöner
                        {
                            if (user.Status != UserStatus.Online)
                            {
                                foreach (var temp in _offline)
                                {
                                    if (temp == user.Nickname)
                                    {
                                        _offline.Remove(temp);

                                        _students.Add(new Student(temp, DateTime.Now.ToShortTimeString().ToString() + " Uhr"));
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var tep in _students)
                                {
                                    if (tep._name == user.Nickname)
                                    {
                                        _students.Remove(tep);

                                        _offline.Add(tep._name);
                                        break;
                                    }
                                }
                            }

                            await UpdateList();

                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task client_Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message + arg.Exception?.Message);
        }

        private async Task client_MessageReceived(SocketMessage message)
        {
            if (message.Channel.Name != "anwesenheit")
            {
                return;
            }

            if (message.Author.IsBot)
            {
                return;
            }

            await message.DeleteAsync();

            try
            {
                var msg = message as SocketUserMessage;

                var context = new SocketCommandContext(_client, msg);

                var text = msg.Content;

                if (text[0] != prefix) return;

                text = text.Substring(1);

                await ExecuteCommandAsync(text, context);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task client_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (listOpen && message.Id == list.Id)
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

                            _students[i]._time = DateTime.Now.ToShortTimeString().ToString() + " Uhr";

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

                case "ende":
                    await list.DeleteAsync();

                    _offline = new List<string>();

                    listOpen = false;
                    break;

                case "v":
                    if (listOpen)
                    {
                        foreach (Student item in _students)
                        {
                            item._veryfied = false;
                        }

                        await list.RemoveAllReactionsAsync();

                        await list.AddReactionAsync(new Emoji("✅"));

                        await UpdateList();
                    }
                    break;

                case "hilfe":
                    await context.Channel.SendMessageAsync("", false, new EmbedBuilder().WithTitle("Kommandos").AddField("Generell", "+ls     -> Liste Starten.\n+v      -> neue Verifizierung.\n+ende   -> liste Beenden.\n+hilfe  -> Hilfe anzeigen.").Build());
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
                    else
                        foreach (var role in user.Roles)
                        {
                            if (role.Name == "Schüler")
                            {
                                _offline.Add((user as IGuildUser).Nickname);

                                break;
                            }
                        }
                }

                _students = new List<Student>();

                foreach (var user in students)
                {
                    _students.Add(new Student((user as IGuildUser).Nickname, "seit Anfang"));
                }

                list = await context.Channel.SendMessageAsync("", false, GetEmbed().Build());

                listOpen = true;

                await list.AddReactionAsync(new Emoji("✅"));
            }
        }

        private async Task UpdateList()
        {
            await list.ModifyAsync(m => { m.Embed = GetEmbed().Build(); m.Content = ""; });
        }

        private EmbedBuilder GetEmbed()
        {
            EmbedBuilder e = new EmbedBuilder();

            e.WithTitle("Anwesenheits Liste");

            e.WithDescription($"Beweise deine geistige Anwesenheit, inedm du auf das ✅ drückst!");

            string verified = "";

            string notverified = "";

            Student[] students = _students.ToArray();

            Array.Sort(students, (x, y) => String.Compare(x._name, y._name));

            int verifyCount = 0;

            int noVerifyCount = 0;

            string time = "";

            foreach (var student in students)
            {
                if (student._veryfied)
                {
                    verified += student._name + "\n";

                    time += student._time + "\n";

                    verifyCount++;
                }
                else
                {
                    notverified += student._name + "\n";

                    noVerifyCount++;
                }
            }

            if (verified != "")
            {
                e.AddField($"{verifyCount} geistig anwesend:", verified, true);

                e.AddField("Zeit:", time, true);
            }
            else
            {
                e.AddField("0 geistig anwesend:", "-", true);

                e.AddField("Zeit:", "-", true);
            }

            if (notverified != "")
            {
                e.AddField($"{noVerifyCount} online:", notverified);
            }
            else
            {
                e.AddField("0 online:", "-");
            }

            int offlineCount = 0;

            string offline = "";

            string[] off = _offline.ToArray();

            Array.Sort(off, (x, y) => String.Compare(x, y));

            foreach (var item in off)
            {
                offline += item + "\n";

                offlineCount++;
            }

            if (offline != "")
            {
                e.AddField($"{offlineCount} offline:", offline);
            }
            else
            {
                e.AddField("0 offline:", "-");
            }

            e.WithColor(Color.DarkRed);

            return e;
        }
    }

    public class Student
    {
        public string _name;
        public bool _veryfied;
        public string _time;

        public Student(string name, string time)
        {
            _name = name;

            _veryfied = false;

            _time = time;
        }
    }
}
