﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using System;
using System.Linq;


namespace WoLightning.Game
{
    public class EmoteReaderHooks : IDisposable
    {
        private Plugin Plugin;
        public Action<IPlayerCharacter, IGameObject, ushort> OnEmoteUnrelated;
        public Action<IPlayerCharacter, ushort> OnEmoteIncoming;
        public Action<IGameObject, ushort> OnEmoteOutgoing;
        public Action<ushort> OnEmoteSelf;
        public Action<ushort> OnSitEmote;

        public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
        private readonly Hook<OnEmoteFuncDelegate> hookEmote;

        public bool IsValid = false;

        internal bool log = false;

        public EmoteReaderHooks(Plugin plugin)
        {
            Plugin = plugin;
            try
            {
                hookEmote = Plugin.GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
                hookEmote.Enable();
                Plugin.Log("Started EmoteReaderHook!");
                IsValid = true;
            }
            catch (Exception ex)
            {
                Plugin.Error(ex + "");
            }
        }

        public void Dispose()
        {
            hookEmote?.Dispose();
            IsValid = false;
        }

        void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
        {

            try // todo: clean this up a bit
            {
                if (Plugin.ClientState.LocalPlayer != null)
                {
                    if (targetId == Plugin.ClientState.LocalPlayer.GameObjectId) // we are the target
                    {
                        if (log) Plugin.PluginLog.Verbose($"We are the target - {emoteId} - {targetId}");
                        var instigatorOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
                        if (instigatorOb != null) OnEmoteIncoming?.Invoke((IPlayerCharacter)instigatorOb, emoteId); // someone is sending a emote targeting us
                    }
                    else // We are not the target
                    {
                        if (log) Plugin.PluginLog.Verbose($"We are not the target - {emoteId} - {targetId}");
                        var targetOb = Plugin.ObjectTable.FirstOrDefault(x => x.GameObjectId == targetId);
                        var instigatorOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
                        if (instigatorOb == null || targetOb == null) //bad data or special emote
                        {
                            if (log) Plugin.PluginLog.Verbose($"inst {instigatorOb} - targ {targetOb}");
                            if (emoteId >= 50 && emoteId <= 52 && instigatorOb.GameObjectId == Plugin.ClientState.LocalPlayer.GameObjectId) OnSitEmote?.Invoke(emoteId); // /sit - getup - /groundsit
                            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
                            return;
                        }
                        if (targetOb.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) // No Target can be found
                        {
                            if (log) Plugin.PluginLog.Verbose($"No Target");
                            if (instigatorOb.GameObjectId == Plugin.ClientState.LocalPlayer.GameObjectId) OnEmoteSelf?.Invoke(emoteId); // we are sending an emote without target
                            else OnEmoteUnrelated?.Invoke((IPlayerCharacter)instigatorOb, targetOb, emoteId); // seomeone is sending a emote without target
                        }
                        else
                        {
                            if (log) Plugin.PluginLog.Verbose($"Someone is targeting someone");
                            if (instigatorOb.GameObjectId == Plugin.ClientState.LocalPlayer.GameObjectId) OnEmoteOutgoing?.Invoke(targetOb, emoteId); // we are sending an emote
                            else OnEmoteUnrelated?.Invoke((IPlayerCharacter)instigatorOb, targetOb, emoteId); // someone is sending a emote to someone else
                        }

                    }
                }
            }
            catch (Exception ex) { Plugin.Error(ex.ToString()); }

            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
        }
    }
}