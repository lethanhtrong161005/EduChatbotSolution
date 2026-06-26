// --- 1. View Mode Toggling ---
function setViewMode(mode) {
    const btnFormatted = document.getElementById("btn-formatted-view");
    const btnText = document.getElementById("btn-text-view");

    const pdfFormatted = document.getElementById("pdf-viewer-section");
    const pdfText = document.getElementById("pdf-text-viewer");

    const docxFormatted = document.getElementById("docx-formatted-viewer");
    const docxText = document.getElementById("docx-text-viewer");

    const pptxFormatted = document.getElementById("pptx-formatted-viewer");
    const pptxText = document.getElementById("pptx-text-viewer");

    if (mode === 'formatted') {
        btnFormatted?.classList.add("active");
        btnText?.classList.remove("active");

        docxFormatted?.classList.remove("d-none");
        docxText?.classList.add("d-none");

        pptxFormatted?.classList.remove("d-none");
        pptxText?.classList.add("d-none");

        pdfFormatted?.classList.remove("d-none");
        pdfText?.classList.add("d-none");
    } else {
        btnFormatted?.classList.remove("active");
        btnText?.classList.add("active");

        docxFormatted?.classList.add("d-none");
        docxText?.classList.remove("d-none");

        pptxFormatted?.classList.add("d-none");
        pptxText?.classList.remove("d-none");

        pdfFormatted?.classList.add("d-none");
        pdfText?.classList.remove("d-none");
    }
}

// --- 2. Chunk Drawer Toggling ---
function toggleChunkDrawer(show) {
    const drawer = document.getElementById("chunkShowcase");
    const backdrop = document.getElementById("chunk-drawer-backdrop");
    if (show) {
        drawer.classList.add("open");
        backdrop.classList.add("open");
    } else {
        drawer.classList.remove("open");
        backdrop.classList.remove("open");
    }
}

// --- 3. Word Document Rendering (DOCX) ---
if (docType === "DOCX") {
    const renderContainer = document.getElementById("docx-render-target");

    fetch(docFilePath)
        .then(res => {
            if (!res.ok) throw new Error("File not found");
            return res.arrayBuffer();
        })
        .then(arrayBuffer => {
            renderContainer.innerHTML = ""; // Clear loader
            docx.renderAsync(arrayBuffer, renderContainer)
                .catch(err => {
                    console.error("docx-preview failed:", err);
                    showDocxTextFallback();
                });
        })
        .catch(err => {
            console.error("fetch failed for docx:", err);
            showDocxTextFallback();
        });
}

function showDocxTextFallback() {
    const renderContainer = document.getElementById("docx-render-target");
    renderContainer.innerHTML = `
    <div class="alert alert-warning">
    <i class="fas fa-exclamation-triangle me-2"></i> Không thể hiển thị bản xem trước có định dạng. Hệ thống đã tự động chuyển sang chế độ hiển thị văn bản trần.
    </div>
    `;
    setViewMode('text');
}

// --- 4. PowerPoint Slide Presentation Viewer (PPTX) ---
let currentSlideIdx = 0;

if (docType === "PPTX") {
    renderPPTXSlides();
}

function renderPPTXSlides() {
    const sidebar = document.getElementById("slides-thumbnail-sidebar");
    if (!sidebar) return;

    sidebar.innerHTML = "";
    if (!pptxSlides || pptxSlides.length === 0) {
        sidebar.innerHTML = "<div class='text-muted' style='font-size:0.8rem;'>Không có slide nào.</div>";
        document.getElementById("active-slide-title").textContent = "Nội dung trống";
        document.getElementById("active-slide-body").innerHTML = "<p class='text-muted fst-italic'>Slide này không có nội dung văn bản.</p>";
        document.getElementById("slide-progress-indicator").textContent = "Slide 0 / 0";
        return;
    }

    pptxSlides.forEach((slide, idx) => {
        const thumb = document.createElement("div");
        thumb.className = `slide-thumb ${idx === 0 ? 'active' : ''}`;
        thumb.setAttribute("onclick", `goToSlide(${idx})`);
        thumb.innerHTML = `<div>Slide ${idx + 1}</div><div style="font-size:0.6rem; opacity:0.7; font-weight:normal; max-width:140px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;">${escapeHtml(slide.sectionTitle || 'Không có tiêu đề')}</div>`;
        sidebar.appendChild(thumb);
    });

    displaySlide(0);
}

