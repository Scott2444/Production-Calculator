namespace ProductionCalculator.Business.Helpers
{
    public static class TruncateHelper
    {
        public static string? TruncateStringNullable(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= maxLength)
            {
                return value;
            }
            else
            {
                return value.Substring(0, maxLength);
            }
        }
        public static string TruncateString(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }
            else
            {
                return value.Substring(0, maxLength);
            }
        }
    }
}