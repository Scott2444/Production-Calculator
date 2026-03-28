using System.Security.Cryptography;
using Resend;

namespace ProductionCalculator.Business.Helpers
{
    public class VerificationCodeHelper
    {
        public static (string code, string codeHash) GenerateCode()
        {
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            return (code, BCrypt.Net.BCrypt.HashPassword(code));
        }
        public static bool VerifyCode(string code, string codeHash)
        {
            return BCrypt.Net.BCrypt.Verify(code, codeHash);
        }
        public static EmailMessage GenerateEmail(string email,string code, string expirationMinutes)
        {
            var body = $"""
            <div style="font-family:Segoe UI, Arial, sans-serif; max-width:480px; margin:0 auto; padding:32px 24px; background:#ffffff; border-radius:12px; box-shadow:0 4px 12px rgba(0,0,0,0.05); color:#1f2937;">
                
                <h2 style="margin:0 0 12px 0; font-size:1.4rem; font-weight:600; color:#432dd7;">
                    Verify your email address
                </h2>

                <p style="margin:0 0 24px 0; font-size:1rem; line-height:1.5; color:#374151;">
                    Use the verification code below to complete your sign-in to <strong>Production Calculator</strong>.
                </p>

                <div style="font-size:2.1rem; font-weight:700; letter-spacing:0.2em; background:#f9fafb; border:1px solid #e5e7eb; border-radius:8px; padding:18px 0; text-align:center; margin-bottom:24px; color:#111827;">
                    {code}
                </div>

                <p style="margin:0 0 8px 0; font-size:0.95rem; color:#374151;">
                    This code will expire in <strong>{expirationMinutes} minutes</strong>.
                </p>

                <p style="margin:0 0 24px 0; font-size:0.95rem; color:#6b7280;">
                    If you did not request this verification, you can safely ignore this email.
                </p>

                <hr style="border:none; border-top:1px solid #e5e7eb; margin:24px 0;" />

                <p style="margin:0; font-size:0.85rem; color:#9ca3af; text-align:center;">
                    © {DateTime.UtcNow.Year} Production Calculator<br />
                    This is an automated message. Please do not reply.
                </p>

            </div>
            """;

            var message = new EmailMessage();
            message.From = "Production Calculator Verification <noreply@production-calculator.com>";
            message.To.Add(email);
            message.Subject = "Please verify your Production Calculator account";
            message.HtmlBody = body;
            return message;
        }
    }
}
