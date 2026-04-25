namespace Domain.Entities
{
    public class DelinquencyPolicy : AuditableEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public int DaysUntilEarlyDelinquency { get; set; } = 1;
        public decimal EarlyDelinquencyPenalty { get; set; } = 0.05m;

        public int DaysUntilSevereDelinquency { get; set; } = 30;
        public decimal SevereDelinquencyPenalty { get; set; } = 0.15m;

        public int DaysUntilWriteOff { get; set; } = 90;

        public bool AutomaticallyChargeLateFees { get; set; } = true;
        public decimal DailyPenaltyRate { get; set; } = 0.001m;
    }
}
