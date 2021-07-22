using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Tinifier.Core.Infrastructure;
using Tinifier.Core.Models.Services;

namespace Tinifier.Core.Services.EmailSender
{
    public interface IReportSender
    {
        Task<string> SendReportAsync(ReportModel model);
    }
    public class ReportSender : IReportSender
    {
        public async Task<string> SendReportAsync(ReportModel model)
        {

            var client = new SendGridClient("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");
            var from = new EmailAddress(model.Email, model.Name);
            var subject = "Tinifier 2.0 Report";
            var to = new EmailAddress("shalamov.stas1992@gmail.com", "Tinifier");
            var plainTextContent = $"";
            var htmlContent = GenerateEmailBody(model);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            if (model.Files != null)
            {
                try
                {
                    foreach (var file in model.Files)
                    {
                        var res = file.Split(',')[1];
                        msg.AddAttachment("Img.png", res);
                    }
                }
                catch
                {
                    htmlContent += "Attachments missed";
                }
            }

            var response = await client.SendEmailAsync(msg);

            return response.StatusCode.ToString();
        }

        private string GenerateEmailBody(ReportModel model)
        {
            StringBuilder stringBuilder = new StringBuilder();

            var color = "#339966";

            stringBuilder.Append($"<p style=\"font-size: 25px; text-align: center\"><strong>Tinifier 2.0<br>{model.Type}</strong></p>");

            stringBuilder.Append($"<div style=\"border: 1px solid {color}; margin-bottom: 10px\">");

            stringBuilder.Append($"<div style=\"background: {color}; font-size: 25px; color: white\">");
            stringBuilder.Append($"<p style=\"margin: 0px; padding: 5px; \"><strong>Description</strong></p></div>");


            stringBuilder.Append($"<div style=\"padding: 5px; \"><p>{model.Description}</p></div>");
            stringBuilder.Append($"</div>");


            return stringBuilder.ToString();
        }

        private List<AttachmentFile> GetAttachments(List<string> fileNames)
        {
            var attachments = new List<AttachmentFile>();
            foreach (var fileName in fileNames)
            {
                byte[] dataArray = null;
                FileInfo fileInfo = new FileInfo(fileName);


                using (var fstream = new FileStream(fileName, FileMode.Open))
                {
                    // dataArray = new byte[fstream.Length];
                    // fstream.Read(dataArray, 0, dataArray.Length);
                    dataArray = SolutionExtensions.ReadFully(fstream);
                }

                //dataArray = File.ReadAllBytes(fileName);
                var attachment = new AttachmentFile() { Base64Content = Convert.ToBase64String(dataArray), FileName = fileInfo.Name };

                attachments.Add(attachment);
            }
            return attachments;
        }

        public class AttachmentFile
        {
            public string Base64Content { get; set; }
            public string FileName { get; set; }
        }


    }
}