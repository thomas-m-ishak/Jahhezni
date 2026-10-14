// وظيفة تأكيد الحذف (Delete Confirmation)
function confirmAction(message) {
    return confirm(message || "هل أنت متأكد من تنفيذ هذا الإجراء؟");
}

document.addEventListener("DOMContentLoaded", function () {
    console.log("Dashboard JS Loaded");

    // إخفاء رسائل الخطأ تلقائياً بعد 5 ثواني
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.display = 'none';
        }, 5000);
    });
});