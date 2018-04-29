namespace GunshotWound.TestPeds
{
    public class TestPedFactory : IEnchancedPedFactory
    {
        public IEnhancedPed Build(PedConfig config)
        {
            return new TestPed
            {
                ManagerScript = config.ManagerScript,
                TargetPed = config.TargetPed,
                IsPlayer = config.IsPlayer
            };
        }
    }
}