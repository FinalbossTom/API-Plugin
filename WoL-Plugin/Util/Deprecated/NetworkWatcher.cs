using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using System.Numerics;
using WoLightning.Util;
using WoLightning.Util.Types;


namespace WoLightning.Game
{
    internal class NetworkWatcher // DEPRECATED CLASS
    {
        public bool running = false;
        Plugin Plugin;
        Preset ActivePreset;

        private IPlayerCharacter? LocalPlayer;
        private uint lastHP = 0;
        private uint lastMaxHP = 0;
        private uint lastMP = 0;
        private bool wasDead = true;

        private uint lastVulnAmount = 0;
        private uint lastDDownAmount = 0;

        public int statusCheckInterval = 60;
        private int lastStatusCheck = 0;

        public int partyCheckInterval = 60;
        private int lastPartyCheck = 0;
        private int lastCheckedIndex;
        private readonly bool[] deadIndexes = [false, false, false, false, false, false, false, false]; //how do i polyfill
        private int amountDead = 0;

        private bool sittingOnChair = false;
        private Vector3 sittingOnChairPos = new();
        readonly private TimerPlus sittingOnChairTimer = new();

        readonly internal string[] FirstPersonWords = ["i", "i'd", "i'll", "me", "my", "myself", "mine"];

        public NetworkWatcher(Plugin plugin)
        {
            Plugin = plugin;
            Plugin.Log("NetworkWatchter Constructor Called");
            Plugin.Log("constructor Finished");
            //sittingOnChairTimer.Elapsed += checkSittingOnChair;
        }

        public void Start() //Todo only start specific services, when respective trigger is on
        {
            running = true;
            ActivePreset = Plugin.Configuration.ActivePreset;

            Plugin.Framework.Update += checkLocalPlayerState;

            Plugin.ChatGui.ChatMessage += HandleChatMessage;
            //Plugin.ClientState.TerritoryChanged += HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf += OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated += OnEmoteUnrelated;
            Plugin.EmoteReaderHooks.OnSitEmote += OnSitEmote;
        }
        public void Stop()
        {

            Plugin.Framework.Update -= checkLocalPlayerState;

            Plugin.ChatGui.ChatMessage -= HandleChatMessage;
            //Plugin.ClientState.TerritoryChanged -= HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated -= OnEmoteUnrelated;
            Plugin.EmoteReaderHooks.OnSitEmote -= OnSitEmote;
            running = false;
        }
        public void Dispose()
        {

            Plugin.Framework.Update -= checkLocalPlayerState;

            Plugin.ChatGui.ChatMessage -= HandleChatMessage;
            //Plugin.ClientState.TerritoryChanged -= HandlePlayerTerritoryChange;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= OnEmoteOutgoing;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= OnEmoteSelf;
            Plugin.EmoteReaderHooks.OnEmoteUnrelated -= OnEmoteUnrelated;
            Plugin.EmoteReaderHooks.OnSitEmote -= OnSitEmote;
            running = false;
        }


        private void checkLocalPlayerState(IFramework Framework)
        {
            /*
            try
            {
                if (LocalPlayer == null)
                {
                    if (Plugin.ClientState.LocalPlayer == null) return;
                    LocalPlayer = Plugin.ClientState.LocalPlayer;
                    lastHP = LocalPlayer.CurrentHp;
                    lastMaxHP = LocalPlayer.MaxHp;
                    lastMP = LocalPlayer.CurrentMp;
                }

                if (lastHP != LocalPlayer.CurrentHp) HandleHPChange();
                lastHP = LocalPlayer.CurrentHp;

                if (lastMP != LocalPlayer.CurrentMp) HandleMPChange();
                lastMP = LocalPlayer.CurrentMp;

                if (lastStatusCheck >= statusCheckInterval && ActivePreset.FailMechanic.IsEnabled()) HandleStatusCheck();
                if (lastPartyCheck >= partyCheckInterval && ActivePreset.PartymemberDies.IsEnabled() && Plugin.PartyList.Length > 0) HandlePartyMembers();

                lastStatusCheck++;
                lastPartyCheck++;
            }
            catch (Exception e)
            {
                Plugin.Error(e.ToString());
            }
        
            */
        }

