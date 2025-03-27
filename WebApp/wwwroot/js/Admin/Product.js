var dataTable;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            url: '/admin/getallproduct',
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
                "data": 'name',
                "width": "15%"
            },
            {
                "data": 'price',
                "width": "15%",
                "render": function (data, type, row) {
                    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(data);
                }
            },
            {
                "data": 'category.name',
                "width": "10%",
            },
            {
                "data": 'id',
                "width": "20%",
                "render": function (data, type, row) {
                    return `
                    <div class="btn-group d-flex justify-content-between" role="group">
                       <a href="/Admin/EditProduct?id=${row.id}" class="btn btn-dark flex-grow-1 mx-1">Edit</a>
                       <a onClick=Delete('/admin/DeleteProduct?id=${row.id}') class="btn btn-danger flex-grow-1 mx-1">Delete</a>
                    </div>`;               
                }
            }
        ]
    });
}
function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete?",
        text: "You cannot undo it!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!",
        cancelButtonText: "Cancel"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    location.reload();
                    toastr.success(data.message);
                }
            })
        }
    });
}