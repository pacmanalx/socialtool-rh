(function($) {

window.addEventListener('DOMContentLoaded', function () {
        var avatar = document.getElementById('avatar');
        var image = document.getElementById('image');
        var input = document.getElementById('input');
        var $progress = $('.progress');
        var $progressBar = $('.progress-bar');
        var $modal = $('#modal');
        var cropper;

        $('[data-toggle="tooltip"]').tooltip();

        input.addEventListener('change', function (e) {
            var files = e.target.files;
            var done = function (url) {
                input.value = '';
                image.src = url;
                $modal.modal('show');
            };
            var reader;
            var file;
            var url;

            if (files && files.length > 0) {
                file = files[0];

                if (URL) {
                    done(URL.createObjectURL(file));
                } else if (FileReader) {
                    reader = new FileReader();
                    reader.onload = function (e) {
                        done(reader.result);
                    };
                    reader.readAsDataURL(file);
                }
            }
        });

        $modal.on('shown.bs.modal', function () {
            cropper = new Cropper(image, {
                viewMode: 1,
                aspectRatio: 1
            });
        }).on('hidden.bs.modal', function () {
            cropper.destroy();
            cropper = null;
        });

        document.getElementById('crop').addEventListener('click', function () {
            var initialAvatarURL;
            var canvas;

            $modal.modal('hide');

            if (cropper) {
                canvas = cropper.getCroppedCanvas({
                    width: 500,
                    height: 500,
                });
                initialAvatarURL = avatar.src;
                avatar.src = canvas.toDataURL();
                $progress.show();
                canvas.toBlob(function (blob) {
                    var formData = new FormData();
                    var csrfToken = $('meta[name="csrf-token"]');

                    formData.append('avatar', blob, 'avatar.jpg');
                    formData.append(csrfToken.attr('data-name-key'), csrfToken.attr('data-name-value'));
                    formData.append(csrfToken.attr('data-value-key'), csrfToken.attr('data-value-value'));

                    $.ajax('/perfil/avatar', {
                        method: 'POST',
                        data: formData,
                        processData: false,
                        contentType: false,
                        headers: {
                            'X-CSRF-TOKEN': csrfToken.attr('data-name-value') + '|' + csrfToken.attr('data-value-value'),
                            'X-Requested-With': 'XMLHttpRequest'
                        },

                        success: function (response) {
                            location.reload();
                        },

                        error: function (request, status, erro) {
                            swal({
                                title: "Oops",
                                text: 'Algo deu errado. Por favor tente novamente.',
                                type: "error"
                            });
                        }
                    });
                });
            }
        });
    });

})(jQuery);
