﻿using Discord;
using Discord.Gateway;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Broadcaster
{
    public class BroadcastClient
    {
        private DiscordSocketClient _client;
        private GuildInfo _currentGuild;

        public BroadcastClient(string token)
        {
            _client = new DiscordSocketClient();
            _client.OnLoggedIn += OnLoggedIn;
            _client.OnJoinedGuild += OnJoinedGuild;
            _client.OnLeftGuild += OnLeftGuild;
            _client.Login(token);
        }

        private void OnLeftGuild(DiscordSocketClient client, GuildUnavailableEventArgs args)
        {
            Console.WriteLine($"[{client.User}] Left guild {args.Guild.Id}.");

            Program.AvailableGuilds.Enqueue(_currentGuild);
        }

        private void OnJoinedGuild(DiscordSocketClient client, SocketGuildEventArgs args)
        {
            if (args.Guild.Id == _currentGuild.Id)
            {
                Console.WriteLine($"[{client.User}] Joined guild {args.Guild.Id}. Processing...");

                ProcessGuild(args.Guild);
            }
        }

        private void OnLoggedIn(DiscordSocketClient client, LoginEventArgs args)
        {
            Console.WriteLine($"[{client.User}] Logged in.");

            Program.AvailableClients.Enqueue(this);
        }

        public async void BroadcastToAsync(GuildInfo guild)
        {
            await Task.Run(() =>
            {
                _currentGuild = guild;

                try
                {
                    SocketGuild socketGuild = _client.GetCachedGuild(guild.Id);

                    Console.WriteLine($"[{_client.User}] Already in guild {guild.Id}. Processing...");

                    ProcessGuild(socketGuild);
                }
                catch (DiscordHttpException ex)
                {
                    if (ex.Code == DiscordError.UnknownGuild)
                        _client.JoinGuild(guild.Invite);
                    else
                        throw;
                }
            });
        }

        private void ProcessGuild(SocketGuild guild)
        {
            do
            {
                foreach (var channel in guild.Channels.Where(c => c.Type == ChannelType.Voice))
                {
                    var ourRoles = guild.GetMember(_client.User.Id).Roles.Select(r => r.Id);

                    bool connect = true;
                    foreach (var overwrite in channel.PermissionOverwrites)
                    {
                        if (overwrite.Type == PermissionOverwriteType.Role && ourRoles.Contains(overwrite.AffectedId) || overwrite.Type == PermissionOverwriteType.Member && overwrite.AffectedId == _client.User.Id)
                        {
                            if (overwrite.GetPermissionState(DiscordPermission.ConnectToVC) == OverwrittenPermissionState.Deny || overwrite.GetPermissionState(DiscordPermission.SpeakInVC) == OverwrittenPermissionState.Deny)
                            {
                                connect = false;

                                break;
                            }
                        }
                    }

                    // disabled for now because:
                    // if a the client will move onto an available guild if it's needed
                    // issue is we don't know if that guild has any people connected to the vc either
                    /*
                    if (guild.VoiceStates.Where(v => v.Channel != null && v.Channel.Id == channel.Id && v.UserId != _client.User.Id).Count() == 0)
                        connect = false;
                    */

                    if (connect)
                        Speak(channel.ToVoiceChannel());

                    Thread.Sleep(1000);
                }
            }
            while (Program.AvailableGuilds.IsEmpty || !Program.AvailableClients.IsEmpty);

            Console.WriteLine($"[{_client.User}] Done processing guild.");

            Program.AvailableGuilds.Enqueue(_currentGuild);
            Program.AvailableClients.Enqueue(this);
        }


        private void Speak(VoiceChannel channel)
        {
            bool done = false;

            var session = _client.JoinVoiceChannel(channel.Guild.Id, channel.Id);
            session.OnConnected += (s, args) =>
            {
                Console.WriteLine($"[{_client.User}] Speaking in channel.");

                var stream = session.CreateStream(channel.Bitrate);
                stream.CopyFrom(Program.Audio);

                done = true;
            };

            while (!done) { Thread.Sleep(10); }
        }
    }
}