function displaySlide(idx) {
    if (!pptxSlides || pptxSlides.length === 0) return;
    currentSlideIdx = idx;

    const thumbs = document.querySelectorAll(".slide-thumb");
    thumbs.forEach((thumb, i) => {
        if (i === idx) thumb.classList.add("active");
        else thumb.classList.remove("active");
    });

    const slide = pptxSlides[idx];
    document.getElementById("active-slide-title").textContent = slide.sectionTitle || `Slide ${idx + 1}`;

    const bodyEl = document.getElementById("active-slide-body");
    if (slide.text) {
        const points = slide.text.split('\n');
        let html = "<ul>";
        points.forEach(point => {
            const cleanPoint = point.trim().replace(/^[-*•]\s*/, '');
            if (cleanPoint) {
                html += `<li>${escapeHtml(cleanPoint)}</li>`;
            }
        });
        html += "</ul>";
        bodyEl.innerHTML = html;
    } else {
        bodyEl.innerHTML = `<p class="text-muted fst-italic">Slide này không có nội dung văn bản trích xuất.</p>`;
    }

    document.getElementById("slide-progress-indicator").textContent = `Slide ${idx + 1} / ${pptxSlides.length}`;
    document.getElementById("btn-prev-slide").disabled = (idx === 0);
    document.getElementById("btn-next-slide").disabled = (idx === pptxSlides.length - 1);
}

// Previous slides control
function prevSlide() {
    if (currentSlideIdx > 0) {
        displaySlide(currentSlideIdx - 1);
    }
}

// Next slides control
function nextSlide() {
    if (pptxSlides && currentSlideIdx < pptxSlides.length - 1) {
        displaySlide(currentSlideIdx + 1);
    }
}

function goToSlide(idx) {
    displaySlide(idx);
}

function toggleFullscreenSlide() {
    const slideBox = document.getElementById("slide-presentation-box");
    if (!document.fullscreenElement) {
        slideBox.requestFullscreen().catch(err => {
            console.error("Fullscreen error:", err);
        });
    } else {
        document.exitFullscreen();
    }
}

// --- 5. Plain Text Line-By-Line Renderer (TXT) ---
if (docType === "TXT") {
    const textLinesContainer = document.getElementById("txt-lines-container");
    if (textLinesContainer) {
        const lines = rawTextContent ? rawTextContent.split('\n') : ["Tài liệu không có nội dung văn bản."];
        let html = "";
        lines.forEach((line, idx) => {
            html += `
    <div class="text-line">
    <span class="line-number">${idx + 1}</span>
    <span class="line-text">${escapeHtml(line)}</span>
    </div>
    `;
        });
        textLinesContainer.innerHTML = html;
    }
}

function searchInTextViewer(query) {
    const cleanQuery = query.trim();
    const lineTexts = document.querySelectorAll(".line-text");
    const countBadge = document.getElementById("txt-search-count");

    if (!cleanQuery) {
        lineTexts.forEach(el => { el.innerHTML = escapeHtml(el.textContent); });
        countBadge.classList.add("d-none");
        return;
    }

    const regex = new RegExp(`(${escapeRegExp(cleanQuery)})`, "gi");
    let matches = 0;

    lineTexts.forEach(el => {
        const originalText = el.textContent;
        if (originalText.toLowerCase().includes(cleanQuery.toLowerCase())) {
            matches += (originalText.toLowerCase().match(new RegExp(escapeRegExp(cleanQuery.toLowerCase()), "g")) || []).length;
            el.innerHTML = escapeHtml(originalText).replace(regex, `<mark class="search-highlight">$1</mark>`);
        } else {
            el.innerHTML = escapeHtml(originalText);
        }
    });

    countBadge.textContent = `${matches} matches`;
    countBadge.classList.remove("d-none");
}

