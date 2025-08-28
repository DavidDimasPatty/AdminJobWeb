(function ($) {
    'use strict';
    $(function () {
        function initModalReject(url, dataBtn) {
            $.get({
                url: url,
                data: dataBtn,
                cache: false
            })
                .done(function (data) {
                    $('#modalContainer').html(data);
                    $('#modal').modal('show');
                })
                .fail(function (xhr, status, error) {
                    console.error('Error loading modal reject:', error);
                    alert('An error occurred while loading the reject modal. Please try again later.');
                });
        }

        $('#tableList').DataTable();

        $("#rejectTrigger").on("click", function (e) {
            var url = "/Validasi/RejectAdmin";
            var dataBtn = {
                id: $(this).data('id'),
                idPerusahaan: $(this).data('perusahaan'),
            };
            initModalReject(url, dataBtn);
        });

        $("#rejectDetailTrigger").on("click", function (e) {
            var url = "/Validasi/DetailRejectAdmin";
            var dataBtn = {
                id: $(this).data('id'),
            };
            initModalReject(url, dataBtn);
        });
    });
})(jQuery);