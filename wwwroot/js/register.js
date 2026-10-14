document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("registerForm");
    const roleSelect = document.getElementById("roleSelect");
    const vendorFields = document.getElementById("vendorFields");
    const phoneInput = document.getElementById("phoneInput");
    const phoneError = document.getElementById("phoneError");
    const merchantNameInput = document.getElementById("merchantNameInput");
    const submitBtn = form ? form.querySelector("button[type='submit']") : null;

    if (!form) return; // تأمين الكود لو الصفحة مش هي صفحة التسجيل

    roleSelect.addEventListener("change", function () {
        if (this.value === "Vendor") {
            vendorFields.style.display = "block";
            if (merchantNameInput) {
                merchantNameInput.required = true; // إجباري فقط للـ Vendor
            }
        } else {
            vendorFields.style.display = "none";
            if (merchantNameInput) {
                merchantNameInput.required = false; // إلغاء الإجبار فوراً للمشتري
                merchantNameInput.value = "";       // تصفير القيمة منعاً لأي تعارض
            }
        }
    });

    // 3. منع كتابة أي حروف عدا الأرقام في حقل الهاتف أثناء الكتابة
    phoneInput.addEventListener("input", function () {
        this.value = this.value.replace(/[^0-9]/g, '');
    });

    // 4. التحقق من رقم الهاتف وحركة الزرار عند الإرسال (Submit)
    form.addEventListener("submit", function (e) {
        const phoneValue = phoneInput.value;
        const phonePattern = /^(010|011|012|015)[0-9]{8}$/;

        // أولاً: تشيك على رقم الهاتف
        if (!phonePattern.test(phoneValue)) {
            e.preventDefault(); // وقف الإرسال
            phoneError.style.display = "block";
            phoneInput.classList.add("is-invalid");
            phoneInput.focus();

            // حركة الهز التنبيهية للزرار
            if (submitBtn) {
                submitBtn.classList.add("shake-animation");
                setTimeout(() => submitBtn.classList.remove("shake-animation"), 500);
            }
            return;
        } else {
            phoneError.style.display = "none";
            phoneInput.classList.remove("is-invalid");
        }

        // ثانياً: لو البيانات كلها تمام (بما فيها الـ validation بتاع الـ .NET لو مستخدم)
        // بنحول الزرار لشكل الـ Loading الشيك
        if (submitBtn) {
            // بنستعمل setTimeout خفيف عشان نضمن إن الـ Browser يلحق يعمل Submit فعلي قبل قفل الزرار
            setTimeout(() => {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> جاري إنشاء حسابك...';
            }, 50);
        }
    });
    console.log(roleSelect.value);
    console.log("Jahhezni Signup Script Ready & Synced!");
});

// إضافة الـ CSS الخاص بحركة الهز يدوياً
if (!document.getElementById("shake-style")) {
    const style = document.createElement('style');
    style.id = "shake-style";
    style.innerHTML = `
        @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-5px); }
            75% { transform: translateX(5px); }
        }
        .shake-animation { animation: shake 0.2s ease-in-out 0s 2; }
    `;
    document.head.appendChild(style);
}