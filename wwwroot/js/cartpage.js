function checkout() {

    fetch('/Package/Checkout', {
        method: 'POST'
    })
        .then(res => res.json())
        .then(data => {

            if (data.success) {
                alert("تم إنشاء الطلب بنجاح 🎉");

                window.location.href = '/Home/Checkout?orderId=' + data.orderId;
            } else {
                alert(data.message);
            }

        });

}