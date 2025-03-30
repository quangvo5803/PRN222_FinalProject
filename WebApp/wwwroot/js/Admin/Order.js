var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#orderTable').DataTable({
        "ajax": {
            url: '/admin/getallorders',
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
                "data": "userName",
                "width": "15%"
            },
            {
                "data": "totalPrice",
                "width": "15%",
                "render": function (data) {
                    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(data);
                }
            },
            {
                "data": "orderDate",
                "width": "20%"
            },
            {
                "data": "status",
                "width": "15%",
                "render": function (data) {
                    if (data === "Pending") {
                        return '<span class="badge bg-warning">Pending</span>';
                    } else if (data === "Completed") {
                        return '<span class="badge bg-success">Completed</span>';
                    } else if (data === "Cancelled") {
                        return '<span class="badge bg-danger">Cancelled</span>';
                    }
                }
            },
            {
                "data": "status",
                "width": "30%",
                "render": function (data, type, row) {
                    if (data === "Pending") {
                        return `
                            <div class="btn-group d-flex justify-content-between" role="group">
                                <a onClick="CompleteOrder(${row.id})" class="btn btn-success flex-grow-1 mx-1">Complete</a>
                                <a onClick="CancelOrder(${row.id})" class="btn btn-danger flex-grow-1 mx-1">Cancel</a>
                            </div>`;
                    } else {
                        return '<span class="text-muted" style="display: block; text-align: center;">Processed</span>';
                    }
                }
            }
        ],
        "language": {
            "emptyTable": "No orders available."
        }
    });
}

function CompleteOrder(id) {
    Swal.fire({
        title: "Are you sure you want to complete this order?",
        text: "This action cannot be undone!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, complete it!",
        cancelButtonText: "Cancel"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/CompleteOrder',
                type: 'POST',
                data: { id: id },
                headers: {
                    RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (data) {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                },
                error: function () {
                    toastr.error("An error occurred while processing.");
                }
            });
        }
    });
}

function CancelOrder(id) {
    Swal.fire({
        title: "Are you sure you want to cancel this order?",
        text: "This action cannot be undone!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, cancel it!",
        cancelButtonText: "Cancel"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/CancelOrder',
                type: 'POST',
                data: { id: id },
                headers: {
                    RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (data) {
                    dataTable.ajax.reload();
                    toastr.success(data.message);
                },
                error: function () {
                    toastr.error("An error occurred while processing.");
                }
            });
        }
    });
}