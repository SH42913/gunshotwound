using System;
using System.Threading.Tasks;
using GTA;
using GTA.Native;

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

namespace GunshotWound.WoundedPeds
{
    /// <summary>
    /// Wounded ped
    /// </summary>
    internal class WoundedPed : IEnhancedPed
    {
        [Flags]
        private enum DamageTypes
        {
            LEGS_DAMAGED = 1,
            ARMS_DAMAGED = 2,
            NERVES_DAMAGED = 4,
            GUTS_DAMAGED = 8,
            STOMACH_DAMAGED = 16,
            LUNGS_DAMAGED = 32,
            HEART_DAMAGED = 64,
        }
        
        /// <summary>
        /// Manager script
        /// </summary>
        public GunshotWound ManagerScript { get; set; }

        private Ped _targetPed;
        /// <summary>
        /// Target ped
        /// </summary>
        public Ped TargetPed
        {
            get { return _targetPed; }
            set
            {
                _targetPed = value;
                if (TargetPed.Armor >= ManagerScript.MaxArmor)
                {
                    CurrentArmor = ManagerScript.MaxArmor;
                }
            }
        }

        private bool _isPlayer;
        /// <summary>
        /// This ped is player
        /// </summary>
        public bool IsPlayer
        {
            get { return _isPlayer; }
            set
            {
                _isPlayer = value;
                if (_isPlayer) return;

                TargetPed.Health =
                    (int) (0.5f * ManagerScript.MaxHealth + rand.NextDouble() * ManagerScript.MaxHealth / 2);
                defaultAccuracy = TargetPed.Accuracy;
            }
        }

        private string HeShe
        {
            get { return TargetPed.Gender == Gender.Male ? "He" : "She"; }
        }

        private int currentArmor;

        private int CurrentArmor
        {
            get { return currentArmor; }
            set
            {
                currentArmor = value;
                TargetPed.Armor = currentArmor;
            }
        }

        private float RefreshNotificationsTimer { get; set; }

        private bool DoctorIsNear { get; set; }

        
        
        private BleedingStates bleedingState;
        private int bleedingLevel;
        private int secondBleedingLevel;
        private BleedingStates BleedingState
        {
            set
            {
                switch (value)
                {
                    case BleedingStates.LIGHT:
                        break;
                    case BleedingStates.MEDIUM:
                        break;
                    case BleedingStates.HEAVY:
                        if (IsPlayer)
                        {
                            Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                        }
                        break;
                    case BleedingStates.DEADLY:
                        if (IsPlayer)
                        {
                            Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                        }
                        else
                        {
                            if (ManagerScript.EnemyNotificationsEnabled)
                            {
                                UI.Notify($"{HeShe} has lost a lot of blood");
                            }
                        }
                        break;
                }
                
                if (value < bleedingState && bleedingState < BleedingStates.DEADLY)
                {
                    secondBleedingLevel++;
                    if (bleedingLevel + secondBleedingLevel/2 > ManagerScript.MaxBleedingLevel)
                        BleedingState = bleedingState + 1;
                }
                else if (bleedingState == value && bleedingState < BleedingStates.DEADLY)
                {
                    bleedingLevel++;
                    if (bleedingLevel + secondBleedingLevel/2 > ManagerScript.MaxBleedingLevel)
                        BleedingState = bleedingState + 1;
                }
                else if(value > bleedingState)
                {
                    bleedingState = value;
                    bleedingLevel = 0;
                }
                
                bleedingHealTimer = ManagerScript.TimeToHealBleeding * (int) bleedingState;
            }
        }
        private float bleedingTimer;
        private float bleedingHealTimer;
        
        
        
        private WoundStates woundState;
        private int woundLevel;
        private int secondWoundLevel;
        private WoundStates WoundState
        {
            set
            {
                switch (value)
                {
                        case WoundStates.LIGHT:
                            TargetPed.Health -= TargetPed.Armor < 1
                                ? ManagerScript.AdditionalDamageOnLightWounds
                                : 2 * ManagerScript.AdditionalDamageOnLightWounds;
                            break;
                        case WoundStates.MEDIUM:
                            TargetPed.Health -= TargetPed.Armor < 1
                                ? ManagerScript.AdditionalDamageOnMediumWounds
                                : 2 * ManagerScript.AdditionalDamageOnMediumWounds;
                            break;
                        case WoundStates.HEAVY:
                            TargetPed.Health -= TargetPed.Armor < 1
                                ? ManagerScript.AdditionalDamageOnHeavyWounds
                                : 2 * ManagerScript.AdditionalDamageOnHeavyWounds;
                            if (IsPlayer)
                            {
                                Function.Call(Hash._SET_CAM_EFFECT, 1);
                                Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                            }
                            break;
                        case WoundStates.DEADLY:
                            TargetPed.Health -= TargetPed.Armor < 1
                                ? ManagerScript.AdditionalDamageOnDeadlyWounds
                                : 2 * ManagerScript.AdditionalDamageOnDeadlyWounds;
                            if (IsPlayer)
                            {
                                Function.Call(Hash._SET_CAM_EFFECT, 2);
                                Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                                Function.Call(Hash._START_SCREEN_EFFECT, "DrugsDrivingIn", 5000, true);
                            }
                            else
                            {
                                if (ManagerScript.EnemyNotificationsEnabled)
                                    UI.Notify("Another one dead!");
                                
                                TargetPed.Task.FleeFrom(Game.Player.Character, 10000);
                            }
                            break;
                }
                
                if (value < woundState && woundState < WoundStates.DEADLY)
                {
                    secondWoundLevel++;
                    if (woundLevel + secondWoundLevel/2 > ManagerScript.MaxWoundLevel)
                        WoundState = woundState + 1;
                }
                else if(woundState == value && woundState < WoundStates.DEADLY)
                {
                    woundLevel++;
                    if (woundLevel + secondWoundLevel/2 > ManagerScript.MaxWoundLevel)
                        WoundState = woundState + 1;
                }
                else if(value > woundState)
                {
                    woundState = value;
                    woundLevel = 0;
                    secondWoundLevel = 0;
                }
                
                woundHealTimer = ManagerScript.TimeToHealWounds * (int) woundState;
            }
        }

        private DamageTypes damages;
        private float deathTimer;
        private float woundHealTimer;
        private int defaultAccuracy;
        private float onFireTimer;
        private float chockingTimer;
        private readonly Random rand = new Random();

        private float foolsDayTimer = 600;

        public async Task UpdateAsync()
        {
            await Task.Run(() => Update());
        }