// --- 6. Helper Functions ---
function escapeHtml(text) {
    if (!text) return "";
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

// Regex escape helper
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

var chunkList;

function renderStatusBadge(status) {

    const settings = StatusSettings[status];

    return `
        <span class="document-status-badge ${settings.className}">
            <i class="fas ${settings.iconClass}"></i>
            ${settings.text.replace(" ({{PROGRESS}}%)", "")}
        </span>
    `;
}

document.addEventListener(
    "DOMContentLoaded",
    () => {

        const badge = document.getElementById("documentStatusBadge");
        badge.innerHTML = renderStatusBadge(badge.dataset.status);

        let currentChunkPage = 1;

        const chunkList =
            document.getElementById("chunkList");

        const chunkPagination =
            document.getElementById("chunkPagination");

        const documentId =
            document.getElementById("chunkShowcase")
                .dataset.docId;

        loadChunks(documentId);

        async function loadChunks(documentId, pageIndex = 1) {

            const response = await fetch(
                `/documents/details/${documentId}?handler=GetChunks&pageIndex=${pageIndex}`);

            const page =
                await response.json();

            renderChunks(page.chunks);

            renderPagination(
                page.pageIndex,
                page.totalPages);

            currentChunkPage =
                page.pageIndex;
        }

        function renderChunks(chunks) {

            chunkList.innerHTML = "";

            for (const chunk of chunks) {

                const vectorText =
                    buildVectorPreview(chunk.vectorPreview);

                chunkList.insertAdjacentHTML(
                    "beforeend",
                    `
            <div class="chunk-row">

                <div class="chunk-text-panel">

                    <div class="chunk-meta">

                        <span class="chunk-index">
                            Chunk #${chunk.chunkIndex}
                        </span>

                        ${chunk.pageNumber
                        ? `
                            <span class="page-number">
                                Page ${chunk.pageNumber}
                            </span>
                            `
                        : ""}

                        ${chunk.sectionTitle
                        ? `
                            <span class="section-title">
                                ${escapeHtml(chunk.sectionTitle)}
                            </span>
                            `
                        : ""}

                        ${chunk.tokenCount
                        ? `
                            <span class="token-count">
                                ${chunk.tokenCount} tokens
                            </span>
                            `
                        : ""}

                    </div>

                    <pre class="chunk-text">
${escapeHtml(chunk.chunkText)}
                    </pre>

                </div>

                <div class="vector-panel">

                    <div class="vector-header">
                        Embedding Preview
                    </div>

                    <pre class="vector-preview">
${vectorText}
                    </pre>

                    <div class="vector-meta">

                        ${chunk.embeddingModel}
                        ·
                        ${chunk.tokenCount ?? "???"}
                        tokens

                    </div>

                </div>

            </div>
            `);
            }
        }

        function buildVectorPreview(vector) {

            const rows = [];

            for (let i = 0; i < vector.length; i += 4) {

                rows.push(
                    vector
                        .slice(i, i + 4)
                        .map(e => e.toFixed(3).padStart(7))
                        .join(", "));
            }

            if (rows.length > 0) {

                rows[rows.length - 1] += ", ...";
            }

            return (
                "(\n" +
                rows.join("\n") +
                "\n)"
            );
        }

        function renderPagination(
            pageIndex,
            totalPages) {

            chunkPagination.innerHTML = "";

            const side = 2;

            let left = pageIndex - side;
            let right = pageIndex + side;

            if (left < 1) {
                left = 1;
                right = Math.min(totalPages, 5);
            }

            if (right > totalPages) {
                right = totalPages;
                left = Math.max(1, totalPages - 4);
            }

            addPageButton(
                "First",
                1,
                pageIndex === 1);

            addPageButton(
                "Prev",
                pageIndex - 1,
                pageIndex === 1);

            if (left > 1) {

                addPageButton(
                    "1",
                    1,
                    false);

                if (left > 2) {

                    chunkPagination.insertAdjacentHTML(
                        "beforeend",
                        `<span class="px-2">...</span>`);
                }
            }

            for (let i = left; i <= right; i++) {

                addPageButton(
                    i,
                    i,
                    false,
                    i === pageIndex);
            }

            if (right < totalPages) {

                if (right < totalPages - 1) {

                    chunkPagination.insertAdjacentHTML(
                        "beforeend",
                        `<span class="px-2">...</span>`);
                }

                addPageButton(
                    totalPages,
                    totalPages,
                    false);
            }

            addPageButton(
                "Next",
                pageIndex + 1,
                pageIndex === totalPages);

            addPageButton(
                "Last",
                totalPages,
                pageIndex === totalPages);
        }

        function addPageButton(
            text,
            page,
            disabled,
            active = false) {

            const btn =
                document.createElement("button");

            btn.className =
                "chunk-page-btn";

            if (active)
                btn.classList.add("active");

            if (disabled)
                btn.classList.add("disabled");

            btn.textContent =
                text;

            btn.addEventListener(
                "click",
                () => loadChunks(
                    documentId,
                    page));

            chunkPagination.appendChild(btn);
        }

        function escapeHtml(text) {

            const div =
                document.createElement("div");

            div.textContent = text;

            return div.innerHTML;
        }

    });

$(document).on(
    "resource:changed",
    async function (_, resUpd) {
        switch (resUpd.resourceType) {
            case "document":
                if (resUpd.resourceId === DocumentDetailsPage.documentId)
                    promptReload();
                break;
            case "chapter":
                if (resUpd.resourceId === DocumentDetailsPage.chapterId)
                    promptReload();
                break;
            case "subject":
                if (resUpd.resourceId === DocumentDetailsPage.subjectId)
                    promptReload();
                break;
            case "user":
                if (resUpd.resourceId === DocumentDetailsPage.uploaderId)
                    promptReload();
                break;
            case "membership":
                if (resUpd.action === "deleted"
                    && resUpd.properties["subjectId"] === DocumentDetailsPage.subjectId
                    && resUpd.properties["userId"] === DocumentDetailsPage.userId) {
                    denyAccess();
                } else {
                    // loadComments();
                }
                break;
            case "comment":
                // loadComments()
                break;
        }
    }
);

function promptReload() {

    if (confirm("This document has been modified. Refresh?"))
        window.location.reload();
}

function denyAccess() {

    alert("Sorry for the inconvenience. You no longer have access to this document.");
    window.location.href = "/documents/library";
}
