using GTA;

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
    public interface IEnhancedPed
    {
        GunshotWound ManagerScript{ get; set; }
        
        Ped TargetPed { get; set; }
        
        bool IsPlayer { get; set; }
        
        void Update();

        void ShowNotifications();

        string GetSubtitlesInfo();

        void RestoreDefaultState();
    }
}