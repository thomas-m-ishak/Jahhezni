document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("loginForm");
    const submitBtn = document.getElementById("submitBtn");

    form.addEventListener("submit", function (e) {
        // نتحقق إذا كان الفورم صالحاً برمجياً
        if ($(form).valid()) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> جاري تسجيل الدخول...';
        } else {
            // تأثير خفيف عند الخطأ
            submitBtn.classList.add("shake-animation");
            setTimeout(() => submitBtn.classList.remove("shake-animation"), 500);
        }
    });
});