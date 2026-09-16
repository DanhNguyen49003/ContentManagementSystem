// CMS Portal JavaScript Helper
// ==========================================

// Hàm chuyển đổi chuỗi tiếng Việt thành Slug chuẩn SEO
function convertToSlug(text) {
    if (!text) return '';
    let str = text.toLowerCase().trim();

    // 1. Chuyển đổi toàn bộ ký tự tiếng Việt có dấu sang không dấu
    str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, 'a');
    str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, 'e');
    str = str.replace(/ì|í|ị|ỉ|ĩ/g, 'i');
    str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, 'o');
    str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, 'u');
    str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, 'y');
    str = str.replace(/đ/g, 'd');

    // 2. Loại bỏ các ký tự đặc biệt, chỉ giữ lại chữ cái, chữ số, khoảng trắng và gạch ngang
    str = str.replace(/[^a-z0-9\s-]/g, '');

    // 3. Đổi nhiều khoảng trắng hoặc dấu gạch nối liên tiếp thành 1 dấu gạch ngang duy nhất
    str = str.replace(/[\s-]+/g, '-');

    // 4. Xóa dấu gạch ngang ở đầu và cuối chuỗi
    str = str.replace(/^-+|-+$/g, '');

    return str;
}

// Tự động kích hoạt cơ chế sinh slug cho toàn bộ các Form có ô Name/Title và Slug
document.addEventListener('DOMContentLoaded', function () {
    const slugInputs = document.querySelectorAll('input#Slug, input[name="Slug"]');

    slugInputs.forEach(slugInput => {
        const form = slugInput.closest('form');
        if (!form) return;

        // Tìm ô nguồn: Name hoặc Title
        const sourceInput = form.querySelector('input#Name, input[name="Name"], input#Title, input[name="Title"]');
        if (!sourceInput) return;

        // Đánh dấu xem người dùng đã từng gõ tay vào ô Slug chưa
        // Nếu ban đầu ô slug đã có giá trị (ví dụ trang Edit), coi như đã tùy chỉnh
        let isManuallyEdited = slugInput.value.trim() !== '';

        // Khi người dùng gõ vào ô Slug
        slugInput.addEventListener('input', function () {
            // Nếu người dùng xóa sạch ô slug thì cho phép tự động đồng bộ lại từ Name/Title
            if (slugInput.value.trim() === '') {
                isManuallyEdited = false;
            } else {
                isManuallyEdited = true;
            }
        });

        // Khi người dùng gõ vào ô Name hoặc Title
        sourceInput.addEventListener('input', function () {
            if (!isManuallyEdited) {
                slugInput.value = convertToSlug(sourceInput.value);
            }
        });

        // Tìm nút "Tạo tự động" nếu có
        const btnRegenerate = form.querySelector('#btnGenerateSlug, .btn-generate-slug');
        if (btnRegenerate) {
            btnRegenerate.addEventListener('click', function (e) {
                e.preventDefault();
                slugInput.value = convertToSlug(sourceInput.value);
                isManuallyEdited = false;
            });
        }
    });
});

