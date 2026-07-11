using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace BusManagement.Service.Services.Implementations;

public class TmMasterMailService : BaseService<TM_MASTER_MAIL, int, TM_MASTER_MAILDTO>, ITmMasterMailService
{
    private readonly ITmMasterMailRepository _repo;
    private readonly IConfiguration _configuration;
    private readonly string _agentConnectionString;

    public TmMasterMailService(ITmMasterMailRepository repository, IMapper mapper, IConfiguration configuration) : base(repository, mapper)
    {
        _repo = repository;
        _configuration = configuration;
        _agentConnectionString = _configuration["ConnectionStrings:AgentConnection"] ?? "Server=APBIVNDB14;Database=AGENTDB;User Id=agent;Password=agent;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=300;Max Pool Size=200;Min Pool Size=10;";
    }

    public bool sendEmailNotify(string title, string mail_from, string mail_to, string mail_cc, string mail_bcc, string body, int priority)
    {
        bool blresult = true;

        try
        {
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(mail_from);
            string[] arrEmail = mail_to.Split(Convert.ToChar(";"));
            if (arrEmail?.Length > 0)
            {
                foreach (string item_to in arrEmail)
                {
                    if (!string.IsNullOrEmpty(item_to?.Trim()))
                        msg.To.Add(new MailAddress(item_to));
                }

            }

            if (mail_cc?.Length > 0)
            {
                arrEmail = mail_cc.Split(Convert.ToChar(";"));
                if (arrEmail.Length > 0)
                {
                    foreach (string item_cc in arrEmail)
                    {
                        if (!string.IsNullOrEmpty(item_cc?.Trim()))
                            msg.CC.Add(new MailAddress(item_cc));
                    }
                }

            }

            if (mail_bcc?.Length > 0)
            {
                arrEmail = mail_bcc.Split(Convert.ToChar(";"));
            }
            if (arrEmail?.Length > 0)
            {
                foreach (string item_bcc in arrEmail)
                {
                    if (!string.IsNullOrEmpty(item_bcc?.Trim()))
                        msg.Bcc.Add(new MailAddress(item_bcc));
                }
            }

            if (priority == 1)
            { msg.Priority = MailPriority.High; }

            msg.Subject = title;
            msg.Body = body;
            //String.Format(body, 1, 2);
            msg.IsBodyHtml = true;

            string password = DecryptAES();
            SmtpClient emailClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"] ?? "smtp-auth.brothergroup.net", int.Parse(_configuration["EmailSettings:SmtpPort" ?? "25"]));
            emailClient.Credentials = new NetworkCredential(_configuration["EmailSettings:SenderAdid"] ?? "zzpsvvmg", password);
            emailClient.EnableSsl = false;
            emailClient.Send(msg);

        }
        catch (Exception ex)
        {
            blresult = false;
        }

        return blresult;
    }
    public bool sendMailWithAttach(string title, string mail_from, string mail_to, string mail_cc, string mail_bcc, string body, string attachFile, int priority)
    {
        bool blresult = true;
        try
        {
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(mail_from);
            string[] arrEmail = mail_to.Split(Convert.ToChar(";"));
            if (arrEmail.Length > 0)
            {
                foreach (string item_to in arrEmail)
                {
                    if (!string.IsNullOrEmpty(item_to))
                    {
                        msg.To.Add(new MailAddress(item_to));
                    }
                }
            }
            if (mail_cc != null)
            {
                arrEmail = mail_cc.Split(Convert.ToChar(";"));
                if (arrEmail.Length > 0)
                {
                    foreach (string item_cc in arrEmail)
                    {
                        if (!string.IsNullOrEmpty(item_cc))
                        {
                            msg.CC.Add(new MailAddress(item_cc));
                        }
                    }
                }

            }
            if (mail_bcc?.Length > 0)
            {
                arrEmail = mail_bcc.Split(Convert.ToChar(";"));

                if (arrEmail?.Length > 0)
                {
                    foreach (string item_bcc in arrEmail)
                    {
                        if (!string.IsNullOrEmpty(item_bcc?.Trim()))
                            msg.Bcc.Add(new MailAddress(item_bcc));
                    }
                }
            }
            if (priority == 1)
            {
                msg.Priority = MailPriority.High;
            }
            if (priority == 0)
            {
                msg.Priority = MailPriority.Normal;
            }
            if (priority == -1)
            {
                msg.Priority = MailPriority.Low;
            }
            msg.Subject = title;
            msg.Body = body;
            msg.IsBodyHtml = true;
            Attachment attachment = new Attachment(attachFile);
            msg.Attachments.Add(attachment);
            string password = DecryptAES();
            SmtpClient emailClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"] ?? "smtp-auth.brothergroup.net", int.Parse(_configuration["EmailSettings:SmtpPort" ?? "25"]));
            emailClient.Credentials = new NetworkCredential(_configuration["EmailSettings:SenderAdid"] ?? "zzpsvvmg", password);
            emailClient.EnableSsl = false;
            emailClient.Send(msg);
            attachment.Dispose();
        }
        catch (Exception ex)
        {
            blresult = false;
        }
        return blresult;
    }

