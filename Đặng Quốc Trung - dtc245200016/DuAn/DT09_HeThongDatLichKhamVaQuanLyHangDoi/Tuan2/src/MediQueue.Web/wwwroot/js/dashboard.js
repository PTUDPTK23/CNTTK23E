(() => {
    "use strict";

    const shell = document.querySelector("[data-dashboard-shell]");
    const sidebar = document.querySelector("[data-sidebar]");
    const openButton = document.querySelector("[data-sidebar-open]");
    const closeButton = document.querySelector("[data-sidebar-close]");
    const overlay = document.querySelector("[data-sidebar-overlay]");
    const collapseButton = document.querySelector("[data-sidebar-collapse]");
    if (!(shell instanceof HTMLElement) || !(sidebar instanceof HTMLElement)) return;

    const storageKey = "mediqueue.ui.sidebar-collapsed";
    let returnFocus = null;

    try {
        const collapsed = window.localStorage.getItem(storageKey) === "true";
        shell.classList.toggle("sidebar-is-collapsed", collapsed);
        collapseButton?.setAttribute("aria-pressed", String(collapsed));
        collapseButton?.setAttribute("aria-label", collapsed ? "Mở rộng menu" : "Thu gọn menu");
    } catch {
        // Storage is optional: the navigation remains fully functional without it.
    }

    const focusableSelector = 'a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])';
    const openSidebar = () => {
        returnFocus = document.activeElement;
        shell.classList.add("sidebar-is-open");
        openButton?.setAttribute("aria-expanded", "true");
        window.requestAnimationFrame(() => sidebar.querySelector(focusableSelector)?.focus());
    };
    const closeSidebar = () => {
        shell.classList.remove("sidebar-is-open");
        openButton?.setAttribute("aria-expanded", "false");
        if (returnFocus instanceof HTMLElement) returnFocus.focus({ preventScroll: true });
    };

    openButton?.addEventListener("click", openSidebar);
    closeButton?.addEventListener("click", closeSidebar);
    overlay?.addEventListener("click", closeSidebar);
    collapseButton?.addEventListener("click", () => {
        const collapsed = shell.classList.toggle("sidebar-is-collapsed");
        collapseButton.setAttribute("aria-pressed", String(collapsed));
        collapseButton.setAttribute("aria-label", collapsed ? "Mở rộng menu" : "Thu gọn menu");
        try { window.localStorage.setItem(storageKey, String(collapsed)); } catch { }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && shell.classList.contains("sidebar-is-open")) closeSidebar();
        if (event.key !== "Tab" || !shell.classList.contains("sidebar-is-open")) return;
        const focusable = [...sidebar.querySelectorAll(focusableSelector)].filter((item) => item instanceof HTMLElement);
        if (focusable.length === 0) return;
        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });

    window.addEventListener("resize", () => {
        if (window.innerWidth >= 992 && shell.classList.contains("sidebar-is-open")) closeSidebar();
    });
})();