        public void Update()
        {
            if (ManagerScript.FoolsDay)
            {
                foolsDayTimer -= Game.LastFrameTime;
                if (foolsDayTimer < 0)
                {
                    foolsDayTimer = rand.Next(300, 600);
                    if (rand.NextDouble() > 0.8d)
                    {
                        int text = rand.Next(0, 4);
                        switch (text)
                        {
                            case 0:
                                if (IsPlayer)
                                {
                                    UI.Notify("~r~You got heart attack!");
                                }
                                else if(ManagerScript.EnemyNotificationsEnabled)
                                {
                                    UI.Notify($"{HeShe} got heart attack!");
                                }
                                AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                break;
                            case 1: 
                                if (IsPlayer)
                                {
                                    UI.Notify("~r~You have a stroke!");
                                }
                                else if(ManagerScript.EnemyNotificationsEnabled)
                                {
                                    UI.Notify($"{HeShe} had a stroke!");
                                }
                                AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                WoundState = WoundStates.DEADLY;
                                break;
                            case 2: 
                                if (IsPlayer)
                                {
                                    UI.Notify("~r~You have an aneurysm of the aorta!");
                                }
                                else if(ManagerScript.EnemyNotificationsEnabled)
                                {
                                    UI.Notify($"{HeShe} had an aneurysm of the aorta!");
                                }
                                AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                BleedingState = BleedingStates.DEADLY;
                                break;
                            case 3: 
                                if (IsPlayer)
                                {
                                    UI.Notify("~r~You were bitten by a bee. You have anaphylactic shock!");
                                }
                                else if(ManagerScript.EnemyNotificationsEnabled)
                                {
                                    UI.Notify($"{HeShe} was bitten by a bee. {HeShe} anaphylactic shock!");
                                }
                                WoundState = WoundStates.DEADLY;
                                break;
                        }
                    }
                    else
                    {
                        if (IsPlayer) UI.Notify("~r~Death: ~s~~italic~We will see another time, lucker");
                    }
                }
            }
            
            if (IsPlayer)
            {
                RefreshPed();
            }

            onFireTimer -= Game.LastFrameTime;
            chockingTimer -= Game.LastFrameTime;
            
            if (TargetPed.Armor > ManagerScript.MaxArmor)
            {
                CurrentArmor = ManagerScript.MaxArmor;
            }
            TargetPed.Armor = CurrentArmor;
                        
            CheckDamage(ManagerScript.GetDamagedWeaponClass(TargetPed),
                ManagerScript.GetDamagedBodyPart(TargetPed));

            BleedingHealing();
            WoundsHealingAndDeathTimer();

            CheckHealth();
            
            RefreshWoundBehavior();
            
            MedicsHealPlayer();

            if (!IsPlayer) return;
            
            if (RefreshNotificationsTimer > 0)
            {
                RefreshNotificationsTimer -= Game.LastFrameTime;
            }
            else if(ManagerScript.TimeToRefreshNotifications > 0)
            {
                WoundsNotifications();
                BleedingNotifications();
                RefreshNotificationsTimer = ManagerScript.TimeToRefreshNotifications;
            }
        }

        private void RefreshWoundBehavior()
        {
            string animationName = ManagerScript.AnimationOnNoneWounds;
            
            switch (woundState)
            {
                case WoundStates.NONE:
                    animationName = ManagerScript.AnimationOnNoneWounds;
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, TargetPed, 1f);
                    deathTimer = ManagerScript.TimeToDeath;
                    if (IsPlayer)
                    {
                        if (!damages.HasFlag(DamageTypes.NERVES_DAMAGED) && !damages.HasFlag(DamageTypes.ARMS_DAMAGED))
                        {
                            Function.Call(Hash._SET_CAM_EFFECT, 0);
                        }
                    }
                    else
                    {
                        if (bleedingState == BleedingStates.NONE)
                        {
                            TargetPed.Accuracy = defaultAccuracy;
                        }
                    }
                    break;
                
                case WoundStates.LIGHT:
                    animationName = ManagerScript.AnimationOnLightWounds;
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, TargetPed, ManagerScript.MoveRateOnLightWounds);
                    if (IsPlayer)
                    {
                        if (!damages.HasFlag(DamageTypes.NERVES_DAMAGED) && !damages.HasFlag(DamageTypes.ARMS_DAMAGED))
                        {
                            Function.Call(Hash._SET_CAM_EFFECT, 0);
                        }
                    }
                    else
                    {
                        if (bleedingState == BleedingStates.HEAVY || bleedingState == BleedingStates.DEADLY)
                        {
                            TargetPed.Accuracy = (int) (defaultAccuracy * 0.5f);
                        }
                        else
                        {
                            TargetPed.Accuracy = (int) (defaultAccuracy * 0.8f);
                        }
                    }
                    break;
                
                case WoundStates.MEDIUM:
                    animationName = ManagerScript.AnimationOnMediumWounds;
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, TargetPed, ManagerScript.MoveRateOnMediumWounds);
                    if (IsPlayer)
                    {
                        if (!damages.HasFlag(DamageTypes.NERVES_DAMAGED) && !damages.HasFlag(DamageTypes.ARMS_DAMAGED))
                        {
                            Function.Call(Hash._SET_CAM_EFFECT, 0);
                        }
                    }
                    else
                    {
                        TargetPed.Accuracy = (int) (defaultAccuracy * 0.5f);
                    }
                    break;
                
