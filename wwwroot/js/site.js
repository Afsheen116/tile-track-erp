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
