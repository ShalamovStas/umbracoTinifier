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
using Tinifier.Core.Infrastructure;
using Tinifier.Core.Infrastructure.Exceptions;
using Tinifier.Core.Models.Db;
using Tinifier.Core.Repository.Common;
using Tinifier.Core.Repository.FileSystemProvider;
using Tinifier.Core.Repository.History;
using Tinifier.Core.Services;
using Tinifier.Core.Services.History;
using Tinifier.Core.Services.ImageCropperInfo;
using Tinifier.Core.Services.Media;
using Tinifier.Core.Services.Settings;
using Tinifier.Core.Services.Statistic;
using Tinifier.Core.Services.Validation;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Packaging;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Web;
using Umbraco.Web.Composing.CompositionExtensions;
using Umbraco.Web.JavaScript;
using Umbraco.Web.Models.Trees;
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
        private readonly IFileSystemProviderRepository _fileSystemProviderRepository;
        private readonly ISettingsService _settingsService;
        private readonly IStatisticService _statisticService;
        private readonly IImageService _imageService;
        private readonly IHistoryService _historyService;
        private readonly IImageCropperInfoService _imageCropperInfoService;
        private readonly IUmbracoDbRepository _umbracoDbRepository;
        private readonly IValidationService _validationService;

        //for testing
        private readonly IHistoryRepository _historyRepository;

        public SectionService(IFileSystemProviderRepository fileSystemProviderRepository, ISettingsService settingsService,
            IStatisticService statisticService, IImageService imageService, IHistoryService historyService, IImageCropperInfoService imageCropperInfoService,
            IHistoryRepository historyRepository, IUmbracoDbRepository umbracoDbRepository, IValidationService validationService)
        {
            _fileSystemProviderRepository = fileSystemProviderRepository;
            _settingsService = settingsService;
            _statisticService = statisticService;
            _imageService = imageService;
            _historyService = historyService;
            _imageCropperInfoService = imageCropperInfoService;
            _historyRepository = historyRepository;
            _umbracoDbRepository = umbracoDbRepository;
            _validationService = validationService;
        }

        public void Initialize()
        {
            SetFileSystemProvider();
            ServerVariablesParser.Parsing += Parsing;
            TreeControllerBase.MenuRendering += MenuRenderingHandler;
            TreeControllerBase.TreeNodesRendering += CustomTreeNodesRendering;
            //ContentService.Saving += ContentService_Saving;

            MediaService.Saved += MediaService_Saved;
            //MediaService.Saving += MediaService_Saving;
            MediaService.Deleted += MediaService_Deleted;

            #region MyRegion

            //PackagingService.UninstalledPackage += PackagingService_UninstalledPackage;
            //PackagingService.ImportedPackage += PackagingService_ImpordedPackage;



            //InstalledPackage.BeforeDelete += PackagingService_UninstalledPackage;
            //InstalledPackage.BeforeSave += InstalledPackage_BeforeSave;

            //ContentService.Saving += ContentService_Saving;
            //MediaService.Saving += MediaService_Saving;
            //MediaService.DeletedVersions += MediaService_DeletedVersions;
            //MediaService.EmptiedRecycleBin += MediaService_EmptiedRecycleBin;
            #endregion

        }

        private void CustomTreeNodesRendering(TreeControllerBase sender, TreeNodesRenderingEventArgs e)
        {

            if (string.Equals(sender.TreeAlias, PackageConstants.MediaAlias, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var node in e.Nodes)
                {
                    //node.Icon = "icon-compress";

                    //var fileInfo = new FileInfo(System.Web.HttpContext.Current.Server.MapPath("~/App_Plugins/Tinifier/pointer.svg"));
                    //if (fileInfo.Exists)
                    //    node.Icon = "~" + System.Web.HttpContext.Current.Server.MapPath("~/App_Plugins/Tinifier/pointer.png");
                    var history = _historyService.GetImageHistory(node.Id as string);
                    if (history != null)
                        node.Icon = "icon-umb-media color-orange";
                }
            }
        }

        private void PackagingService_ImpordedPackage(IPackagingService sender, ImportPackageEventArgs<InstallationSummary> e)
        {
            var s = sender;
        }

        private void PackagingService_UninstalledPackage(IPackagingService sender, UninstallPackageEventArgs e)
        {
            var pack = e.UninstallationSummary.FirstOrDefault();
            if (pack == null)
                return;
            if (pack.MetaData.Name == PackageConstants.SectionName)
            {
                try
                {
                    var directory = new DirectoryInfo(System.Web.HttpContext.Current.Server.MapPath("~/App_Plugins/" + PackageConstants.SectionName));
                    if (directory != null && directory.Exists)
                        directory.Delete(true);
                }
                catch (Exception ex)
                { }
            }
        }

        private void MediaService_DeletedVersions(IMediaService sender, DeleteRevisionsEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void MediaService_Deleted(IMediaService sender, DeleteEventArgs<IMedia> e)
        {
            foreach (var item in e.DeletedEntities)
                _historyService.Delete(item.Id.ToString());

            _statisticService.UpdateStatistic();
        }

        public void Terminate()
        {
        }

        /// <summary>
        /// Update number of images in statistic before removing from recyclebin
        /// </summary>
        /// <param name="sender">IMediaService</param>
        /// <param name="e">RecycleBinEventArgs</param>


        private void ContentService_Saving(IContentService sender, SaveEventArgs<IContent> e)
        {
            var settingService = _settingsService.GetSettings();
            if (settingService == null)
                return;

            // foreach (var entity in e.SavedEntities)
            // {
            //     var imageCroppers = entity.Properties.Where(x => x.PropertyType.PropertyEditorAlias ==
            //                                                      Constants.PropertyEditors.Aliases.ImageCropper);
            //
            //     foreach (Property crop in imageCroppers)
            //     {
            //         var key = string.Concat(entity.Name, "-", crop.Alias);
            //         var imageCropperInfo = _imageCropperInfoService.Get(key);
            //         var imagePath = crop.GetValue();
            //
            //         //Wrong object
            //         if (imageCropperInfo == null && imagePath == null)
            //             continue;
            //
            //         //Cropped file was Deleted
            //         if (imageCropperInfo != null && imagePath == null)
            //         {
            //             _imageCropperInfoService.DeleteImageFromImageCropper(key, imageCropperInfo);
            //             continue;
            //         }
            //
            //         var json = JObject.Parse(imagePath.ToString());
            //         var path = json.GetValue("src").ToString();
            //
            //         //republish existed content
            //         if (imageCropperInfo != null && imageCropperInfo.ImageId == path)
            //             continue;
            //
            //         //Cropped file was created or updated
            //         _imageCropperInfoService.GetCropImagesAndTinify(key, imageCropperInfo, imagePath,
            //             settingService.EnableOptimizationOnUpload, path);
            //     }
            // }
        }

        private void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            MediaSavingHelper.IsSavingInProgress = true;

            // MediaSavingHelper.IsSavingInProgress = true;
            // // reupload image issue https://goo.gl/ad8pTs
            // HandleMedia(e.SavedEntities,
            //         (m) => _historyService.Delete(m.Id.ToString()),
            //         (m) => m.IsPropertyDirty(PackageConstants.UmbracoFileAlias));

            foreach (var mediaEntity in e.SavedEntities)
            {
                var node = _umbracoDbRepository.GetNodeById(mediaEntity.Id.ToString());

            }


        }

        private void MediaService_Saved(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            //IF media already exists, we should not handle content. If we want to tinify media, we should use Tinify menu. 

            var images = _imageService.GetAllImages();
            var statistic = _statisticService.GetStatistic();

            if (statistic.TotalNumberOfImages == images.Count())
                return;

            //foreach (var mediaEntity in e.SavedEntities)
            //{
            //    var node = _umbracoDbRepository.GetNodeById(mediaEntity.Id.ToString());
            //    if (node != null)
            //        return;
            //}

            MediaSavingHelper.IsSavingInProgress = false;

            foreach (var media in e.SavedEntities)
                _historyService.Delete(media.Id.ToString());

            _statisticService.UpdateStatistic();
            // // optimize on upload
            var settingService = _settingsService.GetSettings();
            if (settingService == null || settingService.EnableOptimizationOnUpload == false)
                return;


            HandleMedia(e.SavedEntities,
                (m) =>
                {
                    try
                    {
                        OptimizeOnUploadAsync(m.Id, e).GetAwaiter().GetResult();
                    }
                    catch (NotSupportedExtensionException)
                    { }
                });
        }

        private void HandleMedia(IEnumerable<IMedia> items, Action<IMedia> action, Func<IMedia, bool> predicate = null)
        {
            var isChanged = false;
            foreach (var item in items)
            {
                if (string.Equals(item.ContentType.Alias, PackageConstants.ImageAlias, StringComparison.OrdinalIgnoreCase))
                {
                    if (action != null && (predicate == null || predicate(item)))
                    {
                        action(item);
                        isChanged = true;
                    }
                }
            }
            // if (isChanged)
            //     _statisticService.UpdateStatistic();
        }

        /// <summary>
        /// Call methods for tinifing when upload image
        /// </summary>
        /// <param name="mediaItemId">Media Item Id</param>
        /// <param name="e">CancellableEventArgs</param>
        private async System.Threading.Tasks.Task OptimizeOnUploadAsync(int mediaItemId, CancellableEventArgs e)
        {
            TImage image;

            try
            {
                image = _imageService.GetImage(mediaItemId);
            }
            catch (NotSupportedExtensionException ex)
            {
                e.Messages.Add(new EventMessage(PackageConstants.ErrorCategory, ex.Message,
                    EventMessageType.Error));
                throw;
            }

            var imageHistory = _historyService.GetImageHistory(image.Id);

            if (imageHistory == null)
                await _imageService.OptimizeImageAsync(image).ConfigureAwait(false);
        }

        private void MenuRenderingHandler(TreeControllerBase sender, MenuRenderingEventArgs e)
        {
            if (string.Equals(sender.TreeAlias, PackageConstants.MediaAlias, StringComparison.OrdinalIgnoreCase))
            {
                var mediaIsFolder = false;
                //NodeId = -1 it means Recycle folder
                if (e.NodeId == "-21")
                    return;

                //NodeId = -1 it means root folder. Only "Organise by date" should be added.
                if (e.NodeId == "-1")
                {
                    var menuItemOrganizeImagesButton = new MenuItem(PackageConstants.OrganizeImagesButton, PackageConstants.OrganizeImagesCaption);
                    menuItemOrganizeImagesButton.LaunchDialogView(PackageConstants.OrganizeImagesRoute, PackageConstants.OrganizeImagesCaption);
                    e.Menu.Items.Add(menuItemOrganizeImagesButton);
                    return;
                }

                var history = _historyService.GetImageHistory(e.NodeId);

                if (history == null)
                {
                    MenuItem menuItemTinifyButton = new MenuItem(PackageConstants.TinifierButton, PackageConstants.TinifierButtonCaption);
                    menuItemTinifyButton.LaunchDialogView(PackageConstants.TinyTImageRoute, PackageConstants.SectionName);
                    menuItemTinifyButton.SeparatorBefore = true;
                    menuItemTinifyButton.Icon = PackageConstants.MenuIcon;

                    //If media is folder
                    mediaIsFolder = _validationService.IsFolder(int.Parse(e.NodeId));
                    if (mediaIsFolder)
                        menuItemTinifyButton.Name += " folder";
                    else
                        menuItemTinifyButton.Name += " media";

                    e.Menu.Items.Add(menuItemTinifyButton);
                }
                else
                {
                    var menuItemUndoTinifyButton = new MenuItem(PackageConstants.UndoTinifierButton, PackageConstants.UndoTinifierButtonCaption);
                    menuItemUndoTinifyButton.LaunchDialogView(PackageConstants.UndoTinyTImageRoute, PackageConstants.UndoTinifierButtonCaption);
                    menuItemUndoTinifyButton.Icon = PackageConstants.UndoTinifyIcon;
                    e.Menu.Items.Add(menuItemUndoTinifyButton);

                    var menuItemSettingsButton = new MenuItem(PackageConstants.StatsButton, PackageConstants.StatsButtonCaption);
                    menuItemSettingsButton.LaunchDialogView(PackageConstants.TinySettingsRoute, PackageConstants.StatsDialogCaption);
                    menuItemSettingsButton.Icon = PackageConstants.MenuSettingsIcon;
                    e.Menu.Items.Add(menuItemSettingsButton);
                }
            }
        }

        private void SetFileSystemProvider()
        {
            var path = HostingEnvironment.MapPath("~/Web.config");
            var doc = new XmlDocument();
            doc.Load(path);

            XmlNode xmlNode = doc.DocumentElement.SelectSingleNode("appSettings");
            XmlNodeList xmlList = xmlNode.SelectNodes("add");

            var fileSystemType = "PhysicalFileSystem";
            foreach (XmlNode xmlNodeS in xmlList)
            {
                if (xmlNodeS.Attributes.GetNamedItem("key").Value.Contains("AzureBlobFileSystem"))
                {
                    fileSystemType = "AzureBlobFileSystem";
                    break;
                }
            }

            _fileSystemProviderRepository.Delete();
            _fileSystemProviderRepository.Create(fileSystemType);
        }

        private void Parsing(object sender, Dictionary<string, object> dictionary)
        {
            //var umbracoPath = WebConfigurationManager.AppSettings["umbracoPath"];
            //
            //var apiRoot = $"{umbracoPath.Substring(1)}/backoffice/api/";
            //
            //var urls = dictionary["umbracoUrls"] as Dictionary<string, object>;
            //urls["tinifierApiRoot"] = apiRoot;
        }
    }
}

