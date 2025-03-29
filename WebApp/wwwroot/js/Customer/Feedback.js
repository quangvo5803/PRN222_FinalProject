document.addEventListener("DOMContentLoaded", () => {
    const stars = document.querySelectorAll(".star");
    const ratingText = document.querySelector(".rating-text");
    const ratingInput = document.getElementById("ratingInput");
    const ratingLabels = {
        1: "Very bad",
        2: "Poor",
        3: "Fair",
        4: "Good",
        5: "Excellent"
    };

    let currentRating = 5; // Mặc định là 5 sao

    const setRating = (rating) => {
        currentRating = rating;
        stars.forEach(star => {
            const value = parseInt(star.dataset.value);
            star.textContent = value <= rating ? "★" : "☆";
            star.classList.toggle("active", value <= rating);
        });
        ratingText.textContent = ratingLabels[rating];
        ratingInput.value = rating; // Cập nhật giá trị vào input ẩn
    };

    stars.forEach(star => {
        star.addEventListener("click", () => {
            setRating(parseInt(star.dataset.value));
        });
    });

    setRating(currentRating); // Gọi lần đầu khi trang tải
});

// Upload & preview ảnh
const imageInput = document.getElementById('imageInput');
const imagePreview = document.getElementById('imagePreview');
const addImageBtn = document.getElementById('addImageBtn');

addImageBtn.addEventListener('click', () => imageInput.click());

imageInput.addEventListener('change', () => {
    imagePreview.innerHTML = '';
    Array.from(imageInput.files).forEach(file => {
        const reader = new FileReader();
        reader.onload = function (e) {
            const box = document.createElement('div');
            box.className = 'image-box';
            box.innerHTML = `<img src="${e.target.result}" /><button class="remove-btn" onclick="this.parentElement.remove()">×</button>`;
            imagePreview.appendChild(box);
        };
        reader.readAsDataURL(file);
    });
});

// Gửi ảnh đến server
document.getElementById("submitBtn").addEventListener("click", async () => {
    let formData = new FormData();
    selectedFiles.forEach((file) => {
        formData.append("Images", file);
    });

    const response = await fetch("/Customer/SubmitFeedBack", {
        method: "POST",
        body: formData
    });

    const result = await response.text();
    console.log(result);
});