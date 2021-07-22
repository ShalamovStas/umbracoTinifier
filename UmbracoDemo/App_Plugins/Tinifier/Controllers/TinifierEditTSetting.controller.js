angular.module("umbraco").controller("Tinifier.TinifierEditTSetting.Controller",
    function ($scope, $http, $injector, $routeParams, notificationsService, editorService, appState, eventsService, overlayService, cookieService) {

        //let model = cookieService;
        //console.log(model);
        //model.log();

        //===========================================
        $scope.timage = {};

        let settingsIsLoading = true;

        let statistic = {};
        let tsetting = {};
        let cookieTitle = "tinifierSettings";
        let cookieExpirationMinutes = 60;

        // work with cookie
        function setCookie(cvalue) {
            var d = new Date();
            d.setTime(d.getTime() + (cookieExpirationMinutes * 60 * 1000));
            var expires = "expires=" + d.toUTCString();
            document.cookie = cookieTitle + "=" + cvalue + ";" + expires + ";path=/";
        }

        function getCookie() {
            var name = cookieTitle + "=";
            var decodedCookie = decodeURIComponent(document.cookie);
            var ca = decodedCookie.split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) == ' ') {
                    c = c.substring(1);
                }
                if (c.indexOf(name) == 0) {
                    return c.substring(name.length, c.length);
                }
            }
            return "";
        }

        var cookie = getCookie(cookieTitle);
        if (!cookie)
            GetStatistic();
        else {
            let cookieTinifier = {};
            cookieTinifier = JSON.parse(cookie);
            if (!cookieTinifier || !cookieTinifier.statistic || !cookieTinifier.tsetting)
                GetStatistic();
            settingsIsLoading = false;
            $scope.timage = cookieTinifier.tsetting;
           // statistic.TotalNumberOfImages = cookieTinifier.statistic.TotalNumberOfImages;
           // statistic.TotalOptimizedImages = cookieTinifier.statistic.TotalOptimizedImages;
           // statistic.TotalOriginalImages = cookieTinifier.statistic.TotalOriginalImages;
           //
           // tsetting = cookieTinifier.tsetting;
           // $scope.timage = cookieTinifier.tsetting;
           //
           // settingsIsLoading = false;
           // if ($scope.timage.HideLeftPanel)
           //     HideLeftPannel();
           // else
           //     SetDefaultLeftPannel();
        }

        //GetStatistic();

        function HideLeftPannel() {
            var contentwrapperContainer = document.getElementById("contentwrapper");
            var navigationContainer = document.getElementById("navigation");
            appState.setGlobalState("showNavigation", false);
            contentwrapperContainer.style.left = "0px";
            navigationContainer.style.width = "0px";
        }

        function SetDefaultLeftPannel() {
            var element = document.getElementById("contentwrapper");
            defaultContentwrapperStyle = window.getComputedStyle(element, null).getPropertyValue("left");
            appState.setGlobalState("showNavigation", true);
            element.removeAttribute("style");

            navigationContainer = document.getElementById("navigation");
            navigationContainer.removeAttribute("style");
        }

        //HideLeftPannel();

        var subscriber = eventsService.on('appState.sectionState.changed', function (event, args) {

            if (!args.value)
                return;
            if (args.value != "tinifier") {

                SetDefaultLeftPannel();
                eventsService.unsubscribe(subscriber);
            }
        });

        $scope.submit = function () {
            SubmitSettings();
        };

        $scope.stopTinifing = function () {

            overlayService.open({
                view: "/App_Plugins/Tinifier/Backoffice/Dashboards/Templates/ConfirmStopTinifingDialog.html",

                submitButtonLabel: "Stop tinifing!",
                closeButtonLabel: "Cancel",
                submit: function () {
                    $http.delete(`/umbraco/backoffice/api/TinifierState/DeleteActiveState`).then(function (response) {
                        notificationsService.success("Success", "Tinifing successfully stoped!");
                        location.reload();
                    }, function (response) {
                        notificationsService.error("Error", "Tinifing can`t stop now!");
                    });

                    overlayService.close();
                },
                close: function () {
                    overlayService.close();
                }
            });
        };

        $scope.tinifyEverything = function () {
            let submitBtn = $("#tinifyAllSubmit");
            submitBtn.prop('disabled', true);

            let callback = function () {
                submitBtn.prop('disabled', false);

                if (typeof tsetting.ApiKey === 'undefined' || !tsetting.ApiKey) {
                    overlayService.open({
                        content: `ApiKey not found!`,
                        submitButtonLabel: "Ok",
                        closeButtonLabel: "Cancel",

                        submit: function () {
                            overlayService.close();

                        },
                        close: function () {
                            overlayService.close();
                        }
                    });
                    return;
                }

                let tinifyAllFunctionIsAvaliable = (500 - tsetting.CurrentMonthRequests) >= (statistic.TotalNumberOfImages - statistic.TotalOptimizedImages);
                overlayService.open({
                    view: "/App_Plugins/Tinifier/Backoffice/Dashboards/Templates/ConfirmTinifyEverythingDialog.html",
                    content: `<p>${statistic.TotalNumberOfImages - statistic.TotalOptimizedImages} images will be compressed</p>`,
                    usage: `<p style="color: #2fb576"><strong>${500 - tsetting.CurrentMonthRequests}</p>`,
                    tinifierIsAvaliable: tinifyAllFunctionIsAvaliable,
                    submitButtonLabel: "Tinify everything!",
                    closeButtonLabel: "Cancel",

                    submit: function () {
                        notificationsService
                            .add({
                                headline: "Tinifing started",
                                message: "click <a href=\"/umbraco/#/tinifier\" target=\"_blank\">here</a> for more details",
                                url: '/umbraco/#/tinifier',
                                type: 'success'
                            });

                        $http.put(`/umbraco/backoffice/api/Tinifier/TinifyEverything`).then(postSuccessCallback, postErrorCallback);

                        function postSuccessCallback(response) {
                            notificationsService.success("Success", response.message);
                            GetStatistic();
                        }

                        function postErrorCallback(error) {
                            if (error.Error === 1) {
                                notificationsService.warning("Warning", error.message);
                            }
                            else {
                                notificationsService.error("Error", error.data.headline + " " + error.data.message);
                            }
                        }
                        overlayService.close();

                    },
                    close: function () {
                        overlayService.close();
                    }
                });
            }

            GetStatistic(callback);
        };

        function GetStatistic(CallbackFunction) {
            notificationsService.success("Loading ...");
            $http.get(`/umbraco/backoffice/api/TinifierImagesStatistic/GetStatistic`).then(function (response) {
                let cookieTinifier = {};
                if (response.data.statistic) {
                    statistic.TotalNumberOfImages = response.data.statistic.TotalNumberOfImages;
                    statistic.TotalOptimizedImages = response.data.statistic.TotalOptimizedImages;
                    statistic.TotalOriginalImages = response.data.statistic.TotalOriginalImages;
                    //cookie model
                    cookieTinifier.statistic = response.data.statistic;
                }
                if (response.data.tsetting) {
                    // tsetting.CurrentMonthRequests = response.data.tsetting.CurrentMonthRequests;
                    // tsetting.ApiKey = response.data.tsetting.ApiKey;
                    tsetting = response.data.tsetting;
                    cookieTinifier.tsetting = response.data.tsetting;

                    settingsIsLoading = false;

                    // remove notification
                    RemoveNotification("Loading ...");

                    $scope.timage = response.data.tsetting;
                    if ($scope.timage.HideLeftPanel)
                        HideLeftPannel();
                    else
                        SetDefaultLeftPannel();
                }
                setCookie(JSON.stringify(cookieTinifier));
                //console.clear();
                //console.log("GetStatisticModel");
                //console.log(cookieTinifier);

                if (CallbackFunction)
                    CallbackFunction();
            });
        }

        function SubmitSettings() {
            let tsetting = $scope.timage;

            tsetting.ApiKey = $('#apiKey').val();

            if (tsetting.ApiKey === "") {
                notificationsService.warning("Settings can`t be saved! Please, fill in the ApiKey form. You can get it on a TinyPNG website");
                $("#apiKey").css("border", "2px solid red");
                return;
            }
            $("#apiKey").css("border", "");
            notificationsService.success("Saving in progress ...");
            settingsIsLoading = true;

            $http.post(`/umbraco/backoffice/api/TinifierSettings/PostTSetting`, JSON.stringify(tsetting)).then(postSuccessCallback, postErrorCallback);

            function postSuccessCallback(response) {

                RemoveNotification("Saving in progress ...");
                notificationsService.success("✔️ Settings successfully saved!");
                settingsIsLoading = false;

                let cookieTinifier = {};
                cookieTinifier.statistic = statistic;
                cookieTinifier.tsetting = tsetting;
                setCookie(JSON.stringify(cookieTinifier));

                if ($scope.timage.HideLeftPanel)
                    HideLeftPannel();
                else
                    SetDefaultLeftPannel();
                //GetStatistic();
            }

            function postErrorCallback(error) {
                notificationsService.remove();
                settingsIsLoading = false;

                if (error.Error === 1) {
                    notificationsService.warning("Warning", error.message);
                }
                else {
                    notificationsService.error("Error", error.data.headline + " " + error.data.message);
                }
            }
        }

        function RemoveNotification(notificationMesssage) {
            for (let i = 0; i < notificationsService.current.length; i++) {
                let currentMessage = notificationsService.current[i];
                if (currentMessage.headline === notificationMesssage)
                    notificationsService.remove(i);
            }
        }

        $(document).ready(function () {
            var previousApiKey = "";

            $("#apiKey").focusout(function () {
                ValidateApiKey();
            });

            $("#apiKey").focus(function () {
                previousApiKey = $('#apiKey').val();
            });

            $("#apiKey").keypress(function (event) {
                if (event.charCode == 13)
                    ValidateApiKey();
            });

            $scope.hideLeftPanelSetting = function () {
                if (!settingsIsLoading) {
                    $scope.timage.HideLeftPanel = !$scope.timage.HideLeftPanel;
                    SubmitSettings();
                }
            };

            $scope.preserveMetadataSetting = function () {
                if (!settingsIsLoading) {
                    $scope.timage.PreserveMetadata = !$scope.timage.PreserveMetadata;
                    SubmitSettings();
                }
            };

            $scope.enableUndoOptimizationSetting = function () {
                if (!settingsIsLoading) {
                    $scope.timage.EnableUndoOptimization = !$scope.timage.EnableUndoOptimization;
                    SubmitSettings();
                }
            };

            $scope.enableOptimizationOnUploadSetting = function () {
                if (!settingsIsLoading) {
                    $scope.timage.EnableOptimizationOnUpload = !$scope.timage.EnableOptimizationOnUpload;
                    SubmitSettings();
                }
            };

            function ValidateApiKey() {
                var actualApiKey = $('#apiKey').val();

                if (previousApiKey == null)
                    previousApiKey = "";

                if (previousApiKey !== actualApiKey)
                    SubmitSettings();

                previousApiKey = actualApiKey;
            }
        });
    });

angular.module("umbraco").factory('cookieService', function () {

    cookieTinifier = {
        value: "cookieService message",
        log: function () {
            console.log(this.value);
        }
    };

    return cookieTinifier;
});
