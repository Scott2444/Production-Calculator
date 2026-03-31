using System.Security.Cryptography;
using System.Text;
using Resend;

namespace ProductionCalculator.Business.Helpers
{
    public static class PasswordResetHelper
    {
        public static (string token, string tokenHash) GenerateToken(int size = 48)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(size);
            var token = Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
            return (token, HashToken(token));
        }

        public static string HashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        public static string BuildResetUrl(string frontendBaseUrl, string token)
        {
            var normalizedBaseUrl = frontendBaseUrl.TrimEnd('/');
            return $"{normalizedBaseUrl}/change-password?token={Uri.EscapeDataString(token)}";
        }

        public static EmailMessage GenerateEmail(string email, string resetUrl, int expirationMinutes)
        {
            var body = $"""
            <div style="font-family:Segoe UI, Arial, sans-serif; max-width:480px; margin:0 auto; padding:32px 24px; background:#ffffff; border-radius:12px; box-shadow:0 4px 12px rgba(0,0,0,0.05); color:#1f2937;">

                <h2 style="margin:0 0 12px 0; font-size:1.4rem; font-weight:600; color:#432dd7;">
                    Reset your password
                </h2>

                <p style="margin:0 0 24px 0; font-size:1rem; line-height:1.5; color:#374151;">
                    Use the secure link below to reset the password for your <strong>Production Calculator</strong> account.
                </p>

                <div style="font-size:1.1rem; font-weight:700; background:#f9fafb; border:1px solid #e5e7eb; border-radius:8px; padding:18px 0; text-align:center; margin-bottom:24px; color:#111827;">
                    <a href="{resetUrl}" style="display:inline-block; width:100%; color:#111827; text-decoration:none;">
                        Reset your password
                    </a>
                </div>

                <p style="margin:0 0 8px 0; font-size:0.95rem; color:#374151;">
                    This link will expire in <strong>{expirationMinutes} minutes</strong>.
                </p>

                <p style="margin:0 0 8px 0; font-size:0.95rem; color:#6b7280;">
                    If the button does not work, copy and paste this URL into your browser:
                </p>

                <p style="margin:0 0 24px 0; font-size:0.85rem; color:#111827; word-break:break-all;">
                    {resetUrl}
                </p>

                <p style="margin:0 0 24px 0; font-size:0.95rem; color:#6b7280;">
                    If you did not request this password reset, you can safely ignore this email.
                </p>

                <hr style="border:none; border-top:1px solid #e5e7eb; margin:24px 0;" />

                <p style="margin:0; font-size:0.85rem; color:#9ca3af; text-align:center;">
                    © {DateTime.UtcNow.Year} Production Calculator<br />
                    This is an automated message. Please do not reply.
                </p>

            </div>
            """;

            var message = new EmailMessage();
            message.From = "Production Calculator <noreply@production-calculator.com>";
            message.To.Add(email);
            message.Subject = "Reset your Production Calculator password";
            message.HtmlBody = body;
            return message;
        }
    }
}