﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.Xml.Linq;
using GTA;
using GTA.Native;
using GunshotWound.TestPeds;
using GunshotWound.WoundedPeds;

/*
     GunShot Wound. GTA V mod.
     Copyright (C) 2018  Farley SH42913 Drunk

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses/.
*/

namespace GunshotWound
{
    /// <summary>
    /// Bleeding states
    /// </summary>
    public enum BleedingStates
    {
        NONE,
        LIGHT,
        MEDIUM,
        HEAVY,
        DEADLY
    }

    /// <summary>
    /// Wound states
    /// </summary>
    public enum WoundStates
    {
        NONE,
        LIGHT,
        MEDIUM,
        HEAVY,
        DEADLY
    }

    /// <summary>
    /// Weapon classes
    /// </summary>
    public enum WeaponClasses
    {
        SMALL_CALIBER,
        MEDIUM_CALIBER,
        HIGH_CALIBER,
        SHOTGUN,
        CUTTING,
        LIGHT_IMPACT,
        HEAVY_IMPACT,
        EXPLOSIVE,
        FIRE,
        SUFFOCATING,
        OTHER,
        NOTHING
    }

    /// <summary>
    /// Body parts that can damage
    /// </summary>
    public enum BodyParts
    {
        HEAD,
        NECK,
        UPPER_BODY,
        LOWER_BODY,
        ARM,
        LEG,
        NOTHING
    }

    /// <summary>
    /// Gunshot Wound mod by SH42913
    /// Inspired of Health Realism Mod by SOB
    /// </summary>
    public class GunshotWound : Script
    {
        //MADE BY SH42913
        //BASED ON Health Realism Mod by SOB

        /// <summary>
        /// Debug mode
        /// </summary>
        internal bool DebugMode { get; } = false;



        /// <summary>
        /// Maximal Armor that any peds can carry
        /// </summary>
        public int MaxArmor { get; private set; }

        /// <summary>
        /// Maximal Health for any peds
        /// </summary>
        public int MaxHealth { get; private set; }


        /// <summary>
        /// Player is influenced by mod
        /// </summary>
        public bool WoundedPlayerEnabled { get; private set; }

        /// <summary>
        /// Enemy critical damage notifications enabled
        /// </summary>
        public bool EnemyNotificationsEnabled { get; private set; }

        /// <summary>
        /// Time to auto-update notifications in seconds
        /// </summary>
        public float TimeToRefreshNotifications { get; private set; }

        /// <summary>
        /// Subtitles status enabled
        /// </summary>
        public bool SubtitlesStatusEnabled { get; private set; }

        /// <summary>
        /// Slow motion on deadly wounds
        /// </summary>
        public bool SlowMoOnDeadlyWound { get; private set; }

        /// <summary>
        /// Ingame self healing amount
        /// </summary>
        public float SelfHealingAmount { get; private set; }

        /// <summary>
        /// Player weapons damage modifier
        /// </summary>
        private float DamageModifier { get; set; }

        /// <summary>
        /// Light damage for armor hit
        /// </summary>
        public int ArmorDamage { get; private set; }



        /// <summary>
        /// Key for instant heal
        /// </summary>
        private Keys HealButton { get; set; }

        /// <summary>
        /// Key for Subtitles Status on/off
        /// </summary>
        private Keys SubtitlesButton { get; set; }

        /// <summary>
        /// Key for show damage notifications
        /// </summary>
        private Keys NotificationsButton { get; set; }

        /// <summary>
        /// Key for getting helmet
        /// </summary>
        private Keys HelmetButton { get; set; }



        /// <summary>
        /// Time for bleeding damage in seconds
        /// </summary>
        public float TimeToBleed { get; private set; }

        /// <summary>
        /// Damage for light bleeding
        /// </summary>
        public int LightBleedingDamage { get; private set; }

        /// <summary>
        /// Damage for medium bleeding
        /// </summary>
        public int MediumBleedingDamage { get; private set; }

        /// <summary>
        /// Damage for heavy bleeding
        /// </summary>
        public int HeavyBleedingDamage { get; private set; }

        /// <summary>
        /// Damage for deadly bleeding
        /// </summary>
        public int DeadlyBleedingDamage { get; private set; }

        /// <summary>
        /// Time to heal light bleeding
        /// </summary>
        public float TimeToHealBleeding { get; private set; }

