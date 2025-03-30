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
                "width": "10%"
            },
            {
                "data": 'price',
                "width": "5%",
                "render": function (data, type, row) {
                    return new Intl.NumberFormat('vn-VN', { style: 'currency', currency: 'VND' }).format(data);
                }
            },
            {
                "data": 'categoryName',
                "width": "10%",
            },
            {
                "data": 'avgRating',
                "width": "10%",
                "render": function (data) {
                    return `${data.toFixed(1)} ⭐`;
                }
            },
            {
                "data": 'feedbackCount',
                "width": "10%",
                "render": function (data) {
                    return `${data} feedback(s)`;
                }
            },
            {
                "data": 'id',
                "width": "20%",
                "render": function (data, type, row) {
                    return `
                    <div class="btn-group d-flex justify-content-between" role="group">
                       <a href="/Admin/EditProduct?id=${row.id}" class="btn btn-dark flex-grow-1 mx-1">Edit</a>
                       <a href="/Admin/ViewProductFeedback?id=${row.id}" class="btn btn-primary flex-grow-1 mx-1">View Feedback</a>
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