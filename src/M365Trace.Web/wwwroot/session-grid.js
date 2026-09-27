(() => {
    const focusRow = row => {
        row.focus({ preventScroll: true });
        row.scrollIntoView({ block: "nearest" });
    };

    const getRowByIndex = (grid, index) =>
        grid.querySelector(`tbody tr[data-session-index="${index}"]`);

    const focusIndex = (grid, index, select = true) => {
        const row = getRowByIndex(grid, index);
        if (row) {
            if (select) {
                row.click();
                requestAnimationFrame(() =>
                    focusRow(getRowByIndex(grid, index) ?? row));
            } else {
                focusRow(row);
            }
            return;
        }

        const container = grid.closest(".table-container");
        if (!container) {
            return;
        }

        const measuredRow = grid.querySelector(
            "tbody tr[data-session-row]");
        const rowHeight = measuredRow?.getBoundingClientRect().height
            || Number(grid.dataset.sessionRowHeight);
        const headingHeight = grid.tHead?.offsetHeight ?? 0;
        const targetOffset = headingHeight + (index * rowHeight);
        container.scrollTop = Math.max(
            0,
            targetOffset - ((container.clientHeight - rowHeight) / 2));

        const deadline = performance.now() + 5000;
        const focusWhenRendered = () => {
            const renderedRow = getRowByIndex(grid, index);
            if (renderedRow) {
                if (select) {
                    renderedRow.click();
                }
                requestAnimationFrame(() =>
                    focusRow(getRowByIndex(grid, index) ?? renderedRow));
                return;
            }

            if (performance.now() < deadline) {
                requestAnimationFrame(focusWhenRendered);
            }
        };

        requestAnimationFrame(focusWhenRendered);
    };

    document.addEventListener("pointerdown", event => {
        const row = event.target.closest?.("tr[data-session-row]");
        if (row) {
            focusRow(row);
        }
    });

    document.addEventListener("keydown", event => {
        const row = event.target.closest?.("tr[data-session-row]");
        if (row) {
            const grid = row.closest("table[data-session-grid]");
            const currentIndex = Number(row.dataset.sessionIndex);
            const sessionCount = Number(grid?.dataset.sessionCount);
            let targetIndex = currentIndex;

            switch (event.key) {
                case "ArrowUp":
                    targetIndex = Math.max(0, currentIndex - 1);
                    break;
                case "ArrowDown":
                    targetIndex = Math.min(
                        sessionCount - 1,
                        currentIndex + 1);
                    break;
                case "Home":
                    targetIndex = 0;
                    break;
                case "End":
                    targetIndex = sessionCount - 1;
                    break;
                case "ArrowRight": {
                    const detailPanel = document.querySelector(
                        "[data-session-detail]");
                    if (detailPanel) {
                        event.preventDefault();
                        detailPanel.focus({ preventScroll: true });
                        detailPanel.scrollIntoView({ block: "nearest" });
                    }
                    return;
                }
                case "Enter":
                case " ":
                    event.preventDefault();
                    row.click();
                    return;
                default:
                    return;
            }

            event.preventDefault();
            if (grid && targetIndex !== currentIndex) {
                focusIndex(grid, targetIndex);
            }
            return;
        }

        const detailPanel = event.target.closest?.("[data-session-detail]");
        if (detailPanel
            && event.target === detailPanel
            && event.key === "ArrowLeft") {
            const grid = document.querySelector(
                "table[data-session-grid]");
            const selectedIndex = Number(grid?.dataset.selectedIndex);
            if (grid && selectedIndex >= 0) {
                event.preventDefault();
                focusIndex(grid, selectedIndex, false);
            }
        }
    });
})();