        /*
        private void HandleHPChange()
        {
            if (lastMaxHP != LocalPlayer.MaxHp)
            {
                lastHP = LocalPlayer.CurrentHp;
                lastMaxHP = LocalPlayer.MaxHp;
            }
            if (ActivePreset.Die.IsEnabled() && LocalPlayer.CurrentHp == 0 && !wasDead)
            {
                Plugin.ClientPishock.request(ActivePreset.Die, Plugin.LocalPlayer);
                wasDead = false;
            }

            if (lastHP > LocalPlayer.CurrentHp && ActivePreset.TakeDamage.IsEnabled())
            {
                int amount = (int)lastHP - (int)LocalPlayer.CurrentHp;
                int amountPercent = (int)((double)amount / lastMaxHP * 100);
                if (ActivePreset.TakeDamage.CustomData == null) ActivePreset.TakeDamage.setupCustomData(); //failsafe
                if (ActivePreset.TakeDamage.CustomData["Proportional"][0] == 1)
                {
                    int calcdIntensity = (int)(ActivePreset.TakeDamage.Intensity * ((double)amountPercent / ActivePreset.TakeDamage.CustomData["Proportional"][1]));
                    int calcdDuration = (int)(ActivePreset.TakeDamage.Duration * ((double)amountPercent / ActivePreset.TakeDamage.CustomData["Proportional"][1]));
                    Plugin.ClientPishock.request(ActivePreset.TakeDamage, Plugin.LocalPlayer, [(int)ActivePreset.TakeDamage.OpMode, calcdIntensity, calcdDuration]);
                }
                else Plugin.ClientPishock.request(ActivePreset.TakeDamage, Plugin.LocalPlayer);
            }
            if (lastHP > 0) wasDead = false;
        }
        private void HandleMPChange()
        {
            // Currently Unused
        }
        private void HandleStatusCheck()
        {
            lastStatusCheck = 0;
            bool foundVuln = false;
            bool foundDDown = false;
            if (LocalPlayer.StatusList != null && LocalPlayer.StatusList.Length > 0)
            {
                foreach (var status in LocalPlayer.StatusList)
                {
                    //Yes. We have to check for the IconId. The StatusId is different for different expansions, while the Name is different through languages.
                    if (status.GameData.Value.Icon >= 17101 && status.GameData.Value.Icon <= 17116) // Vuln Up
                    {
                        foundVuln = true;
                        var amount = status.StackCount;

                        Plugin.Log("Found Vuln Up - Amount: " + amount + " lastVulnCount: " + lastVulnAmount);
                        if (amount > lastVulnAmount)
                        {
                            if (ActivePreset.FailMechanic.CustomData == null) ActivePreset.FailMechanic.setupCustomData(); //failsafe
                            if (ActivePreset.FailMechanic.CustomData["Proportional"][0] == 1)
                            {
                                int calcdIntensity = ActivePreset.FailMechanic.Intensity * (amount / ActivePreset.FailMechanic.CustomData["Proportional"][1]);
                                int calcdDuration = ActivePreset.FailMechanic.Duration * (amount / ActivePreset.FailMechanic.CustomData["Proportional"][1]);
                                Plugin.ClientPishock.request(ActivePreset.FailMechanic, Plugin.LocalPlayer, [(int)ActivePreset.FailMechanic.OpMode, calcdIntensity, calcdDuration]);
                            }
                            else Plugin.ClientPishock.request(ActivePreset.FailMechanic, Plugin.LocalPlayer);
                        }
                        lastVulnAmount = amount;
                    }

                    if (status.GameData.Value.Icon >= 18441 && status.GameData.Value.Icon <= 18456) // Damage Down
                    {
                        foundDDown = true;
                        var amount = status.StackCount;
                        if (amount > lastDDownAmount)
                        {
                            if (ActivePreset.FailMechanic.CustomData == null) ActivePreset.FailMechanic.setupCustomData(); //failsafe
                            if (ActivePreset.FailMechanic.CustomData["Proportional"][0] == 1)
                            {
                                int calcdIntensity = ActivePreset.FailMechanic.Intensity * (amount / ActivePreset.FailMechanic.CustomData["Proportional"][1]);
                                int calcdDuration = ActivePreset.FailMechanic.Duration * (amount / ActivePreset.FailMechanic.CustomData["Proportional"][1]);
                                Plugin.ClientPishock.request(ActivePreset.FailMechanic, Plugin.LocalPlayer, [(int)ActivePreset.FailMechanic.OpMode, calcdIntensity, calcdDuration]);
                            }
                            else Plugin.ClientPishock.request(ActivePreset.FailMechanic, Plugin.LocalPlayer);
                        }
                        lastDDownAmount = amount;
                    }
                }
            }
            if (!foundVuln) lastVulnAmount = 0;
            if (!foundDDown) lastDDownAmount = 0;
        }
        private void HandlePartyMembers()
        {
            if (lastCheckedIndex >= Plugin.PartyList.Length) lastCheckedIndex = 0;
            if (Plugin.PartyList[lastCheckedIndex].ObjectId > 0 && Plugin.PartyList[lastCheckedIndex].CurrentHP == 0 && !deadIndexes[lastCheckedIndex])
            {
                deadIndexes[lastCheckedIndex] = true;
                amountDead++;
                Player source = new Player(Plugin.PartyList[lastCheckedIndex].Name.ToString(), (int)Plugin.PartyList[lastCheckedIndex].World.Value.RowId);
                Plugin.Log($"(Deathmode) - Player {source.getFullName()} died - {amountDead}/{Plugin.PartyList.Length} members are dead.");

                Plugin.ClientPishock.request(ActivePreset.PartymemberDies, source, [(int)ActivePreset.PartymemberDies.OpMode, ActivePreset.PartymemberDies.Intensity * (amountDead / Plugin.PartyList.Length), ActivePreset.PartymemberDies.Duration * (amountDead / Plugin.PartyList.Length)]);
            }
            else if (Plugin.PartyList[lastCheckedIndex].ObjectId > 0 && Plugin.PartyList[lastCheckedIndex].CurrentHP > 0 && deadIndexes[lastCheckedIndex])
            {
                deadIndexes[lastCheckedIndex] = false;
                amountDead--;
                Plugin.Log($"(Deathmode) - Player revived - {amountDead}/{Plugin.PartyList.Length} members are dead.");
            }
            lastCheckedIndex++;
            lastPartyCheck = 0;
        }

        */

