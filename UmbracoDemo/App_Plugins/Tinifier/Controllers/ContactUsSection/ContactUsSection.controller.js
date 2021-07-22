angular.module("umbraco").controller("Tinifier.ContactUsSection.Controller",
    function ($scope, $http, $injector, notificationsService, editorService, cookieService) {
        $scope.report = {};
        $scope.report.Name = "";
        $scope.report.Email = "";
        $scope.report.Descroption = "";
        $scope.report.File = "";
        $scope.report.Files = [];
        $scope.report.Type = "Bug";

        let model = cookieService;
        console.log(model);
        model.log();



        function loaded(evt) {
            $scope.report.Files.push(evt.target.result);
        }

        $scope.paste = function () {

            try {
                navigator.clipboard.read().then(
                    clipboardItems => {

                        if (clipboardItems[0].types[0] === "image/png") {
                            const blobOutput = clipboardItems[0].getType("image/png");
                            let bl = Promise.resolve(blobOutput);

                            bl.then(function (v) {

                                let file = URL.createObjectURL(v);
                                var img = $('<img style=\" height: 25px; padding: 5px; border: 1px solid #e9e9e9;\">'); //Equivalent: $(document.createElement('img'))
                                img.attr('src', file);
                                img.appendTo('#clipboard-preview');

                                var reader = new FileReader();
                                reader.readAsDataURL(v);
                                reader.onload = loaded;
                            }, function (e) {
                                // not called
                            });
                        }

                    });
            }
            catch (error) {
                console.log("Exception.");
                $("#uploadimage-label").append("<span style=\" color: gray;\"> (Sorry, your browser doesn't support this function)</span>");
            }
        };

        // -------SUBMIT--------
        $scope.submit = function () {

            let formIsValid = ValidateForm();
            if (!formIsValid)
                return;

            let model = $scope.report;


            $http.post(`/umbraco/backoffice/api/ContactUs/Report`, JSON.stringify(model)).then(postSuccessCallback, postErrorCallback);

            function postSuccessCallback(response) {
                submitBtn.empty();
                submitBtn.append("<i class=\"icon-check\"></i>");
                submitBtn.prop('disabled', false);
                notificationsService.success("Report sent");
            }

            function postErrorCallback(error) {
                submitBtn.empty();
                submitBtn.append("<i class=\"icon-wrong\"></i>");
                submitBtn.css('background-color', "#ff8000");
                submitBtn.prop('disabled', false);
                notificationsService.error("Error. Server is unavaliable.");
            }


            let submitBtn = $("#submit-btn");
            submitBtn.empty();
            submitBtn.append("<i class=\"icon-timer\"></i>");
            submitBtn.prop('disabled', true);


            //let timerId = setTimeout(() => {
            //
            //    $("#submit-btn").empty();
            //    $("#submit-btn").append("<i class=\"icon-wrong\"></i>");
            //    $("#submit-btn").css('background-color', "#ff8000");
            //    $("#submit-btn").prop('disabled', false);
            //    notificationsService.success("Report sent");
            //
            //}, 2000);


        };


        function ValidateForm() {
            let result = true;
            let defaultBorder = "1px solid #d8d7d9";
            let invalidBorder = "2px solid red";
            let nameInput = $("#nameInput");
            let emailInput = $("#emailInput");
            let descriptionText = $("#descriptionText");

            if (nameInput.val() === "") {
                nameInput.css("border", invalidBorder);
                result = false;
            } else
                nameInput.css("border", defaultBorder);

            if (emailInput.val() === "") {
                emailInput.css("border", invalidBorder);
                result = false;
            } else
                emailInput.css("border", defaultBorder);

            if (descriptionText.val() === "") {
                descriptionText.css("border", invalidBorder);
                result = false;
            } else
                descriptionText.css("border", defaultBorder);

            return result;
        }
    });


angular.module("umbraco").directive("selectNgFiles", function () {
    return {
        require: "ngModel",
        link: function postLink(scope, elem, attrs, ngModel) {
            elem.on("change", function (evt) {
                //var files = elem[0].files;
                //ngModel.$setViewValue(files);
                //console.log("change");
                //
                //console.log(scope.report);
                var files = evt.target.files;

                for (var i = 0, f; f = files[i]; i++) {
                    var reader = new FileReader();
                    reader.readAsDataURL(f);
                    reader.onload = loaded;
                }

                function loaded(evt) {
                    scope.report.Files.push(evt.target.result);

                }
            });
        }
    }
});