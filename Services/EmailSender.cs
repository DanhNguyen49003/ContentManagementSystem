using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentManagementSystem.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderName { get; set; } = "CMS Portal";
        public string SenderEmail { get; set; } = "noreply@cmsportal.com";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableSsl { get; set; } = true;
    }

    public static class EmailTemplateHelper
    {
        public static string GenerateConfirmationEmail(string confirmationUrl, string recipientName = "")
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Xác nhận tài khoản CMS Portal</title>
    <style>
        body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #f8fafc; padding: 40px 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #2563eb 0%, #4f46e5 100%); padding: 36px 32px; text-align: center; }}
        .header h1 {{ margin: 0; color: #ffffff; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }}
        .header p {{ margin: 6px 0 0 0; color: #bfdbfe; font-size: 13px; }}
        .content {{ padding: 36px 32px; }}
        .greeting {{ font-size: 16px; font-weight: 700; color: #1e293b; margin-bottom: 16px; }}
        .message {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 28px; }}
        .btn-wrapper {{ text-align: center; margin: 32px 0; }}
        .btn {{ display: inline-block; padding: 14px 32px; background-color: #2563eb; color: #ffffff !important; text-decoration: none; border-radius: 10px; font-size: 14px; font-weight: 700; box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3); }}
        .fallback-link {{ background-color: #f1f5f9; padding: 16px; border-radius: 8px; font-size: 12px; color: #64748b; word-break: break-all; line-height: 1.5; margin-bottom: 24px; }}
        .notice {{ font-size: 12px; color: #94a3b8; line-height: 1.5; border-top: 1px solid #f1f5f9; padding-top: 20px; }}
        .footer {{ background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <div class=""header"">
                <h1>CMS Portal</h1>
                <p>Hệ thống Quản trị Nội dung Toàn diện</p>
            </div>
            <div class=""content"">
                <div class=""greeting"">Xin chào {(string.IsNullOrEmpty(recipientName) ? "bạn" : recipientName)}, 👋</div>
                <div class=""message"">
                    Cảm ơn bạn đã đăng ký tài khoản tại <strong>CMS Portal</strong>. Để kích hoạt và bảo mật tài khoản của bạn, vui lòng bấm vào nút xác thực bên dưới:
                </div>
                <div class=""btn-wrapper"">
                    <a href=""{confirmationUrl}"" class=""btn"" target=""_blank"">Xác nhận tài khoản ngay &rarr;</a>
                </div>
                <div class=""message"" style=""font-size: 13px; margin-bottom: 8px;"">
                    Nếu nút trên không hoạt động, bạn có thể sao chép và dán liên kết sau vào trình duyệt:
                </div>
                <div class=""fallback-link"">
                    {confirmationUrl}
                </div>
                <div class=""notice"">
                    * Lưu ý: Nếu bạn không đăng ký tài khoản tại hệ thống của chúng tôi, bạn có thể hoàn toàn an tâm bỏ qua email này.
                </div>
            </div>
            <div class=""footer"">
                &copy; {DateTime.Now.Year} CMS Portal. Mọi quyền được bảo lưu.
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public static string GenerateResetPasswordEmail(string resetUrl, string recipientName = "")
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Yêu cầu đặt lại mật khẩu - CMS Portal</title>
    <style>
        body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #f8fafc; padding: 40px 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #f59e0b 0%, #ea580c 100%); padding: 36px 32px; text-align: center; }}
        .header h1 {{ margin: 0; color: #ffffff; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }}
        .header p {{ margin: 6px 0 0 0; color: #fef3c7; font-size: 13px; }}
        .content {{ padding: 36px 32px; }}
        .greeting {{ font-size: 16px; font-weight: 700; color: #1e293b; margin-bottom: 16px; }}
        .message {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 28px; }}
        .btn-wrapper {{ text-align: center; margin: 32px 0; }}
        .btn {{ display: inline-block; padding: 14px 32px; background-color: #ea580c; color: #ffffff !important; text-decoration: none; border-radius: 10px; font-size: 14px; font-weight: 700; box-shadow: 0 4px 12px rgba(234, 88, 12, 0.3); }}
        .fallback-link {{ background-color: #f1f5f9; padding: 16px; border-radius: 8px; font-size: 12px; color: #64748b; word-break: break-all; line-height: 1.5; margin-bottom: 24px; }}
        .notice {{ font-size: 12px; color: #94a3b8; line-height: 1.5; border-top: 1px solid #f1f5f9; padding-top: 20px; }}
        .footer {{ background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <div class=""header"">
                <h1>CMS Portal</h1>
                <p>Khôi phục mật khẩu tài khoản</p>
            </div>
            <div class=""content"">
                <div class=""greeting"">Xin chào {(string.IsNullOrEmpty(recipientName) ? "bạn" : recipientName)}, 🔐</div>
                <div class=""message"">
                    Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn tại <strong>CMS Portal</strong>. Vui lòng bấm vào nút bên dưới để tiến hành tạo mật khẩu mới:
                </div>
                <div class=""btn-wrapper"">
                    <a href=""{resetUrl}"" class=""btn"" target=""_blank"">Đặt lại mật khẩu &rarr;</a>
                </div>
                <div class=""message"" style=""font-size: 13px; margin-bottom: 8px;"">
                    Nếu nút trên không thể bấm được, vui lòng mở liên kết sau trong trình duyệt:
                </div>
                <div class=""fallback-link"">
                    {resetUrl}
                </div>
                <div class=""notice"">
                    * Vì lý do an toàn, liên kết này sẽ tự động hết hạn sau một khoảng thời gian nhất định. Nếu bạn không yêu cầu đặt lại mật khẩu, xin vui lòng bỏ qua email này.
                </div>
            </div>
            <div class=""footer"">
                &copy; {DateTime.Now.Year} CMS Portal. Mọi quyền được bảo lưu.
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> options, ILogger<EmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("Đang gửi email tới: {Email} | Tiêu đề: {Subject}", email, subject);

            // If username & password are not configured yet, log the email content clearly for development/testing
            if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogWarning("EmailSettings (Username/Password) chưa được cấu hình SMTP. Email được ghi log thử nghiệm thành công cho {Email}.", email);
                return;
            }

            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = _settings.EnableSsl
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Gửi email thành công tới {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email qua SMTP tới {Email}: {Message}", email, ex.Message);
            }
        }
    }
}

