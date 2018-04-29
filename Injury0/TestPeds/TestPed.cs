using GTA;

namespace GunshotWound.TestPeds
{
    public class TestPed : IEnhancedPed
    {
        public GunshotWound ManagerScript { get; set; }
        public Ped TargetPed { get; set; }
        public bool IsPlayer { get; set; }

        private int ticks;
        
        public void Update()
        {
            if (ManagerScript.DebugMode)
            {
                ticks++;
            }
        }

        public void ShowNotifications()
        {
            if (ManagerScript.DebugMode)
            {
                UI.Notify("~g~Te~y~st ~o~no~r~ti~g~fi~y~ca~o~ti~r~on");
            }
        }

        public string GetSubtitlesInfo()
        {
            string subtitles = "";
            
            if (ManagerScript.DebugMode)
            {
                subtitles += $"Ticks {ticks}";
            }

            return subtitles;
        }

        public void RestoreDefaultState()
        {
            ticks = 0;
        }
    }
}