        /// <summary>
        /// How many times you can get bleeding before bleeding state will increase
        /// </summary>
        public int MaxBleedingLevel { get; private set; }



        /// <summary>
        /// Additional damage when ped get light wound
        /// </summary>
        public int AdditionalDamageOnLightWounds { get; private set; }

        /// <summary>
        /// Additional damage when ped get medium wound
        /// </summary>
        public int AdditionalDamageOnMediumWounds { get; private set; }

        /// <summary>
        /// Additional damage when ped get heavy wound
        /// </summary>
        public int AdditionalDamageOnHeavyWounds { get; private set; }

        /// <summary>
        /// Additional damage when ped get deadly wound
        /// </summary>
        public int AdditionalDamageOnDeadlyWounds { get; private set; }

        /// <summary>
        /// Additional damage when ped get nerves damage
        /// </summary>
        public int AdditionalDamageOnNervesDamage { get; private set; }

        /// <summary>
        /// Additional damage when ped get headshot
        /// </summary>
        public int AdditionalHeadshotDamage { get; private set; }

        /// <summary>
        /// Animation name used for normal state
        /// </summary>
        public string AnimationOnNoneWounds { get; private set; }

        /// <summary>
        /// Animation name used when ped is light wounded
        /// </summary>
        public string AnimationOnLightWounds { get; private set; }

        /// <summary>
        /// Animation name used when ped is medium wounded
        /// </summary>
        public string AnimationOnMediumWounds { get; private set; }

        /// <summary>
        /// Animation name used when ped is heavy wounded
        /// </summary>
        public string AnimationOnHeavyWounds { get; private set; }

        /// <summary>
        /// Animation name used when ped is deadly wounded
        /// </summary>
        public string AnimationOnDeadlyWounds { get; private set; }

        /// <summary>
        /// Animation name used when ped has nerves damage
        /// </summary>
        public string AnimationOnNervesDamage { get; private set; }

        /// <summary>
        /// Animation rate when ped is light wounded. For normal state - 1.0
        /// </summary>
        public float MoveRateOnLightWounds { get; private set; }

        /// <summary>
        /// Animation rate when ped is medium wounded. For normal state - 1.0
        /// </summary>
        public float MoveRateOnMediumWounds { get; private set; }

        /// <summary>
        /// Animation rate when ped is heavy wounded. For normal state - 1.0
        /// </summary>
        public float MoveRateOnHeavyWounds { get; private set; }

        /// <summary>
        /// Animation rate when ped is deadly wounded. For normal state - 1.0
        /// </summary>
        public float MoveRateOnDeadlyWounds { get; private set; }

        /// <summary>
        /// Animation rate when ped has nerves damage. For normal state - 1.0
        /// </summary>
        public float MoveRateOnNervesDamage { get; private set; }

        /// <summary>
        /// Time to heal light wounds
        /// </summary>
        public float TimeToHealWounds { get; private set; }

        /// <summary>
        /// How many times you can get wound before wound state will increase
        /// </summary>
        public int MaxWoundLevel { get; private set; }

        /// <summary>
        /// Time to death on deadly wound in seconds
        /// </summary>
        public float TimeToDeath { get; private set; }



        /// <summary>
        /// Other peds influenced by script enabled
        /// </summary>
        public bool WoundedPedsEnabled { get; private set; }

        /// <summary>
        /// Radius for searching peds
        /// </summary>
        public float SearchingRadius { get; private set; }



        public static List<uint> SmallCaliberWeaponHashes = new List<uint>(new uint[]
        {
            (uint) WeaponHash.Pistol, 
            (uint) WeaponHash.CombatPistol, 
            (uint) WeaponHash.APPistol,
            (uint) WeaponHash.CombatPDW,
            (uint) WeaponHash.MachinePistol, 
            (uint) WeaponHash.MicroSMG, 
            (uint) WeaponHash.MiniSMG,
            (uint) WeaponHash.PistolMk2,
            (uint) WeaponHash.SNSPistol, 
            (uint) WeaponHash.SNSPistolMk2, 
            (uint) WeaponHash.VintagePistol,
        });