        public unsafe void HandleChatMessage(XivChatType type, int timespamp, ref SeString senderE, ref SeString message, ref bool isHandled)
        {
            /*
            if (Plugin.ClientState.LocalPlayer == null)
            {
                Plugin.Error("Localplayer is null while we received a message - Ignoring...", true);
                return;
            }
            if (message == null) return; //sanity check in case we get sent bad data

            string sender = StringSanitizer.LetterOrDigit(senderE.ToString()).ToLower();

            if ((int)type <= 107 && sender.Contains(Plugin.ClientState.LocalPlayer.Name.ToString().ToLower())) // its proooobably a social message
            {

                if (ActivePreset.SayBadWord.IsEnabled())
                {
                    foreach (var (word, settings) in ActivePreset.SayBadWord.CustomData)
                    {

                        if (message.ToString().ToLower().Contains(word.ToLower()))
                        {
                            Plugin.ClientPishock.request(ActivePreset.SayBadWord, Plugin.LocalPlayer, $"You said the bad word: {word}!", settings);
                        }
                    }
                }

                if (ActivePreset.DontSayWord.IsEnabled())
                {
                    bool found = false;
                    foreach (var (word, settings) in ActivePreset.DontSayWord.CustomData)
                    {
                        if (message.ToString().Contains(word))
                        {
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        Plugin.ClientPishock.request(ActivePreset.DontSayWord, Plugin.LocalPlayer);
                    }
                }

                //slightly different logic
                if (ActivePreset.SayFirstPerson.IsEnabled())
                {
                    string sanMessage = StringSanitizer.LetterOrDigit(message.ToString()).ToLower();
                    foreach (var word in sanMessage.Split(' '))
                    {
                        if (FirstPersonWords.Contains(word))
                        {
                            Plugin.ClientPishock.request(ActivePreset.SayFirstPerson, Plugin.LocalPlayer);
                        }
                    }
                }
            }


            if ((int)type == 2122 && ActivePreset.LoseDeathRoll.IsEnabled() && message.Payloads.Find(pay => pay.Type == PayloadType.Icon) != null) // Deathroll
            {
                string[] parts = message.ToString().Split(" ");
                if (parts[1].Length < 6) // check if the name is "You" or similiar, as different languages exist
                {
                    foreach (string part in parts)
                    {
                        if (char.IsDigit(part[0])) // this is a number
                        {
                            if (part.Length == 1 && part == "1")
                            {
                                Plugin.ClientPishock.request(ActivePreset.LoseDeathRoll, Plugin.LocalPlayer);
                            }
                        }
                    }
                }
            }



            ChatTypes? chatType = GetChatTypeFromXivChatType(type);
            if (chatType == null)
            {
                return;
            }
            if (ActivePreset.Channels.Contains(chatType.Value)) //If the channel can be selected and is activated by the user
            {
                List<RegexTrigger> triggers = ActivePreset.SayCustomMessage;
                foreach (RegexTrigger trigger in triggers)
                {
                    //Plugin.Log(message.TextValue,true);
                    if (trigger.IsEnabled() && trigger.Regex != null && trigger.Regex.IsMatch(message.TextValue))
                    {
                        Plugin.ClientPishock.request(trigger, Plugin.LocalPlayer, "[CT] " + trigger.Name + " triggered!");
                    }
                }
            }
        
            */
        }


