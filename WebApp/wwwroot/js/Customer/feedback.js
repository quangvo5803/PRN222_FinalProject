document.addEventListener("DOMContentLoaded", () => {
    const stars = document.querySelectorAll(".star");
    const ratingText = document.querySelector(".rating-text");

    const ratingLabels = {
        1: "Rất tệ",
        2: "Tệ",
        3: "Bình thường",
        4: "Tốt",
        5: "Tuyệt vời"
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
    };

    stars.forEach(star => {
        star.addEventListener("click", () => {
            setRating(parseInt(star.dataset.value));
        });
    });

    setRating(currentRating); // Gọi lần đầu khi trang tải
});
//ảnh
document.addEventListener("DOMContentLoaded", () => {
    const addImageBtn = document.getElementById("addImageBtn");
    const imagePreview = document.getElementById("imagePreview");
    const maxImages = 3;
    let imageCount = 0;

    const createImageBox = (src) => {
        const box = document.createElement("div");
        box.className = "image-box";

        const img = document.createElement("img");
        img.src = src;

        const removeBtn = document.createElement("button");
        removeBtn.className = "remove-btn";
        removeBtn.textContent = "x";
        removeBtn.onclick = () => {
            box.remove();
            imageCount--;
            updatePlaceholder();
        };

        box.appendChild(img);
        box.appendChild(removeBtn);
        return box;
    };

    const createPlaceholderBox = () => {
        const box = document.createElement("div");
        box.className = "image-box placeholder";
        box.innerHTML = `<span>${imageCount}/${maxImages}</span>`;
        return box;
    };

    const updatePlaceholder = () => {
        // Xoá placeholder cũ
        const existing = imagePreview.querySelector(".placeholder");
        if (existing) existing.remove();
        // Thêm placeholder mới nếu chưa đủ ảnh
        if (imageCount < maxImages) {
            imagePreview.appendChild(createPlaceholderBox());
        }
    };

    addImageBtn.addEventListener("click", () => {
        if (imageCount >= maxImages) return alert("Bạn chỉ được thêm tối đa 5 ảnh.");

        // Tạo input file ẩn
        const input = document.createElement("input");
        input.type = "file";
        input.accept = "image/*";
        input.onchange = () => {
            const file = input.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = () => {
                    const imgBox = createImageBox(reader.result);
                    imagePreview.insertBefore(imgBox, imagePreview.lastChild); // Trước placeholder
                    imageCount++;
                    updatePlaceholder();
                };
                reader.readAsDataURL(file);
            }
        };
        input.click();
    });

    updatePlaceholder(); // Hiển thị ban đầu
});