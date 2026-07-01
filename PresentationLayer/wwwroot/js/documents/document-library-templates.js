"use strict";

const DocumentLibraryTemplates = (() => {

    /* ==========================================================
       HTML HELPERS
       ========================================================== */

    function escapeHtml(value) {

        if (value === null || value === undefined)
            return "";

        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    }

    function escapeAttribute(value) {

        return escapeHtml(value);
    }

    function formatChapterTitle(chapter) {

        if (chapter.chapterNumber == null)
            return escapeHtml(chapter.name);

        return `${chapter.chapterNumber}. ${escapeHtml(chapter.name)}`;
    }

    /* ==========================================================
       EMPTY / LOADING
       ========================================================== */

    function renderLoading(message = "Loading...") {

        return `
<div class="flex items-center justify-center py-20 text-slate-500">
    <div class="flex items-center gap-3">
        <i class="fa-solid fa-spinner animate-spin"></i>
        <span>${escapeHtml(message)}</span>
    </div>
</div>`;
    }

    function renderEmpty(
        title,
        description,
        icon = "fa-folder-open") {

        return `
<div class="flex flex-col items-center justify-center rounded-xl border border-dashed border-slate-300 bg-slate-50 py-20">

    <i class="fa-solid ${icon} mb-4 text-4xl text-slate-400"></i>

    <h3 class="text-lg font-semibold text-slate-700">
        ${escapeHtml(title)}
    </h3>

    <p class="mt-2 max-w-lg text-center text-sm text-slate-500">
        ${escapeHtml(description)}
    </p>

</div>`;
    }

    /* ==========================================================
       SUBJECT GRID
       ========================================================== */

    function renderSubjectGrid(
        subjects,
        selectedSubjectId) {

        if (!subjects?.length) {

            return renderEmpty(
                "No subjects",
                "You are not currently assigned to any subjects.");
        }

        return `
<div class="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">

${subjects.map(subject => {

            const selected =
                subject.id === selectedSubjectId;

            return `
<button
    type="button"
    class="
        js-select-subject
        group
        flex
        flex-col
        rounded-2xl
        border
        bg-white
        p-6
        text-left
        shadow-sm
        transition
        hover:-translate-y-0.5
        hover:border-blue-300
        hover:shadow-lg
        ${selected
                    ? "border-blue-500 ring-2 ring-blue-200"
                    : "border-slate-200"}
    "
    data-subject-id="${subject.id}">

    <div class="flex items-start justify-between">

        <div>

            <div class="text-xs font-semibold tracking-wider text-blue-600 uppercase">
                ${escapeHtml(subject.code)}
            </div>

            <h3 class="mt-2 text-xl font-semibold text-slate-900">
                ${escapeHtml(subject.name)}
            </h3>

        </div>

        <i class="
            fa-solid
            fa-book
            text-3xl
            ${selected
                    ? "text-blue-600"
                    : "text-slate-300 group-hover:text-blue-500"}
        "></i>

    </div>

</button>`;

        }).join("")}

</div>`;
    }

    /* ==========================================================
       SUBJECT SIDEBAR
       ========================================================== */

    function renderSubjectSidebar(
        subjects,
        selectedSubjectId,
        collapsed) {

        if (!collapsed)
            return "";

        return `
<div class="flex h-full flex-col">

    <div class="border-b border-slate-200 px-4 py-3">

        <div class="text-xs font-semibold tracking-wider text-slate-500 uppercase">
            Subjects
        </div>

    </div>

    <div class="flex-1 overflow-y-auto">

${subjects.map(subject => {

            const selected =
                subject.id === selectedSubjectId;

            return `
<button
    type="button"
    class="
        js-select-subject
        flex
        w-full
        flex-col
        border-l-4
        px-4
        py-3
        text-left
        transition
        hover:bg-slate-50
        ${selected
                    ? "border-blue-600 bg-blue-50"
                    : "border-transparent"}
    "
    data-subject-id="${subject.id}">

    <span class="
        text-xs
        font-semibold
        uppercase
        tracking-wider
        ${selected
                    ? "text-blue-600"
                    : "text-slate-500"}
    ">
        ${escapeHtml(subject.code)}
    </span>

    <span class="
        mt-1
        line-clamp-2
        text-sm
        font-medium
        ${selected
                    ? "text-slate-900"
                    : "text-slate-700"}
    ">
        ${escapeHtml(subject.name)}
    </span>

</button>`;

        }).join("")}

    </div>

</div>`;
    }

    /* ==========================================================
       CHAPTER SIDEBAR
       ========================================================== */

    function renderChapterSidebar(
        chapters,
        selectedChapterId,
        visible) {

        if (!visible)
            return "";

        return `
<div class="flex h-full flex-col">

    <div class="border-b border-slate-200 px-4 py-3">

        <div class="text-xs font-semibold tracking-wider text-slate-500 uppercase">
            Chapters
        </div>

    </div>

    <div class="flex-1 overflow-y-auto">

<button
    type="button"
    class="
        js-select-chapter
        flex
        w-full
        items-center
        gap-3
        border-l-4
        px-4
        py-3
        text-left
        transition
        hover:bg-slate-50
        ${selectedChapterId == null
                ? "border-blue-600 bg-blue-50"
                : "border-transparent"}
    "
    data-chapter-id="">

    <i class="fa-solid fa-book-open text-slate-400"></i>

    <span class="font-medium">
        Subject Overview
    </span>

</button>

${chapters.map(chapter => {

                    const selected =
                        chapter.id === selectedChapterId;

                    return `
<button
    type="button"
    class="
        js-select-chapter
        flex
        w-full
        items-start
        gap-3
        border-l-4
        px-4
        py-3
        text-left
        transition
        hover:bg-slate-50
        ${selected
                            ? "border-blue-600 bg-blue-50"
                            : "border-transparent"}
    "
    data-chapter-id="${chapter.id}">

    <div class="
        mt-0.5
        flex
        h-7
        w-7
        shrink-0
        items-center
        justify-center
        rounded-full
        bg-slate-100
        text-xs
        font-semibold
        text-slate-600">

        ${chapter.chapterNumber ?? "•"}

    </div>

    <div class="min-w-0 flex-1">

        <div class="
            truncate
            text-sm
            font-medium
            ${selected
                            ? "text-slate-900"
                            : "text-slate-700"}
        ">
            ${formatChapterTitle(chapter)}
        </div>

    </div>

</button>`;

                }).join("")}

    </div>

</div>`;
    }

    /* =========================================================
       DETAILS CARD SWITCH
       ========================================================= */

    function renderDetailsCard(subject, chapter, canUpload) {

        const details =
            chapter
                ? renderChapterDetailsCard(chapter, subject, canUpload)
                : renderSubjectDetailsCard(subject, canUpload);

        const upload =
            canUpload
                ? renderUploadPanel(true)
                : "";

        return details + upload;
    }

    /* =========================================================
       SUBJECT DETAILS CARD
       ========================================================= */

    function renderSubjectDetailsCard(subject, canUpload) {

        if (!subject)
            return "";

        return `
<section class="rounded-xl border border-slate-200 bg-white shadow-sm">

    <div class="flex items-start justify-between border-b border-slate-200 px-6 py-5">

        <div>

            <div class="flex items-center gap-3">

                <span class="rounded-lg bg-emerald-100 px-3 py-1 text-sm font-semibold text-emerald-700">
                    ${escapeHtml(subject.code)}
                </span>

                <h2 class="text-2xl font-bold text-slate-900">
                    ${escapeHtml(subject.name)}
                </h2>

            </div>

            ${subject.description
                ? `
                <p class="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
                    ${escapeHtml(subject.description)}
                </p>
                `
                : ""}

        </div>

        ${canUpload
                ? `
            <button
                id="showUploadBtn"
                class="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-700">

                <i class="fas fa-cloud-upload-alt"></i>
                Upload documents

            </button>
            `
                : ""}

    </div>

    <div class="grid gap-6 p-6 lg:grid-cols-3">

        <div class="rounded-lg bg-slate-50 p-4">

            <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Chapters
            </div>

            <div class="mt-2 text-3xl font-bold text-slate-900">
                ${subject.chapterCount ?? 0}
            </div>

        </div>

        <div class="rounded-lg bg-slate-50 p-4">

            <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Documents
            </div>

            <div class="mt-2 text-3xl font-bold text-slate-900">
                ${subject.documentCount ?? 0}
            </div>

        </div>

        <div class="rounded-lg bg-slate-50 p-4">

            <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Members
            </div>

            <div class="mt-2 text-3xl font-bold text-slate-900">
                ${subject.memberCount ?? 0}
            </div>

        </div>

    </div>

</section>
`;
    }

    /* =========================================================
       CHAPTER DETAILS CARD
       ========================================================= */

    function renderChapterDetailsCard(chapter, subject, canUpload) {

        if (!chapter)
            return "";

        return `
<section class="rounded-xl border border-slate-200 bg-white shadow-sm">

    <div class="flex items-start justify-between border-b border-slate-200 px-6 py-5">

        <div>

            <div class="flex items-center gap-3">

                ${chapter.chapterNumber != null
                ? `
                    <span class="rounded-lg bg-blue-100 px-3 py-1 text-sm font-semibold text-blue-700">
                        Chapter ${chapter.chapterNumber}
                    </span>
                    `
                : ""}

                <h2 class="text-2xl font-bold text-slate-900">
                    ${escapeHtml(chapter.name)}
                </h2>

            </div>

            <div class="mt-2 text-sm text-slate-500">

                ${escapeHtml(subject.code)}
                ·
                ${escapeHtml(subject.name)}

            </div>

            ${chapter.description
                ? `
                <p class="mt-4 max-w-3xl text-sm leading-6 text-slate-600">
                    ${escapeHtml(chapter.description)}
                </p>
                `
                : ""}

        </div>

        ${canUpload
                ? `
            <button
                id="showUploadBtn"
                class="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-700">

                <i class="fas fa-cloud-upload-alt"></i>
                Upload documents

            </button>
            `
                : ""}

    </div>

    <div class="grid gap-6 p-6 lg:grid-cols-2">

        <div class="rounded-lg bg-slate-50 p-4">

            <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Documents
            </div>

            <div class="mt-2 text-3xl font-bold text-slate-900">
                ${chapter.documentCount ?? 0}
            </div>

        </div>

        <div class="rounded-lg bg-slate-50 p-4">

            <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Subject
            </div>

            <div class="mt-2 text-lg font-semibold text-slate-900">
                ${escapeHtml(subject.code)}
            </div>

        </div>

    </div>

</section>
`;
    }

    /* =========================================================
       UPLOAD PANEL
       ========================================================= */

    function renderUploadPanel(showPanel) {

        return `
<div id="uploadPanelContainer"
     class="${showPanel ? "" : "hidden"} mt-6">

    <div id="uploadPanelOverlay"
         class="hidden absolute inset-0 z-20 flex items-center justify-center rounded-xl bg-white/70 backdrop-blur-sm">

        <div class="h-10 w-10 animate-spin rounded-full border-4 border-emerald-600 border-t-transparent">
        </div>

    </div>

    <section id="uploadPanel"
             class="rounded-xl border border-slate-200 bg-white shadow-sm">

        <div class="p-6">

            <div id="dropZone"
                 class="rounded-xl border-2 border-dashed border-emerald-300 bg-emerald-50/40 px-8 py-12 text-center transition hover:border-emerald-500">

                <i class="fas fa-cloud-upload-alt text-5xl text-emerald-600"></i>

                <h3 class="mt-5 text-xl font-semibold text-slate-900">
                    Drag files here
                </h3>

                <p class="mt-2 text-sm text-slate-500">
                    PDF · DOCX · PPTX · TXT · HTML
                </p>

                <input id="fileInput"
                       hidden
                       multiple
                       type="file"
                       accept=".pdf,.docx,.pptx,.txt,.html" />

                <button id="browseBtn"
                        class="mt-6 rounded-lg bg-emerald-600 px-5 py-2.5 font-medium text-white transition hover:bg-emerald-700">

                    Browse files

                </button>

            </div>

            <div id="uploadQueue"
                 class="mt-6 space-y-3">
            </div>

        </div>

    </section>

</div>
`;
    }

    /* =========================================================
       SHARED UI FRAGMENTS
       ========================================================= */

    function renderStat(label, value) {

        return `
<div class="rounded-lg bg-slate-50 p-4">

    <div class="text-xs font-semibold uppercase tracking-wide text-slate-500">
        ${escapeHtml(label)}
    </div>

    <div class="mt-2 text-2xl font-bold text-slate-900">
        ${escapeHtml(value)}
    </div>

</div>
`;
    }

    function renderSectionHeading(title, subtitle = "") {

        return `
<div class="border-b border-slate-200 px-6 py-4">

    <h2 class="text-lg font-semibold text-slate-900">
        ${escapeHtml(title)}
    </h2>

    ${subtitle
                ? `
        <p class="mt-1 text-sm text-slate-500">
            ${escapeHtml(subtitle)}
        </p>
        `
                : ""}

</div>
`;
    }

    /* =========================================================
   DOCUMENT DRAWER
   ========================================================= */

    function renderDocumentDrawer(documents, totalDocuments) {

        if (!documents?.length) {

            return `
<section class="mt-6 rounded-xl border border-slate-200 bg-white shadow-sm">

    ${renderSectionHeading(
                "Documents",
                "No documents match the current filters.")}

    <div class="p-12">

        ${renderEmpty(
                    "No documents found",
                    "Try changing the selected chapter or search term.",
                    "fa-file")}

    </div>

</section>`;
        }

        return `
<section class="mt-6 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">

    ${renderSectionHeading(
            "Documents",
            `${totalDocuments} document${totalDocuments === 1 ? "" : "s"}`)}

    <div class="divide-y divide-slate-200">

        ${documents.map(renderDocumentRow).join("")}

    </div>

</section>`;
    }

    /* =========================================================
       DOCUMENT ROW
       ========================================================= */

    function renderDocumentRow(document) {

        return `
<div class="flex items-center justify-between px-6 py-4 transition hover:bg-slate-50">

    <div class="flex min-w-0 items-center gap-4">

        <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-xl text-slate-500">

            <i class="fas ${getFileIcon(document.extension)}"></i>

        </div>

        <div class="min-w-0">

            <div class="flex flex-wrap items-center gap-3">

                <a href="/documents/download/${document.id}"
                   class="truncate font-semibold text-slate-900 hover:text-blue-600">

                    ${escapeHtml(document.title)}${escapeHtml(document.extension)}

                </a>

                <span
                    id="status-badge-${document.id}"
                    class="document-status-badge status-${document.status.toLowerCase()}">

                    <i class="fas ${StatusSettings[document.status].iconClass}"></i>

                    ${escapeHtml(document.status)}

                </span>

            </div>

            <div class="mt-2 flex flex-wrap gap-x-5 gap-y-1 text-sm text-slate-500">

                ${document.chapterName
                ? `
                    <span>

                        <i class="fas fa-book-open mr-1"></i>

                        ${document.chapterNumber != null
                    ? `${document.chapterNumber}. `
                    : ""}

                        ${escapeHtml(document.chapterName)}

                    </span>
                    `
                : ""}

                <span>

                    <i class="fas fa-user mr-1"></i>

                    ${escapeHtml(document.uploadedBy)}

                </span>

                <span>

                    <i class="fas fa-calendar mr-1"></i>

                    ${escapeHtml(formatDate(document.uploadedAt))}

                </span>

                ${document.fileSize != null
                ? `
                    <span>

                        <i class="fas fa-hard-drive mr-1"></i>

                        ${formatFileSize(document.fileSize)}

                    </span>
                    `
                : ""}

            </div>

        </div>

    </div>

    <div class="ml-6 flex shrink-0 items-center gap-2">

        <a href="/documents/download/${document.id}"
           class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:border-blue-300 hover:text-blue-600">

            <i class="fas fa-download"></i>

        </a>

        <a href="/documents/details/${document.id}"
           class="inline-flex h-10 w-10 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition hover:border-blue-300 hover:text-blue-600">

            <i class="fas fa-eye"></i>

        </a>

        <button
            type="button"
            class="js-delete-document inline-flex h-10 w-10 items-center justify-center rounded-lg border border-red-200 text-red-500 transition hover:bg-red-50"
            data-id="${document.id}">

            <i class="fas fa-trash"></i>

        </button>

    </div>

</div>`;
    }

    /* =========================================================
       PAGINATION
       ========================================================= */

    function renderPagination(currentPage, totalPages) {

        if (!totalPages || totalPages <= 1)
            return "";

        const pages = [];

        for (let page = 1; page <= totalPages; page++) {

            pages.push(`
<button
    type="button"
    class="
        js-page
        rounded-lg
        border
        px-3
        py-2
        text-sm
        font-medium
        transition
        ${page === currentPage
                    ? "border-blue-600 bg-blue-600 text-white"
                    : "border-slate-300 bg-white text-slate-700 hover:border-blue-400 hover:text-blue-600"}
    "
    data-page="${page}">

    ${page}

</button>`);
        }

        return `
<div class="mt-6 flex items-center justify-center gap-2">

    <button
        type="button"
        class="js-page rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm transition hover:border-blue-400 disabled:cursor-not-allowed disabled:opacity-40"
        data-page="${currentPage - 1}"
        ${currentPage === 1 ? "disabled" : ""}>

        <i class="fas fa-chevron-left"></i>

    </button>

    ${pages.join("")}

    <button
        type="button"
        class="js-page rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm transition hover:border-blue-400 disabled:cursor-not-allowed disabled:opacity-40"
        data-page="${currentPage + 1}"
        ${currentPage === totalPages ? "disabled" : ""}>

        <i class="fas fa-chevron-right"></i>

    </button>

</div>`;
    }

    /* =========================================================
       PRIVATE HELPERS
       ========================================================= */

    function formatDate(date) {

        return new Intl.DateTimeFormat(
            "en-GB",
            {
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            })
            .format(new Date(date));
    }

    function formatFileSize(bytes) {

        if (bytes == null)
            return "";

        const units = ["B", "KB", "MB", "GB"];

        let value = bytes;
        let unit = 0;

        while (value >= 1024 && unit < units.length - 1) {

            value /= 1024;
            unit++;
        }

        return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
    }

    function getFileIcon(extension) {

        switch (extension?.toUpperCase()) {

            case ".PDF":
                return "fa-file-pdf";

            case ".DOCX":
                return "fa-file-word";

            case ".PPTX":
                return "fa-file-powerpoint";

            case ".TXT":
                return "fa-file-lines";

            case ".HTML":
                return "fa-file-code";

            default:
                return "fa-file";
        }
    }

    /* =========================================================
       PUBLIC API
       ========================================================= */

    return {

        renderLoading,
        renderEmpty,

        renderSubjectGrid,
        renderSubjectSidebar,
        renderChapterSidebar,

        renderDetailsCard,

        renderDocumentDrawer,
        renderPagination,
    };

})();
