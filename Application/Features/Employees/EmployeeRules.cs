using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Application.Features.Employees
{
    /// <summary>Pure rules for employee accounts: user names, temporary passwords and when a token stops being accepted.</summary>
    public static partial class EmployeeRules
    {
        public const int TemporaryPasswordLength = 14;

        [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]{2,49}$")]
        private static partial Regex UserNamePattern();

        public static string NormalizeUserName(string? userName) => (userName ?? string.Empty).Trim().ToLowerInvariant();

        public static bool IsValidUserName(string userName) => UserNamePattern().IsMatch(userName);

        // No 0/O, 1/l/I: a temporary password is read out or copied by hand.
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnpqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Symbols = "!@#$%&*?";

        /// <summary>
        /// A random password that meets the password policy (upper, lower, digit and symbol), drawn from a
        /// cryptographic generator. It is shown once and must be changed at the first sign-in.
        /// </summary>
        public static string GenerateTemporaryPassword()
        {
            string all = Upper + Lower + Digits + Symbols;
            char[] password = new char[TemporaryPasswordLength];

            password[0] = Pick(Upper);
            password[1] = Pick(Lower);
            password[2] = Pick(Digits);
            password[3] = Pick(Symbols);
            for (int i = 4; i < password.Length; i++) password[i] = Pick(all);

            RandomNumberGenerator.Shuffle(password.AsSpan());
            return new string(password);
        }

        private static char Pick(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];

        /// <summary>
        /// The instant from which tokens are accepted again. Token times have one-second precision, so this is the
        /// next whole second: every token issued at or before the revocation is refused, including one issued in
        /// the same second, and a sign-in a moment later is accepted.
        /// </summary>
        public static DateTime TokensValidFrom(DateTime utcNow) =>
            new(utcNow.Ticks - utcNow.Ticks % TimeSpan.TicksPerSecond + TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        /// <summary>
        /// A signed token is only as good as the account behind it: refuse it when the employee was deactivated or
        /// removed, or when it was issued before the last deactivation, role change or password reset.
        /// </summary>
        public static bool IsTokenValid(bool isActive, bool isDeleted, DateTime? tokensValidFrom, long? issuedAtUnixSeconds)
        {
            if (!isActive || isDeleted) return false;
            if (tokensValidFrom is null) return true;
            if (issuedAtUnixSeconds is null) return false;

            long validFrom = new DateTimeOffset(DateTime.SpecifyKind(tokensValidFrom.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
            return issuedAtUnixSeconds.Value >= validFrom;
        }
    }
}
