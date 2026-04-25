namespace Application.Constants
{
    /// <summary>
    /// Standard color codes for portfolio status visualization.
    /// Colors follow a consistent UI/UX pattern across the application.
    /// </summary>
    public static class PortfolioStatusColors
    {
        public const string Active = "#2e7d32";          // Green - healthy status
        public const string Delinquent = "#d32f2f";      // Red - risk/warning status
        public const string Approved = "#ed6c02";        // Orange - pending/in-progress status
        public const string WrittenOff = "#616161";      // Gray - inactive/closed status
    }

    /// <summary>
    /// Portfolio status labels mapped to their color codes.
    /// </summary>
    public static class PortfolioStatusColorMap
    {
        public static readonly Dictionary<string, string> StatusToColor = new()
        {
            { "Active", PortfolioStatusColors.Active },
            { "Delinquent", PortfolioStatusColors.Delinquent },
            { "Approved", PortfolioStatusColors.Approved },
            { "Written Off", PortfolioStatusColors.WrittenOff },
        };
    }
}