        public static List<uint> MediumCaliberWeaponHashes = new List<uint>(new uint[]
        {
            (uint) WeaponHash.AdvancedRifle, 
            (uint) WeaponHash.AssaultSMG,
            (uint) WeaponHash.BullpupRifle, 
            (uint) WeaponHash.BullpupRifleMk2, 
            (uint) WeaponHash.CarbineRifle,
            (uint) WeaponHash.CarbineRifleMk2,
            (uint) WeaponHash.CompactRifle, 
            (uint) WeaponHash.DoubleActionRevolver, 
            (uint) WeaponHash.Gusenberg,
            (uint) WeaponHash.HeavyPistol,
            (uint) WeaponHash.MarksmanPistol, 
            (uint) WeaponHash.Pistol50, 
            (uint) WeaponHash.Revolver,
            (uint) WeaponHash.RevolverMk2,
            (uint) WeaponHash.SMG, 
            (uint) WeaponHash.SMGMk2, 
            (uint) WeaponHash.SpecialCarbine,
            (uint) WeaponHash.SpecialCarbineMk2,
        });

        public static List<uint> HighCaliberWeaponHashes = new List<uint>(new uint[]
        {
            (uint) WeaponHash.AssaultRifle, 
            (uint) WeaponHash.AssaultrifleMk2, 
            (uint) WeaponHash.CombatMG,
            (uint) WeaponHash.CombatMGMk2, 
            (uint) WeaponHash.HeavySniper, 
            (uint) WeaponHash.HeavySniperMk2,
            (uint) WeaponHash.MarksmanRifle, 
            (uint) WeaponHash.MarksmanRifleMk2, 
            (uint) WeaponHash.MG,
            (uint) WeaponHash.Minigun,
            (uint) WeaponHash.Musket, 
            (uint) WeaponHash.Railgun,
        });

        public static List<uint> ShotgunsWeaponHashes = new List<uint>(new uint[]
        {
            (uint) WeaponHash.AssaultShotgun, 
            (uint) WeaponHash.BullpupShotgun, 
            (uint) WeaponHash.DoubleBarrelShotgun,
            (uint) WeaponHash.HeavyShotgun, 
            (uint) WeaponHash.PumpShotgun,
            (uint) WeaponHash.PumpShotgunMk2,
            (uint) WeaponHash.SawnOffShotgun,
            (uint) WeaponHash.SweeperShotgun,
        });

        public static List<uint> CuttingWeaponHashes = new List<uint>(new uint[]
        {
            //Animal    Cougar     BarbedWire
            4194021054, 148160082, 1223143800, 
            (uint) WeaponHash.BattleAxe, 
            (uint) WeaponHash.Bottle,
            (uint) WeaponHash.Dagger,
            (uint) WeaponHash.Hatchet, 
            (uint) WeaponHash.Knife, 
            (uint) WeaponHash.Machete,
            (uint) WeaponHash.SwitchBlade,
        });
        
        public static List<uint> LightImpactWeaponHashes = new List<uint>(new uint[]
        {
            //GarbageBug Briefcase  Briefcase2
            3794977420, 2294779575, 28811031, 
            (uint) WeaponHash.Ball, 
            (uint) WeaponHash.Flashlight, 
            (uint) WeaponHash.KnuckleDuster,
            (uint) WeaponHash.Nightstick, 
            (uint) WeaponHash.Snowball,
            (uint) WeaponHash.Unarmed, 
            (uint) WeaponHash.Parachute,
            (uint) WeaponHash.NightVision,
        });

        public static List<uint> HeavyImpactWeaponHashes = new List<uint>(new uint[]
        {
            (uint) WeaponHash.Bat, 
            (uint) WeaponHash.Crowbar,
            (uint) WeaponHash.FireExtinguisher, 
            (uint) WeaponHash.Firework,
            (uint) WeaponHash.GolfClub, 
            (uint) WeaponHash.Hammer, 
            (uint) WeaponHash.PetrolCan, 
            (uint) WeaponHash.PoolCue, 
            (uint) WeaponHash.Wrench,
        });

        public static List<uint> ExplosiveWeaponHashes = new List<uint>(new uint[]
        {
            //Explosion
            539292904, 
            (uint) WeaponHash.Grenade, 
            (uint) WeaponHash.CompactGrenadeLauncher,
            (uint) WeaponHash.GrenadeLauncher, 
            (uint) WeaponHash.HomingLauncher, 
            (uint) WeaponHash.PipeBomb,
            (uint) WeaponHash.ProximityMine, 
            (uint) WeaponHash.RPG, 
            (uint) WeaponHash.StickyBomb,
        });

