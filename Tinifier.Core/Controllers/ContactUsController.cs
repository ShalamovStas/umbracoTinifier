using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Tinifier.Core.Controllers;
using Tinifier.Core.Infrastructure;
using Tinifier.Core.Infrastructure.Enums;
using Tinifier.Core.Models;
using Tinifier.Core.Models.Db;
using Tinifier.Core.Models.Services;
using Tinifier.Core.Repository.History;
using Tinifier.Core.Repository.Image;
using Tinifier.Core.Repository.State;
using Tinifier.Core.Services.EmailSender;
using Tinifier.Core.Services.Validation;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Web.WebApi;

namespace Umbraco8.Components
{


    public class ContactUsController : UmbracoAuthorizedApiController
    {
        IReportSender _reportSender;

        public ContactUsController(IReportSender reportSender)
        {
            _reportSender = reportSender;
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Report([FromBody] ReportModel model)
        {
            if(model == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Declined");

            var result = await _reportSender.SendReportAsync(model);
           
            return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        [HttpGet]
        public HttpResponseMessage GetMessage()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Hello from backend");
        }
    }
}

