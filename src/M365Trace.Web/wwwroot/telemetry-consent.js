(() => {
    const selector = "dialog[data-telemetry-consent]";

    function initializeConsentDialog(dialog) {
        if (!(dialog instanceof HTMLDialogElement)
            || dialog.dataset.initialized === "true") {
            return;
        }

        dialog.dataset.initialized = "true";
        dialog.addEventListener("cancel", event => event.preventDefault());
        if (!dialog.open) {
            dialog.showModal();
        }

        requestAnimationFrame(() => {
            dialog.querySelector("button")?.focus();
        });
    }

    function initializeConsentDialogs() {
        document.querySelectorAll(selector).forEach(initializeConsentDialog);
    }

    initializeConsentDialogs();
    new MutationObserver(initializeConsentDialogs).observe(
        document.body,
        { childList: true, subtree: true });
})();