        public static List<uint> OtherWeaponHashes = new List<uint>(new uint[]
        {
            //Fall      WaterCannon Rammed     RunOverCar  HeliCrash
            3452007600, 3425972830, 133987706, 2741846334, 341774354,
        });

        public static List<uint> FireWeaponHashes = new List<uint>(new uint[]
        {
            //ElectricFence Fire
            2461879995, 3750660587, 
            (uint) WeaponHash.StunGun, 
            (uint) WeaponHash.Molotov, 
            (uint) WeaponHash.Flare,
            (uint) WeaponHash.FlareGun,
        });

        public static List<uint> SuffocatingWeaponHashes = new List<uint>(new uint[]
        {
            //Drowning  DrowningVeh Exhaust
            4284007675, 1936677264, 910830060, 
            (uint) WeaponHash.BZGas, 
            (uint) WeaponHash.SmokeGrenade,
            (uint) WeaponHash.GrenadeLauncherSmoke,
        });

        internal bool FoolsDay { get; } = DateTime.Now.Month == 4 && DateTime.Now.Day == 1;
        internal int ticks;

        /// <summary>
        /// List with IEnchancedPedFactory
        /// </summary>
        internal List<IEnchancedPedFactory> Factories { get; } = new List<IEnchancedPedFactory>();

        /// <summary>
        /// List of IEnchancedPed for player
        /// </summary>
        internal List<IEnhancedPed> PlayerList { get; } = new List<IEnhancedPed>();

        /// <summary>
        /// Dictionary where Key - Peds and Value is List of IEnhancedPed
        /// </summary>
        internal Dictionary<Ped, List<IEnhancedPed>> PedDictionary { get; } = new Dictionary<Ped, List<IEnhancedPed>>();

        /// <summary>
        /// Debugneedstring
        /// </summary>
        private string _lastLoadedString;

        public GunshotWound()
        {
            try
            {
                LoadConfig();
                
                _lastLoadedString = "Failed factory load";
                LoadFactories();

                Tick += OnTick;
                KeyUp += OnKey;

                if (WoundedPlayerEnabled)
                {
                    _lastLoadedString = "Failed player init";
                    var config = new PedConfig
                    {
                        IsPlayer = true,
                        TargetPed = Game.Player.Character,
                        ManagerScript = this
                    };
                    
                    foreach (IEnchancedPedFactory factory in Factories)
                    {
                        PlayerList.Add(factory.Build(config));
                    }
                }

                ticks = 0;
            }
            catch (Exception exception)
            {
                XDocument doc = new XDocument();

                doc.Add(new XElement("Error",
                    exception, new XAttribute("ErrorXmlString", _lastLoadedString)));

                doc.Save("scripts/gswerror.xml");
                UI.Notify("~r~GSW got error! Check Scripts folder and send to author file gswerror.xml. Thank you.");
            }
        }

        /// <summary>
        /// Here you can add your factories
        /// </summary>
        private void LoadFactories()
        {
            Factories.Add(new WoundedPedFactory());
            //Factories.Add(new TestPedFactory());
        }