    // Hàm gửi mail custom có thể đính kèm nhiều file và nhúng ảnh cid trong body html
    public async Task<GenericResponse<bool>> SendEmailNotifyCustomSendMultiAttachFile(EmailFormNetMailCustomSendMultiAttachFile emailForm)
    {
        var result = new GenericResponse<bool>();
        if (emailForm == null)
        {
            result.Success = false;
            result.Message = "Không nhận được dữ liệu input!";
            return result;
        }
        try
        {
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(emailForm.mail_from);

            var toEmails = emailForm.mail_to?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            foreach (string item_to in toEmails)
            {
                if (!string.IsNullOrWhiteSpace(item_to))
                    msg.To.Add(new MailAddress(item_to.Trim()));
            }

            if (emailForm.mail_cc != null && emailForm.mail_cc.Length > 0)
            {
                var ccEmails = emailForm.mail_cc.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item_cc in ccEmails)
                {
                    if (!string.IsNullOrWhiteSpace(item_cc))
                        msg.CC.Add(new MailAddress(item_cc.Trim()));
                }
            }

            if (!string.IsNullOrEmpty(emailForm.mail_bcc))
            {
                var bccEmails = emailForm.mail_bcc.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item_bcc in bccEmails)
                {
                    if (!string.IsNullOrWhiteSpace(item_bcc))
                        msg.Bcc.Add(new MailAddress(item_bcc.Trim()));
                }
            }

            if (emailForm.attachmentPaths != null && emailForm.attachmentPaths.Any())
            {
                foreach (var path in emailForm.attachmentPaths)
                {
                    if (!string.IsNullOrWhiteSpace(path))
                        msg.Attachments.Add(new Attachment(path));
                }
            }

            msg.Subject = emailForm.title;

            // === Thay đổi chính: dùng AlternateView để nhúng ảnh cid thay vì gán msg.Body trực tiếp ===
            if (emailForm.embeddedImages != null && emailForm.embeddedImages.Any())
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(emailForm.body, null, "text/html");

                foreach (var img in emailForm.embeddedImages)
                {
                    if (string.IsNullOrWhiteSpace(img.FilePath) || !File.Exists(img.FilePath))
                        continue;

                    var linkedResource = new LinkedResource(img.FilePath)
                    {
                        ContentId = img.ContentId,
                        TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                    };
                    htmlView.LinkedResources.Add(linkedResource);
                }

                msg.AlternateViews.Add(htmlView);
                // không set msg.Body + msg.IsBodyHtml khi đã dùng AlternateView, tránh hiển thị lặp nội dung
            }
            else
            {
                // giữ nguyên hành vi cũ khi không có ảnh nhúng
                msg.Body = emailForm.body;
                msg.IsBodyHtml = true;
            }
            using (System.Net.Mail.SmtpClient emailClient = new System.Net.Mail.SmtpClient(_configuration["EmailSettings:SmtpServer"] ?? "smtp-auth.brothergroup.net", int.Parse(_configuration["EmailSettings:SmtpPort" ?? "25"])))
            {
                string password = DecryptAES();
                emailClient.Credentials = new NetworkCredential(_configuration["EmailSettings:SenderAdid"] ?? "zzpsvvmg", password);
                emailClient.EnableSsl = false;
                await emailClient.SendMailAsync(msg);
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Message:" + ex.Message + "\nStackTrace:" + ex.StackTrace;
        }
        return result;
    }

    private string DecryptAES() //Lệnh giải mã
    {
        //gọi chuỗi kết nối AgentDB
        System.Data.DataTable dt = new System.Data.DataTable();
        SqlConnection conAgent;
        string cipherText = "";
        string key = _configuration["EmailSettings:Key"] ?? "BIVNIT2071";
        conAgent = new SqlConnection(_agentConnectionString);

        if (conAgent.State == ConnectionState.Closed)
        {
            conAgent.Open();
        }
        string sql = $"SELECT VCHR_USER_PASSWORD as password  FROM [AGENTDB].[dbo].[TM_USER_CONFIRM_SERVER] WHERE VCHR_USER_ADID ='{_configuration["EmailSettings:SenderAdid"] ?? "zzpsvvmg"}'";
        using (SqlCommand command = new SqlCommand(sql, conAgent))
        {

            using (SqlDataReader reader = command.ExecuteReader())
            {
                dt.Load(reader);
            }

        }
        cipherText = dt.Rows[0]["password"].ToString();

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (MD5 md5 = MD5.Create())
            {
                byte[] keyBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key));

                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        byte[] resultBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return Encoding.UTF8.GetString(resultBytes);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Lỗi khi decrypt: " + ex.Message, ex);
        }

    }

}
