document.addEventListener("DOMContentLoaded", function () {
    const passwordInput = document.getElementById("Input_Password");
    const confirmInput = document.getElementById("Input_ConfirmPassword");
    const matchMsg = document.getElementById("passwordMatch");

    const checks = {
        length: document.getElementById("req-length"),
        upper: document.getElementById("req-upper"),
        lower: document.getElementById("req-lower"),
        number: document.getElementById("req-number"),
        special: document.getElementById("req-special")
    };

    if (!passwordInput) return;

    passwordInput.addEventListener("input", () => {
        const val = passwordInput.value;
        const valid = {
            length: val.length >= 8,
            upper: /[A-Z]/.test(val),
            lower: /[a-z]/.test(val),
            number: /[0-9]/.test(val),
            special: /[!@#$%^&*(),.?":{}|<>]/.test(val)
        };

        for (const key in valid) {
            if (checks[key]) checks[key].style.color = valid[key] ? "green" : "gray";
        }
    });

    confirmInput.addEventListener("input", () => {
        if (!matchMsg) return;
        if (!confirmInput.value) {
            matchMsg.textContent = "";
            return;
        }
        if (confirmInput.value === passwordInput.value) {
            matchMsg.textContent = "✅ Passwords match";
            matchMsg.style.color = "green";
        } else {
            matchMsg.textContent = "❌ Passwords do not match";
            matchMsg.style.color = "red";
        }
    });
});
