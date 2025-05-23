﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Linq;

namespace WoLightning.Util.Types
{
    enum Worlds
    {
        // Aether Data Center
        Jenova = 40,
        Faerie = 54,
        Siren = 57,
        Gilgamesh = 63,
        Midgardsormr = 65,
        Adamantoise = 73,
        Cactuar = 79,
        Sargatanas = 99,

        // Crystal Data Center
        Brynhildr = 34,
        Mateus = 37,
        Zalera = 41,
        Diabolos = 63,
        Coeurl = 74,
        Malboro = 75,
        Goblin = 81,
        Balmung = 91,

        //Primal Data Center
        Famfrit = 35,
        Exodus = 53,
        Lamia = 55,
        Leviathan = 64,
        Ultros = 77,
        Behemoth = 78,
        Excalibur = 93,
        Hyperion = 95,

        // Dynamis Data Center
        Halicarnassus = 406,
        Maduin = 407,
        Marilith = 404,
        Seraph = 405,
        Cuchalainn = 900, // UNKNOWN
        Golem = 901, // UNKNOWN
        Kraken = 902, // UNKNOWN
        Rafflesia = 903, // UNKNOWN

        // Chaos Data Center
        Omega = 39,
        Moogle = 71,
        Cerberus = 80,
        Louisoix = 83,
        Spriggan = 85,
        Ragnarok = 97,
        Sagittarius = 400,
        Phantom = 401,

        // Light Data Center
        Twintania = 33,
        Lich = 36,
        Zodiark = 42,
        Phoenix = 56,
        Odin = 66,
        Shiva = 67,
        Alpha = 402,
        Raiden = 403,

        // Shadow Data Center
        Innocence = 904, // UNKNOWN
        Pixie = 905, // UNKNOWN
        Titania = 906, // UNKNOWN
        Tycoon = 907, // UNKNOWN

        // Elemental Data Center
        Carbuncle = 45,
        Kujata = 49,
        Typhon = 50,
        Garuda = 58,
        Atomos = 68,
        Tonberry = 72,
        Aegis = 90,
        Gungnir = 94,

        // Gaia Data Center
        Alexander = 43,
        Fenrir = 46,
        Ultima = 51,
        Ifrit = 59,
        Bahamut = 69,
        Tiamat = 76,
        Durandal = 92,
        Ridill = 98,

        // Mana Data Center
        Asura = 23,
        Pandaemonium = 28,
        Anima = 44,
        Hades = 47,
        Ixion = 48,
        Titan = 61,
        Chocobo = 70,
        Masamune = 96,

        // Meteor Data Center
        Bellas = 24,
        Shinryu = 29,
        Unicorn = 30,
        Yojimbo = 31,
        Zeromus = 32,
        Valefor = 53,
        Ramuh = 60,
        Mandragora = 82,

        // Materia Data Center
        Ravana = 21,
        Bismarck = 22,
        Sephirot = 86,
        Sophia = 87,
        Zurvan = 88,
    }

    [Serializable]
    public class Player
    {
        public string? Name { get; set; }
        public int? WorldId { get; set; }
        public string? Key { get; set; }
        public bool? Online { get; set; }
        public bool? PluginActive { get; set; }


        public Player()
        {
        }

        public Player(Payload playerPayload)
        {
            if (playerPayload.Type != PayloadType.Player)
            {
                throw new ArgumentException("Tried to create a Player with Payloadtype " + playerPayload.Type);
            }

            string playerString = playerPayload.ToString()!;
            int x = 0;
            foreach (var part in playerString.Split(","))
            {
                string[] values = part.Split(":");
                if (values.Length != 2) continue;

                if (x == 0) Name = values[1].Trim();
                if (x == 1) WorldId = int.Parse(values[1].Trim());
                x++;
            }
            if (Name == null || WorldId == null)
            {
                throw new Exception("Unable to create Player from Payload!\n" + playerString);
            }
        }

        public static Player? TryCreateFromSeString(SeString senderE)
        {
            foreach (var payload in senderE.Payloads)
            {
                if (payload.Type == PayloadType.Player) return new Player(payload);
            }
            return null;
        }

        public Player(string fullString)
        {
            string[] parts = fullString.Split("@");
            Name = parts[0];
            foreach (Worlds world in Enum.GetValues(typeof(Worlds)).Cast<Worlds>())
            {
                if (world.ToString().Equals(parts[1]))
                {
                    WorldId = (int)world;
                    break;
                }
            }
        }

        public Player(string name, int worldId)
        {
            Name = name;
            WorldId = worldId;
        }

        public Player(string name, int worldId, string key, bool pluginActive)
        {
            Name = name;
            WorldId = worldId;
            Key = key;
            PluginActive = pluginActive;
        }

        //Player source = new Player(Plugin.PartyList[lastCheckedIndex].Name.ToString(), (int)Plugin.PartyList[lastCheckedIndex].World.Value.RowId);

        public Player(IPlayerCharacter playerCharacter)
        {
            Name = playerCharacter.Name.ToString();
            WorldId = (int)playerCharacter.HomeWorld.Value.RowId;
        }

        public string getWorldName()
        {
            if (WorldId == null) return "UNKNOWN";
            string? output = Enum.GetName(typeof(Worlds), WorldId);
            if (output == null) return "UNKNOWN";
            return output;
        }

        public string getFullName()
        {
            return Name + "@" + getWorldName();
        }

        public bool validate()
        {
            return Name != null && Name.Length > 3 && Name.Length < 25 && getWorldName() != "UNKNOWN";
        }
        public override string ToString()
        {
            if (Key == null || Key.Length < 7) return $"[Player] {Name} @{getWorldName()}\nKey: [Unknown]";
            return $"[Player] {Name} @{getWorldName()}\nKey: {Key.Substring(0, 7)}(...)";
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != GetType()) return false;
            return getFullName() == ((Player)obj).getFullName();
        }

    }
}
