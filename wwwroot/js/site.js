// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    var navbar = document.querySelector(".app-navbar");
    if (!navbar) {
        return;
    }

    var root = document.documentElement;
    var threshold = 18;

    function syncNavbarHeight() {
        root.style.setProperty("--app-nav-height", (navbar.offsetHeight + 8) + "px");
    }

    function syncScrollState() {
        navbar.classList.toggle("is-scrolled", window.scrollY > threshold);
    }

    window.addEventListener("scroll", syncScrollState, { passive: true });
    window.addEventListener("resize", syncNavbarHeight);

    var collapsePanel = navbar.querySelector(".navbar-collapse");
    if (collapsePanel) {
        collapsePanel.addEventListener("shown.bs.collapse", syncNavbarHeight);
        collapsePanel.addEventListener("hidden.bs.collapse", syncNavbarHeight);
    }

    syncNavbarHeight();
    syncScrollState();
})();

(function () {
    var rows = document.querySelectorAll(".ledger-clickable-row[data-ledger-url]");
    if (!rows.length) {
        return;
    }

    rows.forEach(function (row) {
        function navigateToLedger() {
            var url = row.getAttribute("data-ledger-url");
            if (url) {
                window.location.href = url;
            }
        }

        row.addEventListener("click", function (event) {
            if (event.button !== 0) {
                return;
            }

            if (event.target.closest("a, button, input, select, textarea, label")) {
                return;
            }

            navigateToLedger();
        });

        row.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                navigateToLedger();
            }
        });
    });
})();

(function () {
    var body = document.body;
    if (!body) {
        return;
    }

    function resolveFunctionKey(event) {
        var key = (event.key || "").toUpperCase();
        var code = (event.code || "").toUpperCase();
        var keyCode = event.keyCode || event.which || 0;

        if (key === "F5" || code === "F5") {
            return "F5";
        }

        if (key === "F6" || code === "F6") {
            return "F6";
        }

        if (key === "F7" || code === "F7") {
            return "F7";
        }

        if (keyCode === 116) {
            return "F5";
        }

        if (keyCode === 117) {
            return "F6";
        }

        if (keyCode === 118) {
            return "F7";
        }

        return "";
    }

    var shortcutTargets = {
        F5: body.dataset.shortcutPaymentsUrl || "",
        F6: body.dataset.shortcutPurchaseUrl || "",
        F7: body.dataset.shortcutSalesUrl || ""
    };

    function handleShortcut(event) {
        if (event.altKey || event.ctrlKey || event.metaKey) {
            return;
        }

        var fnKey = resolveFunctionKey(event);
        if (!fnKey) {
            return;
        }

        var targetUrl = shortcutTargets[fnKey];
        if (!targetUrl) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        if (typeof event.stopImmediatePropagation === "function") {
            event.stopImmediatePropagation();
        }
        window.location.assign(targetUrl);
    }

    window.addEventListener("keydown", handleShortcut, true);
    window.addEventListener("keyup", handleShortcut, true);
})();
