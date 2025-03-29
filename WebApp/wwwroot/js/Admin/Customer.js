var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            url: '/admin/getallcustomer',
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
                "data": 'email',
                "with": "15%"
            },
            {
                "data": 'userName',
                "with": "15%"
            },
            {
                "data": 'phoneNumber',
                "with": "15%"
            },
            {
                "data": 'id',
                "width": "20%",
                "render": function (data, type, row) {
                    return `
                    <div class="btn-group d-flex justify-content-between" role="group">
                       <a href="/Admin/CustomerDetail?id=${row.id}" class="btn btn-dark flex-grow-1 mx-1">Detail</a>
                       
                    </div>`;
                }
            }

        ]
    });
}