                case WoundStates.HEAVY:
                    animationName = ManagerScript.AnimationOnHeavyWounds;
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, TargetPed, ManagerScript.MoveRateOnHeavyWounds);
                    if (!IsPlayer)
                    {
                        TargetPed.Accuracy = (int) (defaultAccuracy * 0.3f);
                    }
                    break;
                
                case WoundStates.DEADLY:
                    animationName = ManagerScript.AnimationOnDeadlyWounds;
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, TargetPed, ManagerScript.MoveRateOnDeadlyWounds);
                    if (!IsPlayer)
                    {
                        TargetPed.Accuracy = (int) (defaultAccuracy * 0.1f);
                    }
                    break;
            }
            
            if (damages.HasFlag(DamageTypes.NERVES_DAMAGED) || damages.HasFlag(DamageTypes.ARMS_DAMAGED))
            {
                if (IsPlayer)
                {
                    Function.Call(Hash._SET_CAM_EFFECT, 2);
                }
                else
                {
                    TargetPed.Accuracy = (int) (defaultAccuracy * 0.1f);
                }
            }

            if (damages.HasFlag(DamageTypes.NERVES_DAMAGED) || damages.HasFlag(DamageTypes.LEGS_DAMAGED))
            {
                if(woundState < WoundStates.DEADLY)
                    animationName = ManagerScript.AnimationOnNervesDamage;
                
                Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, TargetPed, ManagerScript.MoveRateOnNervesDamage);
            }
            Function.Call(Hash.REQUEST_ANIM_SET, animationName);
            
            CheckSprintPossibility();

            if (!TargetPed.IsAlive) return;
            
            if (!Function.Call<bool>(Hash.HAS_ANIM_SET_LOADED, animationName))
            {
                Function.Call(Hash.REQUEST_ANIM_SET, animationName);
            }
            else
            {
                Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET, TargetPed, animationName, 1.0f);
            }
        }

        private void CheckSprintPossibility()
        {
            if (!IsPlayer) return;
            
            if (!(damages.HasFlag(DamageTypes.NERVES_DAMAGED) || damages.HasFlag(DamageTypes.LEGS_DAMAGED))
                && woundState < WoundStates.HEAVY)
            {
                Function.Call(Hash.SET_PLAYER_SPRINT, Game.Player, true);
            }
            else
            {
                Function.Call(Hash.SET_PLAYER_SPRINT, Game.Player, false);
            }
        }

        private void WoundsHealingAndDeathTimer()
        {
            if (woundState > WoundStates.NONE && woundState != WoundStates.DEADLY)
            {
                int doctorMult = DoctorIsNear ? 5 : 1;
                
                if (woundHealTimer > 0)
                    woundHealTimer -= Game.LastFrameTime * doctorMult;
                else
                {
                    woundState = woundState - 1;
                    woundLevel = 0;
                    
                    woundHealTimer = ManagerScript.TimeToHealWounds * (int) woundState;

                    if (ManagerScript.DebugMode)
                    {
                        UI.Notify("Heal wounds to " + (int) woundState);
                    }
                    else if (IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                    {
                        UI.Notify(woundState == WoundStates.NONE
                            ? "You don't feel pain anymore"
                            : "Some wounds have been healed");
                    }
                }
            }
            else if (woundState == WoundStates.DEADLY)
            {
                if(IsPlayer && ManagerScript.SlowMoOnDeadlyWound)
                    Game.TimeScale = 0.5f + 0.5f * TargetPed.Health / ManagerScript.MaxHealth;
                
                if (deathTimer > 0)
                {
                    int multipl = 1;
                    if (damages.HasFlag(DamageTypes.HEART_DAMAGED))
                    {
                        multipl = 10;
                    }
                    else if (damages.HasFlag(DamageTypes.LUNGS_DAMAGED))
                    {
                        multipl = 3;
                    }
                    else if (damages.HasFlag(DamageTypes.STOMACH_DAMAGED))
                    {
                        multipl = 2;
                    }
                    else if (damages.HasFlag(DamageTypes.GUTS_DAMAGED))
                    {
                        multipl = 2;
                    }
                    
                    deathTimer -= multipl * Game.LastFrameTime;
                }
                else
                {
                    TargetPed.Health = 10;
                    TargetPed.ApplyDamage(500);
                    TargetPed.Health -= 500;
                    DropDownWounds();
                }
            }
        }

        private void BleedingHealing()
        {
            if (bleedingState <= BleedingStates.NONE) return;
            
            int doctorMult = DoctorIsNear ? 5 : 1;
            if (onFireTimer > 0)
                doctorMult *= 2;
                
            if (bleedingTimer > 0)
                if (bleedingState == BleedingStates.DEADLY)
                {
                    bleedingTimer -= Game.LastFrameTime * 2;
                }
                else
                {
                    bleedingTimer -= Game.LastFrameTime;
                }
            else
            {
                int bleedDamage = 0;
                    
                switch (woundState)
                {
                    case WoundStates.LIGHT:
                        bleedDamage = ManagerScript.LightBleedingDamage;
                        break;
                    case WoundStates.MEDIUM:
                        bleedDamage = ManagerScript.MediumBleedingDamage;
                        break;
                    case WoundStates.HEAVY:
                        bleedDamage = ManagerScript.HeavyBleedingDamage;
                        break;
                    case WoundStates.DEADLY:
                        bleedDamage = ManagerScript.DeadlyBleedingDamage;
                        break;
                }

                TargetPed.Health -= bleedDamage;
                bleedingTimer = ManagerScript.TimeToBleed;
            }

            if (bleedingHealTimer > 0)
            {
                bleedingHealTimer -= Game.LastFrameTime * doctorMult;
            }
            else if(bleedingState != BleedingStates.DEADLY)
            {
                bleedingState = bleedingState - 1;
                bleedingLevel = 0;
                    
                bleedingHealTimer = ManagerScript.TimeToHealBleeding * (int) bleedingState;
                CheckSprintPossibility();

                if (ManagerScript.DebugMode)
                {
                    UI.Notify("Heal bleeding to " + bleedingState);
                }
                else if (IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                {
                    UI.Notify(bleedingState == BleedingStates.NONE
                        ? "Bleeding stopped"
                        : "Some wounds have been healed");
                }
            }
        }

        private void CheckHealth()
        {
            if (TargetPed.Health > ManagerScript.MaxHealth ||
                TargetPed.Health == ManagerScript.MaxHealth && (woundState > WoundStates.NONE ||
                bleedingState > BleedingStates.NONE))
            {
                Function.Call(Hash.CLEAR_PED_BLOOD_DAMAGE, TargetPed);
                TargetPed.Health = ManagerScript.MaxHealth;
                DropDownWounds();
            }
            else if (TargetPed.Health < 0)
            {
                woundState = WoundStates.DEADLY;
                deathTimer = 0;
            }

            if (IsPlayer && ManagerScript.SelfHealingAmount > 0 && woundState == WoundStates.NONE && bleedingState == BleedingStates.NONE)
            {
                Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, Game.Player, ManagerScript.SelfHealingAmount);
            }
            else if (IsPlayer)
            {
                Function.Call(Hash.SET_PLAYER_HEALTH_RECHARGE_MULTIPLIER, Game.Player, 0f);
            }

            if (DoctorIsNear && TargetPed.Health < ManagerScript.MaxHealth && ManagerScript.ticks % 20 == 0)
            {
                TargetPed.Health += 1;
            }
        }

        public string GetSubtitlesInfo()
        {
            string healthInfo = "";
            if (TargetPed.Health > 0)
            {
                var healthPercent = (float) TargetPed.Health / ManagerScript.MaxHealth;

                if (healthPercent >= 0.8f)
                {
                    healthInfo = $"Your health is {healthPercent * 100}%\n";
                }
                else if(healthPercent > 0.5f)
                {
                    healthInfo = $"Your health is ~y~{healthPercent * 100}%~s~\n";
                }
                else if(healthPercent > 0.2f)
                {
                    healthInfo = $"Your health is ~o~{healthPercent * 100}%~s~\n";
                }
                else
                {
                    healthInfo = $"Your health is ~r~{healthPercent * 100}%~s~\n";
                }
            }

            if (TargetPed.Armor > 0)
            {
                var armorPercent = (float) TargetPed.Armor / ManagerScript.MaxArmor;

                if (armorPercent > 0.8f)
                {
                    healthInfo += "Your armor looks great\n";
                }
                else if (armorPercent > 0.5f)
                {
                    healthInfo += "~y~Some scratches on your armor~s~\n";
                }
                else if (armorPercent > 0.2f)
                {
                    healthInfo += "~o~Large dents on your armor~s~\n";
                }
                else
                {
                    healthInfo += "~r~Your armor looks awful~s~\n";
                }
            }

            if (woundState > WoundStates.NONE)
            {
                healthInfo += "Wounds: ";
                switch (woundState)
                {
                    case WoundStates.LIGHT:
                        healthInfo += "~s~Light\n";
                        break;
                    case WoundStates.MEDIUM:
                        healthInfo += "~y~Medium~s~\n";
                        break;
                    case WoundStates.HEAVY:
                        healthInfo += "~o~HEAVY~s~\n";
                        break;
                    case WoundStates.DEADLY:
                        healthInfo += "~r~DEADLY~s~\n";
                        healthInfo += "~r~You are dying!~s~\n";
                        break;
                }
            }

            if (bleedingState > BleedingStates.NONE)
            {
                healthInfo += "Bleeding: ";
                switch (bleedingState)
                {
                    case BleedingStates.LIGHT:
                        healthInfo += "~s~Light\n";
                        break;
                    case BleedingStates.MEDIUM:
                        healthInfo += "~y~Medium~s~\n";
                        break;
                    case BleedingStates.HEAVY:
                        healthInfo += "~o~HEAVY~s~\n";
                        break;
                    case BleedingStates.DEADLY:
                        healthInfo += "~r~DEADLY~s~\n";
                        break;
                }
            }

            if (damages > 0)
            {
                healthInfo += "Damaged body parts: ";
                    
                if (damages.HasFlag(DamageTypes.NERVES_DAMAGED))
                {
                    healthInfo += "~r~Nerves ";
                }
                    
                if (damages.HasFlag(DamageTypes.HEART_DAMAGED))
                {
                    healthInfo += "~r~Heart ";
                }
                    
                if (damages.HasFlag(DamageTypes.LUNGS_DAMAGED))
                {
                    healthInfo += "~r~Lungs ";
                }
                    
                if (damages.HasFlag(DamageTypes.STOMACH_DAMAGED))
                {
                    healthInfo += "~r~Stomach ";
                }
                    
                if(damages.HasFlag(DamageTypes.GUTS_DAMAGED))
                {
                    healthInfo += "~r~Guts ";
                }
                    
                if (damages.HasFlag(DamageTypes.ARMS_DAMAGED))
                {
                    healthInfo += "~r~Arms ";
                }
                    
                if (damages.HasFlag(DamageTypes.LEGS_DAMAGED))
                {
                    healthInfo += "~r~Legs ";
                }

                healthInfo += "~s~\n";
            }

            if (onFireTimer > 0)
            {
                healthInfo += "~r~You're on fire!~s~\n";
            }

            if (chockingTimer > 0)
            {
                healthInfo += "~r~You're choking!~s~\n";
            }

            if (ManagerScript.DebugMode)
            {
                healthInfo += $"Armor {TargetPed.Armor} CurrArmor {CurrentArmor}\n";
                healthInfo += $"Time to healing B{(int)bleedingState} {bleedingHealTimer:0.0} " +
                              $"and W{(int)woundState} {woundHealTimer:0.0}\n";
                healthInfo += $"Accuracy {TargetPed.Accuracy}\n";
                healthInfo += $"Fool's Day Timer {foolsDayTimer:0.0}\n";
            }

            return healthInfo;
        }

        public void ShowNotifications()
        {
            BleedingNotifications();
            WoundsNotifications();
        }
        
        private void BleedingNotifications()
        {
            if (bleedingState == BleedingStates.LIGHT)
                {
                    int text = rand.Next(0, 4);
                    switch (text)
                    {
                        case 0: UI.Notify("Some scratches"); break;
                        case 1: UI.Notify("Some scratches"); break;
                        case 2: UI.Notify("Some scratches"); break;
                        case 3: UI.Notify("Some scratches"); break;
                    }
                }
                else if (bleedingState == BleedingStates.MEDIUM)
                {
                    int text = rand.Next(0, 4);
                    switch (text)
                    {
                        case 0: UI.Notify("Moderate bleeding"); break;
                        case 1: UI.Notify("Moderate bleeding"); break;
                        case 2: UI.Notify("Moderate bleeding"); break;
                        case 3: UI.Notify("Moderate bleeding"); break;
                    }
                }
                else if(bleedingState == BleedingStates.HEAVY)
                {
                    int text = rand.Next(0, 4);
                    switch (text)
                    {
                        case 0: UI.Notify("~y~Serious bleeding"); break;
                        case 1: UI.Notify("~y~Serious bleeding"); break;
                        case 2: UI.Notify("~y~Serious bleeding"); break;
                        case 3: UI.Notify("~y~Serious bleeding"); break;
                    }
                }
                else if (bleedingState == BleedingStates.DEADLY)
                {
                    int text = rand.Next(0, 4);
                    switch (text)
                    {
                        case 0: UI.Notify("~r~Artery severed"); break;
                        case 1: UI.Notify("~r~Artery severed"); break;
                        case 2: UI.Notify("~r~Artery severed"); break;
                        case 3: UI.Notify("~r~Artery severed"); break;
                    }
                }
        }

        private void WoundsNotifications()
        {
            if (damages.HasFlag(DamageTypes.NERVES_DAMAGED))
            {
                UI.Notify("Limited limb mobility: ~r~nerves are damaged");
            }
            else
            {
                if (damages.HasFlag(DamageTypes.ARMS_DAMAGED))
                {
                    UI.Notify("Difficulty aiming: ~r~arms are damaged");
                }    
                else if (damages.HasFlag(DamageTypes.LEGS_DAMAGED))
                {
                    UI.Notify("Slow moving: ~r~legs are damaged");
                }
            }
            
            if(damages.HasFlag(DamageTypes.LUNGS_DAMAGED))
            {
                UI.Notify("You are hard breathing: ~r~lungs are punctured");
            }
            
            if(damages.HasFlag(DamageTypes.HEART_DAMAGED))
            {
                UI.Notify("You feel awful pain in the chest: ~r~heart is damaged");
            }
            
            if(damages.HasFlag(DamageTypes.STOMACH_DAMAGED))
            {
                UI.Notify("You are very sick: ~r~stomach is damaged");
            }
            
            if(damages.HasFlag(DamageTypes.GUTS_DAMAGED))
            {
                UI.Notify("You are very sick: ~r~guts are damaged");
            }
                
            if (woundState == WoundStates.LIGHT)
            {
                int text = rand.Next(0, 4);
                switch (text)
                {
                    case 0: UI.Notify("Winded"); break;
                    case 1: UI.Notify("Light shock detected"); break;
                    case 2: UI.Notify("Minor bruise"); break;
                    case 3: UI.Notify("Shocked"); break;
                }                
            }    
            else if (woundState == WoundStates.MEDIUM)
            {
                int text = rand.Next(0, 4);
                switch (text)
                {
                    case 0: UI.Notify("Moderate trauma detected"); break;
                    case 1: UI.Notify("Subdural hematoma detected"); break;
                    case 2: UI.Notify("Large bruise"); break;
                    case 3: UI.Notify("Serious injuries"); break;
                }
            }
            else if (woundState == WoundStates.HEAVY)
            {
                int text = rand.Next(0, 3);
                switch (text)
                {
                    case 0: UI.Notify("~y~Life threatening condition detected"); break;
                    case 1: UI.Notify("~y~Heavy internal injuries"); break;
                    case 2: UI.Notify("~y~Stabilizing in emergency state"); break;
                }
            }
            else if (woundState == WoundStates.DEADLY)
            {
                int text = rand.Next(0, 2);
                switch (text)
                {
                    case 0: UI.Notify("~r~~bold~Death imminent"); break;
                    case 2: UI.Notify("~r~~bold~You feel you are dying"); break;
                }
            }            
        }

        private void RefreshPed()
        {
            Ped oldPed = TargetPed;
            TargetPed = Game.Player.Character;
            
            if (!TargetPed.Equals(oldPed))
            {
                DropDownWounds();
            }
        }

        public void RestoreDefaultState()
        {
            DropDownWounds();
        }

        private void DropDownWounds()
        {
            bleedingState = BleedingStates.NONE;
            woundState = WoundStates.NONE;
            damages = 0;
            bleedingTimer = ManagerScript.TimeToBleed;
            bleedingHealTimer = ManagerScript.TimeToHealBleeding;
            woundHealTimer = ManagerScript.TimeToHealWounds;
            deathTimer = ManagerScript.TimeToDeath;
            TargetPed.Health = ManagerScript.MaxHealth;
            TargetPed.AlwaysDiesOnLowHealth = false;
            TargetPed.CanFlyThroughWindscreen = true;
            
            if (!IsPlayer) return;
            
            if (ManagerScript.SlowMoOnDeadlyWound)
                Game.TimeScale = 1f;
            CheckSprintPossibility();
            Function.Call(Hash._STOP_ALL_SCREEN_EFFECTS);
        }

        private void MedicsHealPlayer()
        {
            if (IsPlayer && (TargetPed.Health < ManagerScript.MaxHealth || woundState > WoundStates.NONE ||
                bleedingState > BleedingStates.NONE))
            {
                foreach (Ped ped in World.GetNearbyPeds(TargetPed, 15f))
                {
                    if (ped.IsAlive &&
                        (ped.Model == (Model) PedHash.Paramedic01SMM ||
                         ped.Model == (Model) PedHash.Doctor01SMM ||
                         ped.Model == (Model) PedHash.Autopsy01SMY ||
                         ped.Model == (Model) PedHash.Scientist01SMM ||
                         ped.Model == (Model) PedHash.Jesus01))
                    {
                        if (TargetPed.Health < 0.8f * ManagerScript.MaxHealth || woundState > WoundStates.NONE ||
                            bleedingState > BleedingStates.NONE)
                        {
                            if (ped.IsInVehicle())
                            {
                                ped.Task.LeaveVehicle();
                            }
                            ped.Task.RunTo(TargetPed.Position);
                        }
                        else
                        {
                            ped.Task.EnterVehicle();
                        }

                        DoctorIsNear = World.GetDistance(ped.Position, TargetPed.Position) < 2f;

                        if (IsPlayer && ManagerScript.DebugMode)
                        {
                            UI.ShowSubtitle("Doctor is here");
                        }
                    
                        return;
                    }   
                }
            }
            DoctorIsNear = false;
        }

        private void CheckDamage(WeaponClasses weapon, BodyParts bone)
        {
            switch (weapon)
            {
                default:
                    return;
                case WeaponClasses.NOTHING:
                    return;
                case WeaponClasses.OTHER:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            bool helmetSave = TargetPed.IsWearingHelmet && rand.Next(0, 2) == 1;

                            if (!helmetSave)
                            {
                                int situation = rand.Next(0, 8);
                                string situationText = "";

                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Minor scratches and light bruising";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Winded from impact";
                                        if (IsPlayer)
                                        {
                                            Function.Call(Hash._SET_CAM_EFFECT, 1);
                                        }
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Moderate scratches and contusions";
                                        if (IsPlayer)
                                        {
                                            Function.Call(Hash._SET_CAM_EFFECT, 1);
                                        }
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Life threatening internal bleeding detected";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 4:
                                        situationText = "Blackout possible";
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 5:
                                        situationText = "Badly damaged head";
                                        WoundState = WoundStates.HEAVY;
                                        BleedingState = BleedingStates.HEAVY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 6:
                                        situationText = "Dazed from blow";
                                        if (IsPlayer)
                                        {
                                            Function.Call(Hash._SET_CAM_EFFECT, 1);
                                        }
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 7:
                                        situationText = "Major blunt trauma/TBI detected";
                                        WoundState = WoundStates.HEAVY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }

                                if (ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                if (IsPlayer && ManagerScript.TimeToRefreshNotifications > 0 && TargetPed.IsWearingHelmet)
                                    UI.Notify("You are lucky! Helmet saves your head!");
                            }

                            return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Minor scratches and light bruising";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Moderate scratches";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Life threatening bleeding detected";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 4:
                                    situationText = "Mobility temporarily limited from impact";
                                    WoundState = WoundStates.HEAVY;
                                    break;
                                case 5:
                                    situationText = "Broken neck detected";
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Minor scratches and light bruising";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Moderate scratches";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Life threatening bleeding detected";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 4:
                                    situationText = "Mobility temporarily limited from impact";
                                    WoundState = WoundStates.HEAVY;
                                    break;
                                case 5:
                                    situationText = "Heartbreak";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.DEADLY;
                                    AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Minor scratches and light bruising";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Moderate scratches";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Life threatening bleeding detected";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 4:
                                    situationText = "Mobility temporarily limited from impact";
                                    WoundState = WoundStates.HEAVY;
                                    break;
                                case 5:
                                    situationText = "Punctured stomach detected. ~r~Potentially fatal injury";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.DEADLY;
                                    AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Punctured guts detected. ~r~Potentially fatal injury";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.DEADLY;
                                    AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Minor scratches and light bruising";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Moderate scratches";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Life threatening bleeding detected";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 4:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "Broken arm detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                        case BodyParts.LEG:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Minor scratches and light bruising";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Moderate scratches";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Life threatening bleeding detected";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 4:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "Broken leg detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                    }
                }
                case WeaponClasses.SMALL_CALIBER:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            bool helmetSave = TargetPed.IsWearingHelmet && rand.Next(0, 2) == 1;

                            if (!helmetSave)
                            {
                                int situation = rand.Next(0, 5);
                                string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from ricochet/fragment. " +
                                                        "Light bleeding detected";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Part of ear sails off away";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Bullet fly through your head";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 3:
                                        situationText = "Small caliber bullet torn apart your brain.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 4:
                                        situationText = "Heavy brain damage detected.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                                return;
                            }
                            else
                            {
                                if (IsPlayer && ManagerScript.TimeToRefreshNotifications > 0 && TargetPed.IsWearingHelmet)
                                    UI.Notify("You are lucky! Bullet stuck in helmet!");
                            }
                            
                            return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from ricochet/fragment. " +
                                                    "Light bleeding detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Through-and-through small caliber gunshot wound. nerves severed";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Small caliber bullet in your neck. nerves severed";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed.";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 9);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from richochet/fragment. " +
                                                        "Light bleeding detected";
                                        WoundState = WoundStates.LIGHT;
                                        BleedingState = BleedingStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Moderate bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 4:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 5:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured or collapsed lung detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "Punctured heart detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                        break;
                                    case 8:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 10);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from richochet/fragment. " +
                                                        "Light bleeding detected";
                                        WoundState = WoundStates.LIGHT;
                                        BleedingState = BleedingStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Moderate bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 4:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 5:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured stomach detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "Punctured guts detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                        break;
                                    case 8:
                                        situationText = "~o~Artery severed";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 9:
                                        situationText = "Bullet torn apart your balls";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        if (ManagerScript.EnemyNotificationsEnabled && !IsPlayer &&
                                            HeShe == "He")
                                            UI.Notify("You see his balls fly away\nUh, probably very painful");
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Light bleeding detected";
                                    WoundState = WoundStates.LIGHT;
                                    BleedingState = BleedingStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Moderate bleeding from GSW path";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through small caliber gunshot wound. " +
                                                    "Moderate bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Arm bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                           int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Light bleeding detected";
                                    WoundState = WoundStates.LIGHT;
                                    BleedingState = BleedingStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Moderate bleeding from GSW path";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through small caliber gunshot wound. " +
                                                    "Moderate bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Leg bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.MEDIUM_CALIBER:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            int situation = rand.Next(0, 5);
                            string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from ricochet/fragment. " +
                                                        "Light bleeding detected";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Part of ear sails off away";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Bullet fly through your head";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 3:
                                        situationText = "Small caliber bullet torn apart your brain.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 4:
                                        situationText = "Heavy brain damage detected.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                                return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from ricochet/fragment. " +
                                                    "Light bleeding detected";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Spalling/Fragmentation risk detected. Medium bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 3:
                                    situationText = "Through-and-through medium caliber gunshot wound. Nerves severed";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Medium caliber bullet stuck in your neck. Nerves severed";
                                    if (IsPlayer)
                                    {
                                        Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                                    }
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.DEADLY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed.";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 9);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from richochet/fragment. " +
                                                        "Light bleeding detected";
                                        WoundState = WoundStates.LIGHT;
                                        BleedingState = BleedingStates.MEDIUM;
                                        break;
                                    case 1:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 2:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Medium bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 4:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 5:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.HEAVY;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured or collapsed lung detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "Punctured heart detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                        break;
                                    case 8:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= 2 * ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 10);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from richochet/fragment. " +
                                                        "Light bleeding detected";
                                        WoundState = WoundStates.LIGHT;
                                        BleedingState = BleedingStates.MEDIUM;
                                        break;
                                    case 1:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 2:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Medium bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 4:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 5:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.HEAVY;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured stomach detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "Punctured guts detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                        break;
                                    case 8:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 9:
                                        situationText = "Bullet torn apart your balls";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        if (ManagerScript.EnemyNotificationsEnabled && !IsPlayer &&
                                            HeShe == "He")
                                            UI.Notify("You see his balls fly away\nUh, probably very painful");
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= 2 * ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Medium bleeding detected";
                                    WoundState = WoundStates.LIGHT;
                                    BleedingState = BleedingStates.MEDIUM;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 2:
                                    situationText = "Medium bleeding from GSW path";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through medium caliber gunshot wound. " +
                                                    "Medium bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Arm bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Medium bleeding detected";
                                    WoundState = WoundStates.LIGHT;
                                    BleedingState = BleedingStates.MEDIUM;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 2:
                                    situationText = "Medium bleeding from GSW path";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through medium caliber gunshot wound. " +
                                                    "Medium bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Leg bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.HIGH_CALIBER:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            int situation = rand.Next(0, 4);
                            string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Part of ear sails off away";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Bullet fly through your head";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 2:
                                        situationText = "High caliber bullet torn apart your brain.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 3:
                                        situationText = "Heavy brain damage detected.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                                return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 5);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 1:
                                    situationText = "Spalling/Fragmentation risk detected. Heavy bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Through-and-through high caliber gunshot wound. Nerves severed";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "High caliber bullet stuck in your neck. Nerves severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.DEADLY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "~o~Artery severed.";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 8);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 1:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 2:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Heavy bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 4:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 5:
                                        situationText = "Punctured or collapsed lung detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured heart detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= 3 * ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 9);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 1:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 2:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Heavy bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 4:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 5:
                                        situationText = "Punctured stomach detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured guts detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 8:
                                        situationText = "Bullet torn apart your balls";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        if (ManagerScript.EnemyNotificationsEnabled && !IsPlayer &&
                                            HeShe == "He")
                                            UI.Notify("You see his balls fly away\nUh, probably very painful");
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= 3 * ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Light bleeding detected";
                                    WoundState = WoundStates.MEDIUM;
                                    BleedingState = BleedingStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 2:
                                    situationText = "Heavy bleeding from GSW path";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through medium caliber gunshot wound. " +
                                                    "Heavy bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Arm bone was broken";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Light bleeding detected";
                                    WoundState = WoundStates.MEDIUM;
                                    BleedingState = BleedingStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 2:
                                    situationText = "Heavy bleeding from GSW path";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through medium caliber gunshot wound. " +
                                                    "Heavy bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Leg bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.SHOTGUN:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            int situation = rand.Next(0, 5);
                                string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from ricochet/fragment. " +
                                                        "Light bleeding detected";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Part of ear sails off away";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Bullet fly through your head";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 3:
                                        situationText = "Small caliber bullet torn apart your brain.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 4:
                                        situationText = "Heavy brain damage detected.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                                return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from ricochet/fragment. " +
                                                    "Light bleeding detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Through-and-through small caliber gunshot wound. Nerves severed";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Small caliber bullet in your neck. Nerves severed";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed.";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 9);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from richochet/fragment. " +
                                                        "Light bleeding detected";
                                        WoundState = WoundStates.LIGHT;
                                        BleedingState = BleedingStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Moderate bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 4:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 5:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured or collapsed lung detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "Punctured heart detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                        break;
                                    case 8:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 10);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze injury from richochet/fragment. " +
                                                        "Light bleeding detected";
                                        WoundState = WoundStates.LIGHT;
                                        BleedingState = BleedingStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Long shallow GSW. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Internal bleeding from GSW path";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Through-and-through small caliber gunshot wound. " +
                                                        "Moderate bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 4:
                                        situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 5:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured stomach detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "Punctured guts detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                        break;
                                    case 8:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 9:
                                        situationText = "Bullet torn apart your balls";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        if (ManagerScript.EnemyNotificationsEnabled && !IsPlayer &&
                                            HeShe == "He")
                                            UI.Notify("You see his balls fly away\nUh, probably very painful");
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Light bleeding detected";
                                    WoundState = WoundStates.LIGHT;
                                    BleedingState = BleedingStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Moderate bleeding from GSW path";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through small caliber gunshot wound. " +
                                                    "Moderate bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Arm bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                           int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze injury from richochet/fragment. " +
                                                    "Light bleeding detected";
                                    WoundState = WoundStates.LIGHT;
                                    BleedingState = BleedingStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow GSW. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Moderate bleeding from GSW path";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Through-and-through small caliber gunshot wound. " +
                                                    "Moderate bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Spalling/Fragmentation risk detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Leg bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.IMPACT:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            bool helmetSave = TargetPed.IsWearingHelmet && rand.Next(0, 2) == 1;

                            if (!helmetSave)
                            {
                                int situation = rand.Next(0, 8);
                                string situationText = "";

                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Light bruising";
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Winded from impact";
                                        if (IsPlayer)
                                        {
                                            Function.Call(Hash._SET_CAM_EFFECT, 1);
                                        }
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Moderate scratches and contusions";
                                        if (IsPlayer)
                                        {
                                            Function.Call(Hash._SET_CAM_EFFECT, 1);
                                        }
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Life threatening internal bleeding detected";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 4:
                                        situationText = "Blackout possible";
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 5:
                                        situationText = "Badly damaged head";
                                        WoundState = WoundStates.HEAVY;
                                        BleedingState = BleedingStates.HEAVY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 6:
                                        situationText = "Dazed from blow";
                                        if (IsPlayer)
                                        {
                                            Function.Call(Hash._SET_CAM_EFFECT, 1);
                                        }
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 7:
                                        situationText = "Major blunt trauma/TBI detected";
                                        WoundState = WoundStates.HEAVY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }

                                if (ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                if (IsPlayer && ManagerScript.TimeToRefreshNotifications > 0 && TargetPed.IsWearingHelmet)
                                    UI.Notify("You are lucky! Helmet saves your head!");
                            }

                            return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 5);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Light bruise";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Heavy bruise";
                                    WoundState = WoundStates.HEAVY;
                                    break;
                                case 4:
                                    situationText = "Broken neck detected";
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (TargetPed.Armor < 1)
                            {
                                int situation = rand.Next(0, 6);
                                string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Light bruise";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Moderate bruise";
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 2:
                                        situationText = "Heavy bruise";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Some ribs are broken";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 4:
                                        situationText = "Lungs was punctured by broken ribs";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.HEAVY;
                                        AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                        break;
                                    case 5:
                                        situationText = "Heartbreak";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (TargetPed.Armor < 1)
                            {
                                int situation = rand.Next(0, 5);
                                string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Light bruise";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Moderate bruise";
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 2:
                                        situationText = "Heavy bruise";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 3:
                                        situationText = "Stomach damaged";
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                    case 4:
                                        situationText = "Guts damaged";
                                        WoundState = WoundStates.HEAVY;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Light bruise";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Heavy bruise";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.HEAVY;
                                    break;
                                case 3:
                                    situationText = "Bruised or fractured pinky finger";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 4:
                                    situationText = "Small-joint strain detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "Broken arm detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Light bruise";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Moderate bruise";
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                                case 2:
                                    situationText = "Heavy bruise";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.HEAVY;
                                    break;
                                case 3:
                                    situationText = "Bruised or fractured pinky finger";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 4:
                                    situationText = "Small-joint strain detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "Broken leg detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.CUTTING:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            bool helmetSave = TargetPed.IsWearingHelmet && rand.Next(0, 2) == 1;

                            if (!helmetSave)
                            {
                                int situation = rand.Next(0, 5);
                                string situationText = "";
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze shallow cut. " +
                                                        "Light bleeding detected";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Part of ear sails off away";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Deep stab wound detected.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 3:
                                        situationText = "Brain was torn apart.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                    case 4:
                                        situationText = "Heavy brain damage detected.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                if (IsPlayer && ManagerScript.TimeToRefreshNotifications > 0 && TargetPed.IsWearingHelmet)
                                    UI.Notify("You are lucky! Helmet saves your head!");
                            }
                            
                            return;
                        }
                        case BodyParts.NECK:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze cut wound. " +
                                                    "Light bleeding detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow cut. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 2:
                                    situationText = "Deep stab wound detected. Serious bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 3:
                                    situationText = "Deep stab wound detected. Nerves severed";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Penetrating wound. Nerves severed";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.HEAVY;
                                    AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed.";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 8);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze cut wound. " +
                                                        "Light bleeding detected";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Long shallow cut. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Deep stab wound. Serious bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Shallow stab wound. " +
                                                        "Moderate bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 4:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 5:
                                        situationText = "Punctured or collapsed lung detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured heart detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (CurrentArmor < ManagerScript.ArmorDamage)
                            {
                                string situationText = "";
                                int situation = rand.Next(0, 8);
                                
                                switch (situation)
                                {
                                    case 0:
                                        situationText = "Graze cut wound. " +
                                                        "Light bleeding detected";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 1:
                                        situationText = "Long shallow cut. " +
                                                        "Superficial damage and some bleeding";
                                        BleedingState = BleedingStates.LIGHT;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 2:
                                        situationText = "Deep stab wound. Serious bleeding";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                    case 3:
                                        situationText = "Shallow stab wound. " +
                                                        "Moderate bleeding";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.LIGHT;
                                        break;
                                    case 4:
                                        situationText = "Possible nerves damage. Bleeding detected";
                                        BleedingState = BleedingStates.MEDIUM;
                                        WoundState = WoundStates.MEDIUM;
                                        AddDamageFlag(DamageTypes.NERVES_DAMAGED);
                                        break;
                                    case 5:
                                        situationText = "Punctured stomach detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                        break;
                                    case 6:
                                        situationText = "Punctured guts detected. ~r~Potentially fatal injury";
                                        BleedingState = BleedingStates.HEAVY;
                                        WoundState = WoundStates.DEADLY;
                                        AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                        break;
                                    case 7:
                                        situationText = "~o~Artery severed.";
                                        BleedingState = BleedingStates.DEADLY;
                                        WoundState = WoundStates.MEDIUM;
                                        break;
                                }
    
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor -= ManagerScript.ArmorDamage;
                            }
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            int situation = rand.Next(0, 7);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze cut wound. " +
                                                    "Light bleeding detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow cut. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 2:
                                    situationText = "Deep stab wound. Serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Shallow stab wound. " +
                                                    "Moderate bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "Defense wounds to fingers; Superficial damage";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                                case 6:
                                    situationText = "Arm bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                            int situation = rand.Next(0, 6);
                            string situationText = "";
                            
                            switch (situation)
                            {
                                case 0:
                                    situationText = "Graze cut wound. " +
                                                    "Light bleeding detected";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    break;
                                case 1:
                                    situationText = "Long shallow cut. " +
                                                    "Superficial damage and some bleeding";
                                    BleedingState = BleedingStates.LIGHT;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 2:
                                    situationText = "Deep stab wound. Serious bleeding";
                                    BleedingState = BleedingStates.HEAVY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 3:
                                    situationText = "Shallow stab wound. " +
                                                    "Moderate bleeding";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.LIGHT;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 4:
                                    situationText = "~o~Artery severed";
                                    BleedingState = BleedingStates.DEADLY;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                                case 5:
                                    situationText = "Leg bone was broken";
                                    BleedingState = BleedingStates.MEDIUM;
                                    WoundState = WoundStates.MEDIUM;
                                    AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                    break;
                            }

                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.EXPLOSIVE:
                {
                    switch (bone)
                    {
                        default:
                            return;
                        case BodyParts.NOTHING:
                            return;
                        case BodyParts.HEAD:
                        {
                            string situationText = "Your head was blown";
                            BleedingState = BleedingStates.DEADLY;
                            WoundState = WoundStates.DEADLY;
                            TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                
                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);

                            return;
                        }
                        case BodyParts.NECK:
                        {
                            string situationText = "Your head was fly away";
                            BleedingState = BleedingStates.DEADLY;
                            WoundState = WoundStates.DEADLY;
                            TargetPed.Health -= ManagerScript.AdditionalHeadshotDamage;
                                
                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            
                            return;
                        }
                        case BodyParts.UPPER_BODY:
                        {
                            if (CurrentArmor * 2 < ManagerScript.MaxArmor)
                            {
                                string situationText = "Your chest was blown";
                                BleedingState = BleedingStates.DEADLY;
                                WoundState = WoundStates.DEADLY;
                                AddDamageFlag(DamageTypes.HEART_DAMAGED);
                                AddDamageFlag(DamageTypes.LUNGS_DAMAGED);
                                
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor = 0;
                            }
                            
                            return;
                        }
                        case BodyParts.LOWER_BODY:
                        {
                            if (CurrentArmor * 2 < ManagerScript.MaxArmor)
                            {
                                string situationText = "Your stomach was blown";
                                BleedingState = BleedingStates.DEADLY;
                                WoundState = WoundStates.DEADLY;
                                AddDamageFlag(DamageTypes.STOMACH_DAMAGED);
                                AddDamageFlag(DamageTypes.GUTS_DAMAGED);
                                
                                if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                    UI.Notify(situationText);
                            }
                            else
                            {
                                CurrentArmor = 0;
                            }
                            
                            return;
                        }
                        case BodyParts.ARM:
                        {
                            string situationText = "Arm was blown";
                            BleedingState = BleedingStates.DEADLY;
                            WoundState = WoundStates.HEAVY;
                            AddDamageFlag(DamageTypes.ARMS_DAMAGED);
                                
                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                        case BodyParts.LEG:
                        {
                            string situationText = "Leg was blown";
                            BleedingState = BleedingStates.DEADLY;
                            WoundState = WoundStates.HEAVY;
                            AddDamageFlag(DamageTypes.LEGS_DAMAGED);
                                
                            if(ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        }
                    }
                }
                case WeaponClasses.FIRE:
                {
                    switch (bone)
                    {
                        default:
                            string situationText = "You are burning";
                            WoundState = WoundStates.MEDIUM;
                            onFireTimer = 10;

                            if (ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        case BodyParts.NOTHING:
                            return;
                    }
                }
                case WeaponClasses.SUFFOCATING:
                {
                    switch (bone)
                    {
                        default:
                            string situationText = "You are choking";
                            WoundState = WoundStates.LIGHT;
                            chockingTimer = 10;

                            if (ManagerScript.DebugMode || IsPlayer && ManagerScript.TimeToRefreshNotifications > 0)
                                UI.Notify(situationText);
                            return;
                        case BodyParts.NOTHING:
                            return;
                    }
                }
            }
        }

        private void AddDamageFlag(DamageTypes newDamage)
        {
            switch (newDamage)
            {
                case DamageTypes.ARMS_DAMAGED:
                    damages = damages | DamageTypes.ARMS_DAMAGED;
                
                    if (IsPlayer)
                    {
                        Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                    }
                    else
                    {
                        if (ManagerScript.EnemyNotificationsEnabled && !IsPlayer)
                        {
                            UI.Notify(string.Format("{0} arm looks very bad", HeShe == "He" ? "His" : "Her"));
                        }
                    }
                    return;
                
                case DamageTypes.LEGS_DAMAGED:
                    damages = damages | DamageTypes.LEGS_DAMAGED;
                
                    if (IsPlayer)
                    {
                    
                    }
                    else
                    {
                        if (ManagerScript.EnemyNotificationsEnabled && !IsPlayer)
                        {
                            UI.Notify(string.Format("{0} leg looks very bad", HeShe == "He" ? "His" : "Her"));
                        }
                    }
                    return;
                
                case DamageTypes.HEART_DAMAGED:
                    damages = damages | DamageTypes.HEART_DAMAGED;
                
                    if (IsPlayer)
                    {
                    
                    }
                    else
                    {
                        if (ManagerScript.EnemyNotificationsEnabled)
                        {
                            UI.Notify($"{HeShe} coughs up blood");
                        }
                    
                        TargetPed.Task.StandStill((int)ManagerScript.TimeToDeath/8);
                    }
                    
                    return;
                case DamageTypes.LUNGS_DAMAGED:
                    damages = damages | DamageTypes.LUNGS_DAMAGED;
                
                    if (IsPlayer)
                    {
                    
                    }
                    else
                    {
                        if (ManagerScript.EnemyNotificationsEnabled)
                        {
                            UI.Notify($"{HeShe} coughs up blood");
                        }
                    }
                    return;
                
                case DamageTypes.STOMACH_DAMAGED:
                    damages = damages | DamageTypes.STOMACH_DAMAGED;
                
                    if (IsPlayer)
                    {
                    
                    }
                    else
                    {
                        if (ManagerScript.EnemyNotificationsEnabled)
                        {
                            UI.Notify($"{HeShe} looks very sick");
                        }
                    }
                    return;
                
                case DamageTypes.GUTS_DAMAGED:
                    damages = damages | DamageTypes.GUTS_DAMAGED;
                
                    if (IsPlayer)
                    {
                    
                    }
                    else
                    {
                        if (ManagerScript.EnemyNotificationsEnabled)
                        {
                            UI.Notify($"{HeShe} looks very sick");
                        }
                    }
                    return;
                
                case DamageTypes.NERVES_DAMAGED:
                    damages = damages | DamageTypes.NERVES_DAMAGED;
                    TargetPed.Health -= ManagerScript.AdditionalDamageOnNervesDamage;
                
                    if (IsPlayer)
                    {
                        Function.Call(Hash.SET_FLASH, 0, 0, 100, 500, 100);
                    }
                    return;
                default:
                    break;
            }
        }
    }
}