        /// <summary>
        /// Here you can update config-load
        /// </summary>
        private void LoadConfig()
        {
            XDocument doc = XDocument.Load("scripts/GunshotWoundConfig.xml");
            XElement category = doc.Root.Element("General");

            _lastLoadedString = "WoundedPlayerEnabled";
            WoundedPlayerEnabled = 
                bool.Parse(category.Element("WoundedPlayerEnabled").Value);
            _lastLoadedString = "DamageModifier";
            DamageModifier = 
                float.Parse(category.Element("DamageModifier").Value, CultureInfo.InvariantCulture);

            _lastLoadedString = "MaximalArmor";
            MaxArmor = 
                int.Parse(category.Element("MaximalArmor").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "ArmorDamage";
            ArmorDamage = 
                int.Parse(category.Element("ArmorDamage").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "MaximalHealth";
            MaxHealth = 
                int.Parse(category.Element("MaximalHealth").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "SelfHealingAmount";
            SelfHealingAmount = 
                float.Parse(category.Element("SelfHealingAmount").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "RefreshNotificatitonsTime";
            TimeToRefreshNotifications = 
                float.Parse(category.Element("RefreshNotificationsTime").Value, CultureInfo.InvariantCulture);

            _lastLoadedString = "OtherPedsNotifications";
            EnemyNotificationsEnabled = bool.Parse(category.Element("OtherPedsNotifications").Value);
            _lastLoadedString = "SubtitlesStatus";
            SubtitlesStatusEnabled = bool.Parse(category.Element("SubtitlesStatus").Value);
            _lastLoadedString = "DeadlyWoundSlowMotion";
            SlowMoOnDeadlyWound = bool.Parse(category.Element("DeadlyWoundSlowMotion").Value);



            category = doc.Root.Element("WoundedPeds");
            _lastLoadedString = "WoundedPedsEnabled";
            WoundedPedsEnabled = 
                bool.Parse(category.Element("WoundedPedsEnabled").Value);
            _lastLoadedString = "WorkingRadius";
            SearchingRadius = 
                float.Parse(category.Element("WorkingRadius").Value, CultureInfo.InvariantCulture);



            category = doc.Root.Element("BleedingInjuries");
            _lastLoadedString = "TimeToBleed";
            TimeToBleed = 
                float.Parse(category.Element("TimeToBleed").Value, CultureInfo.InvariantCulture);

            _lastLoadedString = "LightBleedingDamage";
            LightBleedingDamage = 
                int.Parse(category.Element("LightBleedingDamage").Value);
            _lastLoadedString = "MediumBleedingDamage";
            MediumBleedingDamage = 
                int.Parse(category.Element("MediumBleedingDamage").Value);
            _lastLoadedString = "HeavyBleedingDamage";
            HeavyBleedingDamage = 
                int.Parse(category.Element("HeavyBleedingDamage").Value);
            _lastLoadedString = "DeadlyBleedingDamage";
            DeadlyBleedingDamage = 
                int.Parse(category.Element("DeadlyBleedingDamage").Value);

            _lastLoadedString = "MaxBleedingLevel";
            MaxBleedingLevel = 
                int.Parse(category.Element("MaxBleedingLevel").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "HealBleedingTime";
            TimeToHealBleeding = 
                float.Parse(category.Element("HealBleedingTime").Value, CultureInfo.InvariantCulture);



            category = doc.Root.Element("Wounds");
            _lastLoadedString = "TimeToDeath";
            TimeToDeath = 
                float.Parse(category.Element("TimeToDeath").Value, CultureInfo.InvariantCulture);

            _lastLoadedString = "AdditionalDamageLightWound";
            AdditionalDamageOnLightWounds = 
                int.Parse(category.Element("AdditionalDamageLightWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "AdditionalDamageMediumWound";
            AdditionalDamageOnMediumWounds = 
                int.Parse(category.Element("AdditionalDamageMediumWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "AdditionalDamageHeavyWound";
            AdditionalDamageOnHeavyWounds = 
                int.Parse(category.Element("AdditionalDamageHeavyWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "AdditionalDamageDeadlyWound";
            AdditionalDamageOnDeadlyWounds = 
                int.Parse(category.Element("AdditionalDamageDeadlyWound").Value, CultureInfo.InvariantCulture);

            _lastLoadedString = "AdditionalDamageNervesDamage";
            AdditionalDamageOnNervesDamage = 
                int.Parse(category.Element("AdditionalDamageNervesDamage").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "AdditionalDamageHeadshot";
            AdditionalHeadshotDamage = 
                int.Parse(category.Element("AdditionalDamageHeadshot").Value, CultureInfo.InvariantCulture);

            _lastLoadedString = "MoveRateLightWound";
            MoveRateOnLightWounds = 
                float.Parse(category.Element("MoveRateLightWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "MoveRateMediumWound";
            MoveRateOnMediumWounds = 
                float.Parse(category.Element("MoveRateMediumWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "MoveRateHeavyWound";
            MoveRateOnHeavyWounds = 
                float.Parse(category.Element("MoveRateHeavyWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "MoveRateDeadlyWound";
            MoveRateOnDeadlyWounds = 
                float.Parse(category.Element("MoveRateDeadlyWound").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "MoveRateNervesLegsDamage";
            MoveRateOnNervesDamage = 
                float.Parse(category.Element("MoveRateNervesLegsDamage").Value, CultureInfo.InvariantCulture);

            AnimationOnNoneWounds = category.Element("AnimationNoneWound").Value;
            AnimationOnLightWounds = category.Element("AnimationLightWound").Value;
            AnimationOnMediumWounds = category.Element("AnimationMediumWound").Value;
            AnimationOnHeavyWounds = category.Element("AnimationHeavyWound").Value;
            AnimationOnDeadlyWounds = category.Element("AnimationDeadlyWound").Value;
            AnimationOnNervesDamage = category.Element("AnimationNervesLegsDamage").Value;

            _lastLoadedString = "MaxWoundLevel";
            MaxWoundLevel = 
                int.Parse(category.Element("MaxWoundLevel").Value, CultureInfo.InvariantCulture);
            _lastLoadedString = "HealWoundTime";
            TimeToHealWounds = 
                float.Parse(category.Element("HealWoundTime").Value, CultureInfo.InvariantCulture);



            category = doc.Root.Element("Hotkeys");
            _lastLoadedString = "HealKey";
            HealButton = (Keys) Enum.Parse(typeof(Keys), category.Element("HealKey").Value);

            _lastLoadedString = "SubtitlesKey";
            SubtitlesButton = (Keys) Enum.Parse(typeof(Keys), category.Element("SubtitlesKey").Value);

            _lastLoadedString = "NotificationsKey";
            NotificationsButton = (Keys) Enum.Parse(typeof(Keys), category.Element("NotificationsKey").Value);

            _lastLoadedString = "GetHelmetKey";
            HelmetButton = (Keys) Enum.Parse(typeof(Keys), category.Element("GetHelmetKey").Value);
        }

        private void OnKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == SubtitlesButton)
            {
                SubtitlesStatusEnabled = !SubtitlesStatusEnabled;

                if (!SubtitlesStatusEnabled)
                {
                    UI.ShowSubtitle("SubStatus is ~r~off");
                }
            }

            if (e.KeyCode == HelmetButton)
            {
                if (!Game.Player.Character.IsWearingHelmet)
                {
                    if (Game.Player.Money > 50)
                    {
                        int situation = new Random().Next(0, 5);

                        switch (situation)
                        {
                            case 0:
                                Game.Player.Money -= 30;
                                Game.Player.Character.GiveHelmet(false, HelmetType.RegularMotorcycleHelmet, 0);
                                break;
                            case 1:
                                Game.Player.Money -= 30;
                                Game.Player.Character.GiveHelmet(false, HelmetType.RegularMotorcycleHelmet, 1);
                                break;
                            case 2:
                                Game.Player.Money -= 30;
                                Game.Player.Character.GiveHelmet(false, HelmetType.RegularMotorcycleHelmet, 2);
                                break;
                            case 3:
                                Game.Player.Money -= 30;
                                Game.Player.Character.GiveHelmet(false, HelmetType.RegularMotorcycleHelmet, 3);
                                break;
                            case 4:
                                Game.Player.Money -= 30;
                                Game.Player.Character.GiveHelmet(false, HelmetType.RegularMotorcycleHelmet, 4);
                                break;
                        }
                    }
                }
                else
                {
                    Game.Player.Character.RemoveHelmet(false);
                }
            }

            if (e.KeyCode == NotificationsButton)
            {
                foreach (IEnhancedPed ped in PlayerList)
                {
                    ped.ShowNotifications();
                }
            }

            if (e.KeyCode == HealButton)
            {
                foreach (IEnhancedPed ped in PlayerList)
                {
                    ped.RestoreDefaultState();
                }

                UI.Notify("You are ~g~healed~s~, f*ckin cheater!\nBetter call ~r~911~s~ or find ~y~medics~s~!");
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, DamageModifier);

                if (ticks++ == 500)
                {
                    UI.Notify("You're using ~r~GunShot Wound~s~\nby ~g~SH42913~s~. Thank you!~WS~");
                    if (DebugMode)
                    {
                        UI.Notify($"Wounded Player: {WoundedPlayerEnabled}");
                        UI.Notify($"Wounded Peds: {WoundedPedsEnabled}");
                    }
                    if (FoolsDay)
                    {
                        UI.Notify("~WS~~WS~~WS~\n~italic~~f~Happy Fool's Day!\n~WS~~WS~~WS~");
                    }
                }

                if (WoundedPlayerEnabled)
                {
                    string subtitlesStatus = "";
                    foreach (IEnhancedPed ped in PlayerList)
                    {
                        ped.Update();
                        subtitlesStatus += ped.GetSubtitlesInfo();
                    }
                    Function.Call(Hash.CLEAR_PED_LAST_WEAPON_DAMAGE, Game.Player.Character);
                    
                    if(SubtitlesStatusEnabled) UI.ShowSubtitle(subtitlesStatus);
                }

                if (WoundedPedsEnabled)
                {
                    var toRemove = new List<Ped>();
                    
                    AddAllPedsInRadius(SearchingRadius);

                    foreach (Ped ped in PedDictionary.Keys)
                    {
                        if (ped != null && ped.IsAlive &&
                            DistanceToPlayer(ped) < SearchingRadius * 2)
                        {
                            foreach (IEnhancedPed enhancedPed in PedDictionary[ped])
                            {
                                enhancedPed.Update();
                            }
                            Function.Call(Hash.CLEAR_PED_LAST_WEAPON_DAMAGE, ped);
                        }
                        else
                        {
                            toRemove.Add(ped);
                        }
                    }

                    foreach (Ped pedToRemove in toRemove)
                    {
                        PedDictionary.Remove(pedToRemove);
                        if (DebugMode)
                        {
                            pedToRemove.CurrentBlip.Remove();
                            UI.Notify("I removed ped");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                XDocument doc = new XDocument();

                doc.Add(new XElement("Error",
                    exception));

                doc.Save("scripts/gswerror.xml");
                UI.Notify("~r~GSW got error! " +
                          "Check Scripts folder and send to author file gswerror.xml. " +
                          "Thank you.");
            }
        }

        private void AddAllPedsInRadius(float radius)
        {
            Ped[] allNearbyPeds = World.GetNearbyPeds(Game.Player.Character, radius);

            if (allNearbyPeds.Length <= 0) return;

            int counter = 0;
            foreach (Ped nearbyPed in allNearbyPeds)
            {
                if (!nearbyPed.IsAlive || !nearbyPed.IsHuman || PedDictionary.ContainsKey(nearbyPed)) continue;
                
                PedDictionary.Add(nearbyPed, new List<IEnhancedPed>());
                foreach (IEnchancedPedFactory factory in Factories)
                {
                    PedDictionary[nearbyPed].Add(factory.Build(new PedConfig
                    {
                        ManagerScript = this,
                        TargetPed = nearbyPed,
                        IsPlayer = false
                    }));
                }

                if (!DebugMode) continue;
                nearbyPed.AddBlip();
                counter++;
            }

            if (DebugMode && counter > 0)
            {
                UI.Notify(counter + " peds added");
            }
        }

        /// <summary>
        /// With this method you can get which body-part was damaged
        /// </summary>
        /// <param name="target">Target ped</param>
        /// <param name="debug">Debug notifications</param>
        /// <returns>Damaged body part</returns>
        public static unsafe BodyParts GetDamagedBodyPart(Ped target, bool debug = false)
        {
            int damagedBoneNum = 0;
            int* x = &damagedBoneNum;
            Function.Call(Hash.GET_PED_LAST_DAMAGE_BONE, target, x);

            if (damagedBoneNum != 0)
            {
                Enum.TryParse(damagedBoneNum.ToString(), out Bone damagedBone);
                if (debug)
                    UI.Notify($"It was {damagedBone}");

                switch (damagedBone)
                {
                    case Bone.SKEL_Head:
                        Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
                        if (debug) UI.Notify($"You got {BodyParts.HEAD}");
                        return BodyParts.HEAD;
                    case Bone.SKEL_Neck_1:
                        Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
                        if (debug) UI.Notify($"You got {BodyParts.NECK}");
                        return BodyParts.NECK;
                    case Bone.SKEL_Spine2:
                    case Bone.SKEL_Spine3:
                        Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
                        if (debug) UI.Notify($"You got {BodyParts.UPPER_BODY}");
                        return BodyParts.UPPER_BODY;
                    case Bone.SKEL_Pelvis:
                    case Bone.SKEL_Spine_Root:
                    case Bone.SKEL_Spine0:
                    case Bone.SKEL_Spine1:
                    case Bone.SKEL_ROOT:
                        Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
                        if (debug) UI.Notify($"You got {BodyParts.LOWER_BODY}");
                        return BodyParts.LOWER_BODY;
                    case Bone.SKEL_L_Thigh:
                    case Bone.SKEL_R_Thigh:
                    case Bone.SKEL_L_Toe0:
                    case Bone.SKEL_R_Toe0:
                    case Bone.SKEL_R_Foot:
                    case Bone.SKEL_L_Foot:
                    case Bone.SKEL_L_Calf:
                    case Bone.SKEL_R_Calf:
                        Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
                        if (debug) UI.Notify($"You got {BodyParts.LEG}");
                        return BodyParts.LEG;
                    case Bone.SKEL_L_Clavicle:
                    case Bone.SKEL_R_Clavicle:
                    case Bone.SKEL_L_Forearm:
                    case Bone.SKEL_R_Forearm:
                    case Bone.SKEL_L_Hand:
                    case Bone.SKEL_R_Hand:
                        Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
                        if (debug) UI.Notify($"You got {BodyParts.ARM}");
                        return BodyParts.ARM;
                }
            }

            Function.Call(Hash.CLEAR_PED_LAST_DAMAGE_BONE, target);
            return BodyParts.NOTHING;
        }

        /// <summary>
        /// With this method you can get which weapon class damage ped
        /// </summary>
        /// <param name="target">Target ped</param>
        /// <param name="debug">Debug notifications</param>
        /// <returns>Weapon class</returns>
        public static WeaponClasses GetDamagedWeaponClass(Ped target, bool debug = false)
        {
            /*if (!Function.Call<bool>(Hash.HAS_PED_BEEN_DAMAGED_BY_WEAPON, target, 0, 2))
                return WeaponClasses.NOTHING;*/

            if (PedWasDamaged(OtherWeaponHashes, target, debug))
            {
                return WeaponClasses.OTHER;
            }

            if (PedWasDamaged(SmallCaliberWeaponHashes, target, debug))
            {
                return WeaponClasses.SMALL_CALIBER;
            }

            if (PedWasDamaged(MediumCaliberWeaponHashes, target, debug))
            {
                return WeaponClasses.MEDIUM_CALIBER;
            }

            if (PedWasDamaged(HighCaliberWeaponHashes, target, debug))
            {
                return WeaponClasses.HIGH_CALIBER;
            }

            if (PedWasDamaged(ShotgunsWeaponHashes, target, debug))
            {
                return WeaponClasses.SHOTGUN;
            }

            if (PedWasDamaged(CuttingWeaponHashes, target, debug))
            {
                return WeaponClasses.CUTTING;
            }
            
            if (PedWasDamaged(LightImpactWeaponHashes, target, debug))
            {
                return WeaponClasses.LIGHT_IMPACT;
            }

            if (PedWasDamaged(HeavyImpactWeaponHashes, target, debug))
            {
                return WeaponClasses.HEAVY_IMPACT;
            }

            if (PedWasDamaged(ExplosiveWeaponHashes, target, debug))
            {
                return WeaponClasses.EXPLOSIVE;
            }

            if (PedWasDamaged(FireWeaponHashes, target, debug))
            {
                return WeaponClasses.FIRE;
            }

            if (PedWasDamaged(SuffocatingWeaponHashes, target, debug))
            {
                return WeaponClasses.SUFFOCATING;
            }

            return WeaponClasses.NOTHING;
        }

        private static bool PedWasDamaged(IEnumerable<uint> hashes, Ped target, bool debug = false)
        {
            foreach (uint hash in hashes)
            {
                if (!Function.Call<bool>(Hash.HAS_PED_BEEN_DAMAGED_BY_WEAPON, target, hash, 0)) continue;
                
                if (debug)
                {
                    Enum.TryParse(hash.ToString(), out WeaponHash hitBy);
                    UI.Notify("Hit by " + hitBy);
                    UI.Notify($"You got {WeaponClasses.SUFFOCATING}");
                }

                return true;
            }

            return false;
        }
        
        public static float DistanceToPlayer(Ped target)
        {
            return World.GetDistance(Game.Player.Character.Position, target.Position);
        }
    }

    public static class RandomExtensions
    {
        public static bool TrueWithChance(this Random random, float chance = 0.5f)
        {
            return random.NextDouble() >= chance;
        }
    }
}

    