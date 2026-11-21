(() => {
    "use strict";

    document.querySelectorAll("[data-password-toggle]").forEach((button) => {
        button.addEventListener("click", () => {
            const shell = button.closest(".input-shell");
            const input = shell?.querySelector("[data-password-input]");
            if (!(input instanceof HTMLInputElement)) return;

            const willShow = input.type === "password";
            input.type = willShow ? "text" : "password";
            button.setAttribute("aria-pressed", String(willShow));
            button.setAttribute("aria-label", willShow ? "Ẩn mật khẩu" : "Hiện mật khẩu");
            input.focus({ preventScroll: true });
        });
    });

    document.querySelectorAll("[data-password-strength]").forEach((input) => {
        const checklist = input.closest(".field-group")?.querySelector("[data-password-checklist]");
        if (!(input instanceof HTMLInputElement) || !checklist) return;

        const update = () => {
            const value = input.value;
            checklist.querySelector('[data-check="length"]')?.classList.toggle("is-valid", value.length >= 12);
            checklist.querySelector('[data-check="unique"]')?.classList.toggle("is-valid", new Set(value).size >= 3);
        };
        input.addEventListener("input", update);
        update();
    });

    document.querySelectorAll("[data-secure-form]").forEach((form) => {
        form.addEventListener("submit", (event) => {
            window.requestAnimationFrame(() => {
                if (event.defaultPrevented || form.matches(":invalid")) return;
                const button = form.querySelector("[data-submit-button]");
                const status = form.querySelector("[data-form-status]");
                if (button instanceof HTMLButtonElement) {
                    button.disabled = true;
                    button.classList.add("is-loading");
                    button.setAttribute("aria-busy", "true");
                }
                if (status instanceof HTMLElement) status.hidden = false;
            });
        });
    });

    const closeToast = (toast) => {
        if (!(toast instanceof HTMLElement)) return;
        toast.style.opacity = "0";
        toast.style.transform = "translateX(16px)";
        window.setTimeout(() => toast.remove(), 220);
    };
    document.querySelectorAll("[data-toast-close]").forEach((button) => {
        button.addEventListener("click", () => closeToast(button.closest(".app-toast")));
    });
    document.querySelectorAll('[data-auto-dismiss="true"]').forEach((toast) => {
        window.setTimeout(() => closeToast(toast), 6500);
    });
})();
