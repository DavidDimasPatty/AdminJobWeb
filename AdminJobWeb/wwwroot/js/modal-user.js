(function ($) {
    'use strict';
    $(function () {
        function initModalUser() {
            $.ajax({
                url: '/User/CreateAdmin',
                type: 'GET',
                success: function (data) {
                    $('#modalContainer').html(data);
                    $('#modal').modal('show');
                },
                error: function (xhr, status, error) {
                    console.error('Error loading modal user:', error);
                    alert('An error occurred while loading the user modal. Please try again later.');
                }
            });
        }

        $('#modalTrigger').on('click', function () {
            initModalUser();
        });
    });
})(jQuery);