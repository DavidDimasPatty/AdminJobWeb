(function ($) {
    'use strict';
    $(function () {
        function initModalUser() {
            $.get({
                url: '/User/SendFormAdmin',
                data: {
                    link:"@ViewBag.link"
                }
            })
                .done(function (data) {
                    $('#modalContainer').html(data);
                    $('#modal').modal('show');
                })
                .fail(function (xhr, status, error) {
                    console.error('Error loading modal user:', error);
                    alert('An error occurred while loading the user modal. Please try again later.');
                });
        }

        $('#tableList').DataTable();

        $('#modalTrigger').on('click', function () {
            initModalUser();
        });
    });
})(jQuery);