        private void HandlePlayerTerritoryChange(ushort obj)
        {
            // Currently Unused
        }

        private void TargetSomething()
        {

        }

        private void OnEmoteIncoming(IPlayerCharacter sourceObj, ushort emoteId)
        {
            //Plugin.PluginLog.Info("[INCOMING EMOTE] Source: " + sourceObj.ToString() + " EmoteId: " + emoteId);
            /*
            if (ActivePreset.GetPat.IsEnabled() && emoteId == 105)
            {
                Player source = new Player(sourceObj.Name.ToString(), (int)sourceObj.HomeWorld.Value.RowId);
                Plugin.ClientPishock.request(ActivePreset.GetPat, source, $"You got pat'd by {sourceObj.Name}!");
            }
            if (ActivePreset.GetSnapped.IsEnabled() && emoteId == 205)
            {
                Player source = new Player(sourceObj.Name.ToString(), (int)sourceObj.HomeWorld.Value.RowId);
                Plugin.ClientPishock.request(ActivePreset.GetSnapped, source, $"You got snapped at by {sourceObj.Name}!");
            }
            */
        }

        private void OnEmoteUnrelated(IPlayerCharacter sourceObj, IGameObject targetObj, ushort emoteId)
        {
            //Plugin.PluginLog.Info("[Unrelated Emote] Source: " + sourceObj.ToString() + " Target:" + targetObj + " EmoteId: " + emoteId);
            // Currently Unused
        }

        private void OnEmoteOutgoing(IGameObject targetObj, ushort emoteId)
        {
            //Plugin.PluginLog.Info("[OUTGOING EMOTE] Target: " + targetObj.ToString() + " EmoteId: " + emoteId);

        }

        private void OnEmoteSelf(ushort emoteId)
        {
            //Plugin.PluginLog.Info("[SELF EMOTE] EmoteId: " + emoteId);
            // Currently Unused.
        }

        private void OnSitEmote(ushort emoteId)
        {
            // 50 = /sit
            // 51 = getup from /sit
            // 52 = /groundsit
            /*
            if (ActivePreset.SitOnFurniture.IsEnabled())
            {
                if (emoteId == 50) // /sit on Chair done
                {
                    sittingOnChair = true;
                    sittingOnChairPos = Plugin.ClientState.LocalPlayer.Position;
                    Plugin.ClientPishock.request(ActivePreset.SitOnFurniture, Plugin.LocalPlayer);
                    int calc = 5000;
                    if (ActivePreset.SitOnFurniture.Duration <= 10) calc += ActivePreset.SitOnFurniture.Duration * 1000;
                    sittingOnChairTimer.Interval = calc;
                    sittingOnChairTimer.Start();
                }

                if (emoteId == 52) // /sit with no furniture or /groundsit on furniture - check nearby chairs
                {
                    // Todo - Implement
                }

                if (emoteId == 51)
                {
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                }
            }
            */
        }

        /*
        private void checkSittingOnChair(object? sender, ElapsedEventArgs? e)
        {
            if (sittingOnChair && Plugin.ClientState.LocalPlayer.Position.Equals(sittingOnChairPos))
            {
                Plugin.ClientPishock.request(ActivePreset.SitOnFurniture, Plugin.LocalPlayer, $"You are still sitting on Furniture!");
                sittingOnChairTimer.Refresh();
            }
            else
            {
                sittingOnChair = false;
                sittingOnChairTimer.Stop();
            }
        }
        */

        /* Unused Debug stuff
        private void HandleNetworkMessage(nint dataPtr, ushort OpCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            Plugin.PluginLog.Info($"(Net) dataPtr: {dataPtr} - OpCode: {OpCode} - ActorId: {sourceActorId} - TargetId: {targetActorId} - direction: ${direction.ToString()}");

            //if (MasterCharacter != null && MasterCharacter.IsValid() && MasterCharacter.Name + "#" + MasterCharacter.HomeWorld.Id == Plugin.Configuration.MasterNameFull) return;

            var targetOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.GameObjectId == targetActorId);
            if (targetOb != null && targetOb.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                if (((IPlayerCharacter)targetOb).Name + "#" + ((IPlayerCharacter)targetOb).HomeWorld.Id == Plugin.Configuration.MasterNameFull)
                {
                    MasterCharacter = (IPlayerCharacter)targetOb;
                    Plugin.PluginLog.Info("Found Master Signature!");
                    Plugin.PluginLog.Info(MasterCharacter.ToString());
                    Plugin.GameNetwork.NetworkMessage -= HandleNetworkMessage;
                    return;
                }
                //Plugin.PluginLog.Info(targetOb.ToString());
            }
        }*/

    }
}
