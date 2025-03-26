$(document).ready(function () {
    new WOW().init();

    $(".testimonial-carousel").owlCarousel({
        autoplay: true,
        smartSpeed: 1000,
        center: true,
        margin: 24,
        dots: true,
        loop: true,
        nav: false,
        responsive: {
            0: { items: 1 },
            768: { items: 2 },
            992: { items: 3 }
        }
    });
    var signUpBtn = document.getElementById("signUpBtn");
    if (signUpBtn) {
        signUpBtn.addEventListener("click", function () {
            var email = document.getElementById("emailInput").value.trim();
            if (email) {
                window.location.href = "/Home/Register?email=" + encodeURIComponent(email);
            } else {
                alert("Vui lòng nhập email!");
            }
        });
    }
});
