var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            url: '/admin/getallfeedback',
            type: 'GET',
            dataSrc: 'data'
        },
        "columns": [
            {
                "data": null,
                "width": "5%",
                "render": function (data, type, row, meta) {
                    return meta.row + 1;
                }
            },
            {
                "data": 'user.username',
                "width": "5%"
            },
            {
                "data": 'product.name',
                "width": "5%",   
            },
            {
                "data": 'feedbackstars',
                "width": "5%",
            },
            {
                "data": 'feedbackcontent',
                "width": "5%",
            }

        ]
    });
}