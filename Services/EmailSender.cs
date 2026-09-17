using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace ContentManagementSystem.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp-relay.brevo.com";
        public int SmtpPort { get; set; } = 587;
        public int Port { get => SmtpPort; set => SmtpPort = value; }
        public string SenderName { get; set; } = "CMS Portal";
        public string SenderEmail { get; set; } = "danh49003@gmail.com";
        public string Username { get; set; } = "danh49003@gmail.com";
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
</body>
</html>";
        }

        public static string GenerateNewPostApprovalEmail(string postTitle, string authorName, string categoryName, string postSnippet, string reviewUrl)
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Bài viết mới cần duyệt - CMS Portal</title>
    <style>
        body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #f8fafc; padding: 40px 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #7c3aed 0%, #2563eb 100%); padding: 36px 32px; text-align: center; }}
        .header h1 {{ margin: 0; color: #ffffff; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }}
        .header p {{ margin: 6px 0 0 0; color: #e9d5ff; font-size: 13px; }}
        .content {{ padding: 36px 32px; }}
        .greeting {{ font-size: 16px; font-weight: 700; color: #1e293b; margin-bottom: 16px; }}
        .message {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 20px; }}
        .card {{ background-color: #f8fafc; border-left: 4px solid #7c3aed; padding: 18px 20px; border-radius: 8px; margin-bottom: 24px; }}
        .card h3 {{ margin: 0 0 8px 0; color: #1e293b; font-size: 16px; font-weight: 700; }}
        .card p {{ margin: 4px 0; font-size: 13px; color: #64748b; }}
        .btn-wrapper {{ text-align: center; margin: 32px 0; }}
        .btn {{ display: inline-block; padding: 14px 32px; background-color: #7c3aed; color: #ffffff !important; text-decoration: none; border-radius: 10px; font-size: 14px; font-weight: 700; box-shadow: 0 4px 12px rgba(124, 58, 237, 0.3); }}
        .footer {{ background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <div class=""header"">
                <h1>CMS Portal</h1>
                <p>Thông Báo Kiểm Duyệt Bài Viết Mới</p>
            </div>
            <div class=""content"">
                <div class=""greeting"">Kính gửi Ban Quản Trị & Ban Kiểm Duyệt QA, 📋</div>
                <div class=""message"">
                    Hệ thống vừa tiếp nhận một bài viết mới cần được xem xét và phê duyệt trước khi xuất bản rộng rãi:
                </div>
                <div class=""card"">
                    <h3>{postTitle}</h3>
                    <p><strong>Tác giả:</strong> {(string.IsNullOrEmpty(authorName) ? "Thành viên hệ thống" : authorName)}</p>
                    <p><strong>Chuyên mục:</strong> {(string.IsNullOrEmpty(categoryName) ? "Chung" : categoryName)}</p>
                    <p><strong>Thời gian gửi:</strong> {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}</p>
                    {(string.IsNullOrEmpty(postSnippet) ? "" : $"<p style=\"margin-top: 8px; color: #475569; font-style: italic;\">\"{postSnippet}\"</p>")}
                </div>
                <div class=""btn-wrapper"">
                    <a href=""{reviewUrl}"" class=""btn"" target=""_blank"">Duyệt bài viết ngay &rarr;</a>
                </div>
            </div>
            <div class=""footer"">
                &copy; {DateTime.Now.Year} CMS Portal - Ban Quản Trị & Ban Kiểm Duyệt QA.
            </div>
        </div>
    </div>
</body>
</html>";
        }

        public static string GenerateOtpVerificationEmail(string otpCode, string recipientName = "", int expirationMinutes = 5)
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mã xác thực OTP - CMS Portal</title>
    <style>
        body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #f8fafc; padding: 40px 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%); padding: 36px 32px; text-align: center; }}
        .header h1 {{ margin: 0; color: #ffffff; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }}
        .header p {{ margin: 6px 0 0 0; color: #e0e7ff; font-size: 13px; }}
        .content {{ padding: 36px 32px; }}
        .greeting {{ font-size: 16px; font-weight: 700; color: #1e293b; margin-bottom: 16px; }}
        .message {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 24px; }}
        .otp-box-wrapper {{ text-align: center; margin: 28px 0; }}
        .otp-box {{ display: inline-block; padding: 16px 36px; background-color: #f8fafc; border: 2px dashed #6366f1; border-radius: 14px; font-size: 36px; font-weight: 800; color: #4f46e5; letter-spacing: 12px; font-family: 'Courier New', Courier, monospace; }}
        .expiry-badge {{ display: inline-block; margin-top: 14px; padding: 6px 16px; background-color: #fef2f2; color: #dc2626; border-radius: 9999px; font-size: 12px; font-weight: 600; border: 1px solid #fecaca; }}
        .notice {{ font-size: 12px; color: #94a3b8; line-height: 1.5; border-top: 1px solid #f1f5f9; padding-top: 20px; margin-top: 24px; }}
        .footer {{ background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <div class=""header"">
                <h1>CMS Portal</h1>
                <p>Xác thực tài khoản người dùng</p>
            </div>
            <div class=""content"">
                <div class=""greeting"">Xin chào {(string.IsNullOrEmpty(recipientName) ? "bạn" : recipientName)}, 👋</div>
                <div class=""message"">
                    Cảm ơn bạn đã đăng ký tài khoản tại <strong>CMS Portal</strong>. Để kích hoạt tài khoản của bạn, vui lòng nhập mã xác thực OTP 6 chữ số dưới đây vào trang đăng ký:
                </div>
                <div class=""otp-box-wrapper"">
                    <div class=""otp-box"">{otpCode}</div>
                    <br />
                    <span class=""expiry-badge"">⏱ Hiệu lực trong {expirationMinutes} phút</span>
                </div>
                <div class=""message"" style=""font-size: 13px; color: #64748b; text-align: center;"">
                    Vui lòng không cung cấp mã OTP này cho bất kỳ ai để đảm bảo an toàn cho tài khoản của bạn.
                </div>
                <div class=""notice"">
                    * Lưu ý: Nếu bạn không thực hiện yêu cầu đăng ký tại CMS Portal, bạn có thể hoàn toàn yên tâm bỏ qua email này.
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

        public static string GeneratePostApprovedEmail(string postTitle, string authorName = "", string postUrl = "")
        {
            return $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Bài viết đã được duyệt - CMS Portal</title>
    <style>
        body {{ margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; color: #334155; }}
        .wrapper {{ width: 100%; table-layout: fixed; background-color: #f8fafc; padding: 40px 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }}
        .header {{ background: linear-gradient(135deg, #059669 0%, #10b981 100%); padding: 36px 32px; text-align: center; }}
        .header h1 {{ margin: 0; color: #ffffff; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }}
        .header p {{ margin: 6px 0 0 0; color: #d1fae5; font-size: 13px; }}
        .content {{ padding: 36px 32px; }}
        .greeting {{ font-size: 16px; font-weight: 700; color: #1e293b; margin-bottom: 16px; }}
        .message {{ font-size: 14px; line-height: 1.6; color: #475569; margin-bottom: 24px; }}
        .post-card {{ background-color: #f0fdf4; border-left: 4px solid #10b981; padding: 18px 20px; border-radius: 8px; margin-bottom: 24px; }}
        .post-card h3 {{ margin: 0 0 6px 0; color: #065f46; font-size: 16px; font-weight: 700; }}
        .post-card p {{ margin: 0; font-size: 13px; color: #047857; }}
        .btn-wrapper {{ text-align: center; margin: 28px 0; }}
        .btn {{ display: inline-block; padding: 14px 32px; background-color: #059669; color: #ffffff !important; text-decoration: none; border-radius: 10px; font-size: 14px; font-weight: 700; box-shadow: 0 4px 12px rgba(5, 150, 105, 0.3); }}
        .footer {{ background-color: #f8fafc; padding: 24px 32px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <div class=""header"">
                <h1>CMS Portal</h1>
                <p>Thông Báo Phê Duyệt Bài Viết</p>
            </div>
            <div class=""content"">
                <div class=""greeting"">Xin chúc mừng {(string.IsNullOrEmpty(authorName) ? "bạn" : authorName)}! 🎉</div>
                <div class=""message"">
                    Bài viết của bạn đã được <strong>Admin / QA Coordinator</strong> kiểm duyệt thành công và đã được xuất bản công khai trên hệ thống <strong>CMS Portal</strong>:
                </div>
                <div class=""post-card"">
                    <h3>{postTitle}</h3>
                    <p>Trạng thái: <strong>Đã xuất bản (Công khai)</strong></p>
                </div>
                <div class=""btn-wrapper"">
                    <a href=""{postUrl}"" class=""btn"" target=""_blank"">Xem bài viết ngay &rarr;</a>
                </div>
            </div>
            <div class=""footer"">
                &copy; {DateTime.Now.Year} CMS Portal. Chúc bạn có những trải nghiệm sáng tạo nội dung tuyệt vời!
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

            // Nếu mật khẩu/key chưa được cấu hình thì ghi log lại nội dung
            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogWarning("EmailSettings (Password/Key) chưa được cấu hình. Nội dung email được ghi log cho {Email}.", email);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlMessage
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.Timeout = 10000; // 10 giây timeout tránh treo luồng web
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Brevo port 587 uses STARTTLS, port 465 uses SSL
                var secureSocketOption = _settings.SmtpPort == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : (_settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

                await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, secureSocketOption);

                if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password))
                {
                    await client.AuthenticateAsync(_settings.Username, _settings.Password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Gửi email thành công tới {Email} qua {Server}:{Port}", email, _settings.SmtpServer, _settings.SmtpPort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, @"
=======================================================================
❌ [EmailSender] LỖI GỬI EMAIL TỚI: {Email}
Mã lỗi: {Message}
Máy chủ SMTP: {Server}:{Port} | Tài khoản: {Username}
-----------------------------------------------------------------------
💡 NGUYÊN NHÂN & CÁCH KHẮC PHỤC:
1. Nếu dùng Brevo (smtp-relay.brevo.com:587):
   - Mã '535 Authentication failed' nghĩa là Login hoặc SMTP Key không khớp.
   - Hãy vào: https://app.brevo.com/settings/keys/smtp
   - Kiểm tra đúng giá trị tại ô 'Login' (thường là mã xxx@smtp-brevo.com hoặc email đăng ký).
   - Nhấn 'Generate a new SMTP key' và copy key 'xsmtpsib-...' dán vào Password trong appsettings.json.
2. Nếu dùng Gmail (Khuyên dùng - cực kỳ ổn định):
   - SmtpServer: smtp.gmail.com | SmtpPort: 587 | EnableSsl: true
   - Username: dia_chi_gmail_cua_ban@gmail.com
   - Password: Mật khẩu ứng dụng 16 ký tự (tạo tại myaccount.google.com/apppasswords)
=======================================================================",
                    email, ex.Message, _settings.SmtpServer, _settings.SmtpPort, _settings.Username);
            }
        }
    }
}

