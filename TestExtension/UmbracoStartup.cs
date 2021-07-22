using Newtonsoft.Json.Linq;
using NPoco.fastJSON;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Xml;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Packaging;
using Umbraco.Core.Models.Sections;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;
using Umbraco.Web.Composing.CompositionExtensions;
using Umbraco.Web.JavaScript;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Services;
using Umbraco.Web.Trees;

namespace Tinifier.Core.Application
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    class tinifierStartup : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<SectionService>();

        }
    }

    public class SectionService : IComponent
    {
        ISectionService _sectionService;
        string SectionName = "TestExtension";

        public SectionService(ISectionService sectionService)
        {
            _sectionService = sectionService;
        }

        public void Initialize()
        {

            PackagingService.UninstalledPackage += PackagingService_UninstalledPackage;

        }

        public void Terminate()
        {

        }

        private void PackagingService_UninstalledPackage(IPackagingService sender, UninstallPackageEventArgs e)
        {
            var pack = e.UninstallationSummary.FirstOrDefault();
            if (pack == null)
                return;

            if (pack.MetaData.Name == SectionName)
            {
                try
                {
                    var directory = new DirectoryInfo(System.Web.HttpContext.Current.Server.MapPath("~/App_Plugins/" + SectionName));
                    if (directory != null && directory.Exists)
                        directory.Delete(true);
                }
                catch (Exception ex)
                { }
            }
        }

    }
}

