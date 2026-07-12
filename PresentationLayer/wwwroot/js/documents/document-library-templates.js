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

    function joinClasses(...classes) {

        return classes
            .filter(Boolean)
            .join(" ");
    }

    /* ==========================================================
       FORMATTING HELPERS
       ========================================================== */

    function formatChapterTitle(chapter) {

        if (!chapter)
            return "";

        if (chapter.chapterNumber == null)
            return escapeHtml(chapter.name);

        return `Chapter ${chapter.chapterNumber} · ${escapeHtml(chapter.name)}`;
    }

    function formatDate(date) {

        if (!date)
            return "";

        return new Intl.DateTimeFormat(
            "en-GB",
            {
                dateStyle: "medium",
                timeStyle: "short",
            })
            .format(new Date(date));
    }

    function formatTimeAgo(date) {

        if (!date)
            return "";

        const diff = diffDate(new Date(), new Date(date));

        if (diff.years > 1) return `${diff.years} years ago`;
        if (diff.years === 1) return `A year ago`;
        if (diff.months > 1) return `${diff.months} months ago`;
        if (diff.months === 1) return `A month ago`;
        if (diff.days > 1) return `${diff.days} days ago`;
        if (diff.days === 1) return `A day ago`;
        if (diff.hours > 1) return `${diff.hours} hours ago`;
        if (diff.hours === 1) return `An hour ago`;
        if (diff.minutes > 1) return `${diff.minutes} minutes ago`;
        return `Just now`;
    }

    function diffDate(first, second) {
        const ms = Math.abs(first.getTime() - second.getTime());
        const seconds = Math.floor((ms / 1000) % 60);
        const minutes = Math.floor((ms / (1000 * 60)) % 60);
        const hours = Math.floor((ms / (1000 * 60 * 60)) % 24);
        const days = Math.floor((ms / (1000 * 60 * 60 * 24)) % (365.25 / 12));
        const months = Math.floor((ms / (1000 * 60 * 60 * 24 * (365.25 / 12))) % 12);
        const years = Math.floor((ms / (1000 * 60 * 60 * 24 * 365.25)));
        return {
            years,
            months,
            days,
            hours,
            minutes,
            seconds,
        };
    }

    function formatFileSize(bytes) {

        if (bytes == null)
            return "";

        const units = ["B", "KB", "MB", "GB", "TB"];

        let value = bytes;
        let unit = 0;

        while (value >= 1024 && unit < units.length - 1) {
            value /= 1024;
            unit++;
        }

        return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
    }

    function getFileIcon(extension) {

        switch ((extension ?? "").toUpperCase()) {

            case ".PDF":
                return "fa-file-pdf";

            case ".DOC":
            case ".DOCX":
                return "fa-file-word";

            case ".PPT":
            case ".PPTX":
                return "fa-file-powerpoint";

            case ".TXT":
                return "fa-file-alt";

            case ".HTML":
            case ".HTM":
                return "fa-file-code";

            default:
                return "fa-file";
        }
    }

    /* ==========================================================
       GENERIC PLACEHOLDERS
       ========================================================== */

    function renderEmpty(icon, title, description) {

        return `
<div class="flex flex-col items-center justify-center px-6 py-12 text-center">

    <div class="mb-5 flex h-20 w-20 items-center justify-center rounded-2xl bg-slate-800 text-3xl text-slate-500">

        <i class="fa-solid ${icon}"></i>

    </div>

    <h3 class="text-xl font-semibold text-white">

        ${escapeHtml(title)}

    </h3>

    <p class="mt-3 max-w-xl text-sm leading-6 text-slate-400">

        ${escapeHtml(description)}

    </p>

</div>`;
    }

    function renderLoading({
        title = "Loading...",
        description = "Please wait while we retrieve the requested data.",
        compact = false,
    } = {}) {

        if (compact) {

            return `
            <div class="flex items-center justify-center gap-3 rounded-lg border border-slate-700 bg-slate-900/60 px-3 py-5 mx-2 my-3">

                <i class="fa-solid fa-spinner fa-spin text-lg text-emerald-400"></i>

                <span class="text-sm font-medium text-slate-300">
                    ${title}
                </span>

            </div>
        `;
        }

        return `
        <div class="flex flex-col items-center justify-center rounded-3xl border border-slate-700 bg-slate-900/50 px-10 py-16 text-center">

            <div class="mb-5 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500/10">

                <i class="fa-solid fa-spinner fa-spin text-3xl text-emerald-400"></i>

            </div>

            <h3 class="text-xl font-semibold text-white">
                ${title}
            </h3>

            <p class="mt-2 max-w-md text-sm leading-relaxed text-slate-400">
                ${description}
            </p>

        </div>
    `;
    }

    function renderRetryButton(
        retryClass,
        retryLabel = "Retry") {

        if (!retryClass)
            return "";

        return `
<button type="button"
        class="${escapeAttribute(retryClass)} inline-flex items-center justify-center gap-2 rounded-xl border border-red-700/70 bg-red-950/40 px-4 py-2 text-sm font-semibold text-red-200 transition hover:border-red-500 hover:bg-red-900/40 hover:text-white">

    <i class="fa-solid fa-rotate-right"></i>

    ${escapeHtml(retryLabel)}

</button>`;
    }

    function renderLoadFailed({
        title = "Unable to load",
        description = "An unexpected error occurred while loading this content.",
        compact = false,
        retryClass = null,
        retryLabel = "Retry",
    } = {}) {

        const retryButton = renderRetryButton(
            retryClass,
            retryLabel);

        if (compact) {

            return `
<div class="mx-2 my-3 rounded-xl border border-red-900/60 bg-red-950/30 px-4 py-4">

    <div class="flex items-start gap-3">

        <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-red-500/10">

            <i class="fa-solid fa-triangle-exclamation text-red-400"></i>

        </div>

        <div class="min-w-0 flex-1">

            <div class="text-sm font-semibold text-red-200">

                ${escapeHtml(title)}

            </div>

            ${description
                    ? `
            <p class="mt-1 text-xs leading-5 text-slate-400">

                ${escapeHtml(description)}

            </p>`
                    : ""}

        </div>

    </div>

            ${retryButton
                    ? `
    <div class="mt-4 flex justify-end">

        ${retryButton}

    </div>`
                    : ""}

</div>`;
        }

        return `
<div class="flex flex-col items-center justify-center rounded-3xl border border-red-900/60 bg-red-950/20 px-10 py-16 text-center">

    <div class="mb-5 flex h-16 w-16 items-center justify-center rounded-full bg-red-500/10">

        <i class="fa-solid fa-triangle-exclamation text-3xl text-red-400"></i>

    </div>

    <h3 class="text-xl font-semibold text-red-100">

        ${escapeHtml(title)}

    </h3>

    ${description
                ? `
    <p class="mt-2 max-w-md text-sm leading-relaxed text-slate-400">

        ${escapeHtml(description)}

    </p>`
                : ""}

    ${retryButton
                ? `
    <div class="mt-6">

        ${retryButton}

    </div>`
                : ""}

</div>`;
    }

    /* ==========================================================
       UPLOAD PRIVILEGE BADGE
       ========================================================== */

    function renderUploadPrivilegeBadge(canUpload) {

        const classNames = canUpload
            ? {
                container: "border-emerald-700 bg-emerald-600/10",
                icon: "bg-emerald-600/20 text-emerald-300",
                title: "text-emerald-300",
                description: "text-slate-400",
            }
            : {
                container: "border-slate-700 bg-slate-900",
                icon: "bg-slate-800 text-slate-500",
                title: "text-slate-300",
                description: "text-slate-500",
            };


        return `
<div class="${classNames.container} inline-flex items-center gap-3 rounded-2xl border px-5 py-3">

    <div class="${classNames.icon} flex h-11 w-11 items-center justify-center rounded-xl">

        <i class="fa-solid fa-cloud-arrow-up"></i>

    </div>

    <div>

        <div class="${classNames.title} text-sm font-semibold">

            ${canUpload ? "Upload available" : "Browse only"}

        </div>

        <div class="${classNames.description} text-xs">

            ${canUpload
                ? "You may upload teaching resources."
                : "You may only view documents."}

        </div>

    </div>

</div>`;
    }

    /* ==========================================================
       SUBJECT NAVIGATION SIDEBAR
       ========================================================== */

    function renderSubjectSidebar(subjects, selectedSubjectId) {

        if (!subjects?.length) {

            return renderEmpty(
                "fa-book",
                "No Subjects",
                "No subjects are currently available.");
        }

        return subjects
            .map(subject => {

                const selected = subject.id === selectedSubjectId;

                return `
<button
    type="button"
    class="${joinClasses(
                    "js-select-subject",
                    "group",
                    "flex",
                    "w-full",
                    "items-center",
                    "gap-3",
                    "border-l-4",
                    "px-4",
                    "py-3",
                    "text-left",
                    "transition",
                    "hover:bg-slate-800/60",
                    selected
                        ? "border-emerald-500 bg-slate-800"
                        : "border-transparent")}"
    data-subject-id="${subject.id}">

    <div class="${joinClasses(
                            "mt-1",
                            "flex",
                            "h-10",
                            "w-10",
                            "shrink-0",
                            "items-center",
                            "justify-center",
                            "rounded-xl",
                            selected
                                ? "bg-emerald-600 text-white"
                                : "bg-slate-800 text-slate-400 group-hover:bg-slate-700")}">

        <i class="fa-solid fa-book"></i>

    </div>

    <div class="min-w-0 flex-1">

        <div class="${joinClasses(
                                    "text-xs",
                                    "font-semibold",
                                    "tracking-wider",
                                    "uppercase",
                                    selected
                                        ? "text-emerald-300"
                                        : "text-slate-500")}">

            ${escapeHtml(subject.code)}

        </div>

        <div class="${joinClasses(
                                            "mt-1",
                                            "line-clamp-2",
                                            "text-sm",
                                            "font-medium",
                                            selected
                                                ? "text-white"
                                                : "text-slate-300")}">

            ${escapeHtml(subject.name)}

        </div>

    </div>

</button>`;
            })
            .join("");
    }

    function renderSubjectSidebarLoading() {

        return renderLoading({
            title: "Loading subjects...",
            compact: true,
        });
    }

    function renderSubjectSidebarLoadFailed() {

        return renderLoadFailed({
            title: "Unable to load subjects",
            description: "The subject list could not be retrieved.",
            compact: true,
            retryClass: "js-retry-subjects",
        });
    }

    /* ==========================================================
       CHAPTER NAVIGATION SIDEBAR
       ========================================================== */

    function renderChapterSidebar(chapters, selectedChapterId) {

        if (!chapters?.forSubjectId) {

            return renderEmpty(
                "fa-book-open",
                "No Subject Selected",
                "Select a subject to view its chapters.");
        }

        const overviewSelected = selectedChapterId === null;

        let html = `
<button
    type="button"
    class="${joinClasses(
            "js-select-chapter",
            "group",
            "flex",
            "w-full",
            "items-center",
            "gap-3",
            "border-l-4",
            "px-4",
            "py-3",
            "text-left",
            "transition",
            "hover:bg-slate-800/60",
            overviewSelected
                ? "border-emerald-500 bg-slate-800"
                : "border-transparent")}"
    data-chapter-id="">

    <div class="${joinClasses(
                    "flex",
                    "h-10",
                    "w-10",
                    "items-center",
                    "justify-center",
                    "rounded-xl",
                    overviewSelected
                        ? "bg-emerald-600 text-white"
                        : "bg-slate-800 text-slate-400")}">

        <i class="fa-solid fa-book-open"></i>

    </div>

    <div>

        <div class="${overviewSelected ? "font-semibold text-white" : "font-medium text-slate-300"}">

            Subject Overview

        </div>

    </div>

</button>`;

        html += chapters
            .map(chapter => {

                const selected = chapter.id === selectedChapterId;

                return `
<button
    type="button"
    class="${joinClasses(
                    "js-select-chapter",
                    "group",
                    "flex",
                    "w-full",
                    "items-center",
                    "gap-3",
                    "border-l-4",
                    "px-4",
                    "py-3",
                    "text-left",
                    "transition",
                    "hover:bg-slate-800/60",
                    selected
                        ? "border-emerald-500 bg-slate-800"
                        : "border-transparent")}"
    data-chapter-id="${chapter.id}">

    <div class="${joinClasses(
                            "flex",
                            "h-10",
                            "w-10",
                            "shrink-0",
                            "items-center",
                            "justify-center",
                            "rounded-xl",
                            selected
                                ? "bg-emerald-600 text-white"
                                : "bg-slate-800 text-slate-400")}">

        ${chapter.chapterNumber ?? "•"}

    </div>

    <div class="min-w-0 flex-1">

        <div class="${joinClasses(
                                    "truncate",
                                    "text-sm",
                                    selected
                                        ? "font-semibold text-white"
                                        : "font-medium text-slate-300")}">

            ${formatChapterTitle(chapter)}

        </div>

    </div>

</button>`;
            })
            .join("");

        return html;
    }

    function renderChapterSidebarLoading() {

        return renderLoading({
            title: "Loading chapters...",
            compact: true,
        });
    }

    function renderChapterSidebarLoadFailed() {

        return renderLoadFailed({
            title: "Unable to load chapters",
            description: "The chapter list could not be retrieved.",
            compact: true,
            retryClass: "js-retry-chapters",
        });
    }

    /* ==========================================================
       MAIN SECTION HEADER
       ========================================================== */

    function renderMainSectionBreadcrumb(subject, chapter) {

        return `
<span class="rounded-lg bg-emerald-500/15 px-3 py-1 text-sm font-semibold text-emerald-300">

    ${subject ? subject.code : "Select Subject"}

</span>

            ${subject
                ? `
            <i class="fa-solid fa-chevron-right text-xs text-slate-600"></i>

            <span class="rounded-lg bg-blue-500/20 px-3 py-1 text-sm font-semibold text-blue-300">

                ${chapter ? `Chapter ${chapter.chapterNumber}` : "Subject Overview"}

            </span>`
                : ""}`;
    }

    /* ==========================================================
       SUBJECT GRID
       ========================================================== */

    function renderSubjectGrid(subjects, selectedSubjectId) {

        if (!subjects?.length) {
            return renderEmpty(
                "fa-book",
                "No subjects available",
                "You are not currently enrolled in any subjects.");
        }

        return `
<div class="mb-8 flex items-center justify-between">

    <div>

        <h2 class="text-2xl font-bold">

            Your Subjects

        </h2>

        <p class="mt-2 text-sm text-slate-400 whitespace-normal">

            Select a subject to browse documents,
            chapters and teaching resources.

        </p>

    </div>

</div>

<div id="subjectGridContainer"
     class="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">

${subjects.map(subject => {

            const selected = subject.id === selectedSubjectId;

            return `
<button
    type="button"
    class="${joinClasses(
                "js-select-subject",
                "group",
                "overflow-hidden",
                "rounded-2xl",
                "border",
                "text-left",
                "transition",
                "duration-200",
                "flex",
                "flex-col",
                "justify-between",
                selected
                    ? "border-emerald-500 bg-slate-800 shadow-lg shadow-emerald-500/10"
                    : "border-slate-700 bg-slate-800 hover:border-slate-500 hover:-translate-y-1 hover:shadow-xl"
            )}
    "
    data-subject-id="${subject.id}">

    <div class="flex-1 flex items-center gap-2 justify-between p-6">

        <div class="min-w-0">

            <div class="inline-flex rounded-lg bg-emerald-500/15 px-3 py-1 text-xs font-semibold uppercase tracking-wider text-emerald-300">

                ${escapeHtml(subject.code)}

            </div>

            <h3 class="mt-4 line-clamp-2 text-xl font-semibold text-slate-100">

                ${escapeHtml(subject.name)}

            </h3>

            ${subject.description
                    ? `
                <p class="mt-3 line-clamp-3 text-sm leading-6 text-slate-400">

                    ${escapeHtml(subject.description)}

                </p>`
                    : ""}

        </div>

        <i class="fa-solid fa-book text-3xl ${selected ? "text-emerald-400" : "text-slate-500 group-hover:text-emerald-400"}"></i>

    </div>

    <div class="grid 2xl:grid-cols-3 border-t border-slate-700">

        ${renderStatTile(subject.chapterCount ?? 0, "Chapters")}
        ${renderStatTile(subject.documentCount ?? 0, "Documents")}
        ${renderStatTile(subject.memberCount ?? 0, "Members")}

    </div>

</button>`;
        }).join("")}

</div>`;
    }

    function renderSubjectGridLoading() {

        return renderLoading({
            title: "Loading subjects",
            description: "Preparing your document library...",
        });
    }

    function renderSubjectGridLoadFailed() {

        return renderLoadFailed({
            title: "Unable to load subjects",
            description: "Please try loading your document library again.",
            retryClass: "js-retry-subjects",
        });
    }

    /* ==========================================================
       SUBJECT DETAILS
       ========================================================== */

    function renderSubjectDetails(subject) {

        if (!subject) {

            return renderEmpty(
                "fa-book",
                "No subject selected",
                "Select a subject to view its details.");
        }

        return `
<div class="overflow-hidden border-b border-slate-700 px-8 py-3">

    <div class="flex items-center justify-between gap-8">

        <div class="min-w-0 flex-1">

            <h2 class="mt-4 text-4xl font-bold text-white">

                ${escapeHtml(subject.name)}

            </h2>

            ${subject.description
                ? `
                <p class="mt-6 max-w-4xl text-base leading-8 text-slate-300">

                    ${escapeHtml(subject.description)}

                </p>`
                : ""}

        </div>

    </div>

</div>

<div class="grid grid-cols-2 3xl:grid-cols-4 gap-6 p-8">

    ${renderInfoCard("fa-book-open", "Chapters", subject.chapterCount ?? 0)}
    ${renderInfoCard("fa-file", "Documents", subject.documentCount ?? 0)}
    ${renderInfoCard("fa-users", "Members", subject.memberCount ?? 0)}
    ${renderInfoCard("fa-clock", "Updated", formatTimeAgo(subject.lastUpdated) ?? "—")}

</div>`;
    }

    function renderSubjectDetailsLoading() {

        return renderLoading({
            title: "Loading subject",
            description: "Retrieving subject information...",
        });
    }

    function renderSubjectDetailsLoadFailed() {

        return renderLoadFailed({
            title: "Unable to load subject",
            description: "The selected subject could not be retrieved.",
            retryClass: "js-retry-subject-details",
        });
    }

    /* ==========================================================
       CHAPTER DETAILS
       ========================================================== */

    function renderChapterDetails(subject, chapter) {

        if (!subject || !chapter) {

            return renderEmpty(
                "fa-book-open",
                "No chapter selected",
                "Select a chapter to view its details.");
        }

        return `
<div class="border-b border-slate-700 px-8 py-3">

    <div class="flex items-center justify-between gap-8">

        <div class="min-w-0 flex-1">

            <h2 class="mt-4 text-4xl font-bold text-white">

                ${escapeHtml(chapter.name)}

            </h2>

            ${chapter.description
                ? `
                <p class="mt-6 max-w-4xl text-base leading-8 text-slate-300">

                    ${escapeHtml(chapter.description)}

                </p>`
                : ""}

        </div>

    </div>

</div>

<div class="grid gap-6 p-8 2xl:grid-cols-3">

    ${renderInfoCard("fa-book", "Subject", subject.code)}
    ${renderInfoCard("fa-layer-group", "Chapter", chapter.chapterNumber ?? "—")}
    ${renderInfoCard("fa-file-lines", "Documents", chapter.documentCount ?? 0)}

</div>`;
    }

    function renderChapterDetailsLoading() {

        return renderLoading({
            title: "Loading chapter",
            description: "Retrieving chapter information...",
        });
    }

    function renderChapterDetailsLoadFailed() {

        return renderLoadFailed({
            title: "Unable to load chapter",
            description: "The selected chapter could not be retrieved.",
            retryClass: "js-retry-chapter-details",
        });
    }

    /* ==========================================================
       SHARED DETAIL HELPERS
       ========================================================== */

    function renderStatTile(value, label) {

        return `
<div class="text-center py-4 border-slate-700 max-2xl:border-b 2xl:border-r max-2xl:last:border-b-0 2xl:last:border-r-0">

    <div class="text-2xl font-bold text-white">

        ${escapeHtml(value)}

    </div>

    <div class="mt-1 text-xs uppercase tracking-wide text-slate-500">

        ${escapeHtml(label)}

    </div>

</div>`;
    }

    function renderInfoCard(icon, label, value) {

        return `
<div class="rounded-xl border border-slate-700 bg-slate-900/50 p-4">

    <div class="flex items-center justify-start gap-3">

        <div class="shrink-0 flex h-12 w-12 items-center justify-center rounded-xl bg-slate-700 text-slate-300">

            <i class="fa-solid ${icon}"></i>

        </div>

        <div>

            <div class="text-xs uppercase tracking-wide text-slate-500">

                ${escapeHtml(label)}

            </div>

            <div class="mt-1 text-xl font-bold text-white truncate">

                ${escapeHtml(value)}

            </div>

        </div>

    </div>

</div>`;
    }

    /* =========================================================
       UPLOAD PANEL
       ========================================================= */

    function renderUploadSubjectSummary(subject) {

        if (!subject) {

            return renderEmpty(
                "fa-file",
                "Select a subject",
                "You must first select a subject to upload to.");
        }

        return `
Every uploaded file will belong to
<strong class="text-emerald-400">
    ${escapeHtml(subject.code)}
</strong>
—
${escapeHtml(subject.name)}`;
    }

    function renderUploadChapterTags(
        chapters,
        selectedChapterIds) {

        const chapterList = Array.from(chapters ?? []);

        if (chapterList.length === 0) {

            return `
<div class="flex items-start gap-3 rounded-xl border border-slate-700 bg-slate-800/60 px-4 py-3">

    <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-700 text-slate-400">

        <i class="fa-solid fa-layer-group"></i>

    </div>

    <div class="min-w-0">

        <div class="text-sm font-semibold text-slate-200">

            No chapter tags available

        </div>

        <p class="mt-1 text-xs leading-5 text-slate-400">

            This document will belong to the selected subject without
            an additional chapter tag.

        </p>

    </div>

</div>`;
        }

        const selectedSet = new Set(
            Array.from(selectedChapterIds ?? [], String));

        return chapterList.map(chapter => {

            const selected = selectedSet.has(chapter.id);

            return `
<button type="button"
        class="${joinClasses(
                "js-chapter-tag",
                "rounded-full",
                "border",
                "px-4",
                "py-2",
                "text-sm",
                "font-medium",
                "transition-all",
                "duration-200",
                selected
                    ? "border-emerald-500 bg-emerald-600/15 text-emerald-300 shadow-[0_0_0_1px_rgba(16,185,129,.15)]"
                    : "border-slate-600 bg-slate-700 text-slate-200 hover:border-emerald-500 hover:bg-slate-600"
            )}"
        data-chapter-id="${escapeAttribute(chapter.id)}">

    ${selected
                    ? `<i class="fa-solid fa-check mr-2 text-xs"></i>`
                    : ""}

    ${formatChapterTitle(chapter)}

</button>`;
        }).join("");
    }

    function renderUploadChapterTagsLoading() {

        return renderLoading({
            title: "Loading chapter tags...",
            compact: true,
        });
    }

    function renderUploadChapterTagsLoadFailed() {

        return renderLoadFailed({
            title: "Unable to load chapter tags",
            description: "The available chapter tags could not be retrieved.",
            compact: true,
            retryClass: "js-retry-chapters",
        });
    }

    /* =========================================================
       UPLOAD MODAL
       ========================================================= */

    function getUploadFileExtension(fileName) {

        const name = String(fileName ?? "");
        const dotIndex = name.lastIndexOf(".");

        return dotIndex >= 0
            ? name.slice(dotIndex).toUpperCase()
            : "";
    }

    function getUploadFileStyle(extension) {

        switch (extension) {

            case ".PDF":
                return {
                    tileClass: "bg-red-500/10",
                    iconClass: "text-red-400",
                };

            case ".DOC":
            case ".DOCX":
                return {
                    tileClass: "bg-blue-500/10",
                    iconClass: "text-blue-400",
                };

            case ".PPT":
            case ".PPTX":
                return {
                    tileClass: "bg-orange-500/10",
                    iconClass: "text-orange-400",
                };

            case ".HTML":
            case ".HTM":
                return {
                    tileClass: "bg-violet-500/10",
                    iconClass: "text-violet-400",
                };

            case ".TXT":
                return {
                    tileClass: "bg-slate-700",
                    iconClass: "text-slate-300",
                };

            default:
                return {
                    tileClass: "bg-slate-700",
                    iconClass: "text-slate-300",
                };
        }
    }

    function getUploadStatusView(statusName) {

        const views = {

            Pending: {
                label: "Queued",
                iconClass: "fa-clock",
                textClass: "text-slate-400",
                barClass: "bg-slate-500",
            },

            Starting: {
                label: "Starting",
                iconClass: "fa-spinner fa-spin",
                textClass: "text-amber-300",
                barClass: "bg-amber-400",
            },

            Active: {
                label: "Uploading",
                iconClass: "fa-cloud-arrow-up",
                textClass: "text-emerald-300",
                barClass: "bg-emerald-500",
            },

            Succeeded: {
                label: "Uploaded",
                iconClass: "fa-circle-check",
                textClass: "text-emerald-300",
                barClass: "bg-emerald-500",
            },

            Failed: {
                label: "Failed",
                iconClass: "fa-triangle-exclamation",
                textClass: "text-red-300",
                barClass: "bg-red-500",
            },

            Aborted: {
                label: "Cancelled",
                iconClass: "fa-ban",
                textClass: "text-slate-500",
                barClass: "bg-slate-500",
            },
        };

        return views[statusName] ?? views.Pending;
    }

    function renderUploadRow(uploadItem) {

        const file = uploadItem.file;
        const fileName = file.name;
        const extension = getUploadFileExtension(fileName);

        const fileStyle = getUploadFileStyle(extension);
        const statusView = getUploadStatusView(
            uploadItem.statusName);

        const rawProgress = Number.isFinite(uploadItem.progress)
            ? uploadItem.progress
            : 0;

        const progress =
            uploadItem.statusName === "Succeeded"
                ? 100
                : Math.min(
                    Math.max(Math.round(rawProgress), 0),
                    100);

        const canRetry = uploadItem.canRetry;
        const canCancel = uploadItem.canCancel;
        const canRemove = uploadItem.canRemove;

        return `
<article class="js-upload-item group px-4 py-4 transition-colors hover:bg-slate-800/70"
         data-upload-id="${escapeAttribute(uploadItem.id)}">

    <div class="flex items-center gap-3">

        <div class="${fileStyle.tileClass} flex h-11 w-11 shrink-0 items-center justify-center rounded-xl">

            <i class="fa-solid ${getFileIcon(extension)} ${fileStyle.iconClass} text-lg"></i>

        </div>

        <div class="min-w-0 flex-1">

            <div class="flex items-start gap-3">

                <div class="min-w-0 flex-1">

                    <div title="${escapeAttribute(fileName)}"
                         class="truncate text-sm font-semibold text-slate-100">

                        ${escapeHtml(fileName)}

                    </div>

                    <div class="mt-1.5 flex min-w-0 items-center gap-2 text-xs">

                        <span class="${statusView.textClass} inline-flex shrink-0 items-center gap-1.5 font-medium">

                            <i class="fa-solid ${statusView.iconClass}"></i>

                            ${statusView.label}

                        </span>

                        <span class="text-slate-600">·</span>

                        <span class="truncate text-slate-500">

                            ${formatFileSize(file.size)}

                        </span>

                        <span class="js-upload-percent ml-auto w-10 shrink-0 text-right font-medium text-slate-400">

                            ${progress}%

                        </span>

                    </div>

                </div>

                <div class="grid w-[72px] shrink-0 grid-cols-2 gap-2">

                    ${canRetry
                ? `
                    <button type="button"
                            class="js-retry-upload flex h-8 w-8 items-center justify-center rounded-lg border border-amber-500/40 bg-amber-500/10 text-amber-300 transition hover:border-amber-400 hover:bg-amber-500/20 hover:text-amber-200"
                            data-upload-id="${escapeAttribute(uploadItem.id)}"
                            aria-label="Retry upload of ${escapeAttribute(fileName)}"
                            title="Retry upload">

                        <i class="fa-solid fa-rotate-right"></i>

                    </button>`
                : `
                    <span aria-hidden="true"></span>`}

                    ${canCancel
                ? `
                    <button type="button"
                            class="js-cancel-upload flex h-8 w-8 items-center justify-center rounded-lg border border-slate-700 bg-slate-800 text-slate-400 transition hover:border-red-500/60 hover:bg-red-500/10 hover:text-red-300"
                            data-upload-id="${escapeAttribute(uploadItem.id)}"
                            aria-label="Cancel upload of ${escapeAttribute(fileName)}"
                            title="Cancel upload">

                        <i class="fa-solid fa-xmark"></i>

                    </button>`
                : canRemove
                    ? `
                    <button type="button"
                            class="js-remove-upload flex h-8 w-8 items-center justify-center rounded-lg border border-slate-700 bg-slate-800 text-slate-400 transition hover:border-slate-500 hover:bg-slate-700 hover:text-white"
                            data-upload-id="${escapeAttribute(uploadItem.id)}"
                            aria-label="Remove ${escapeAttribute(fileName)} from upload list"
                            title="Remove from upload list">

                        <i class="fa-solid fa-xmark"></i>

                    </button>`
                    : `
                    <span aria-hidden="true"></span>`}

                </div>

            </div>

            <div class="mt-3 h-1.5 overflow-hidden rounded-full bg-slate-800"
                 role="progressbar"
                 aria-label="Upload progress for ${escapeAttribute(fileName)}"
                 aria-valuemin="0"
                 aria-valuemax="100"
                 aria-valuenow="${progress}"
                 aria-valuetext="${escapeAttribute(statusView.label)}, ${progress} percent">

                <div class="js-upload-progress-bar ${statusView.barClass} h-full rounded-full transition-[width] duration-200 ease-out"
                     style="width: ${progress}%">
                </div>

            </div>

        </div>

    </div>

</article>`;
    }

    /* =========================================================
       DOCUMENT DRAWER
       ========================================================= */

    function renderDocumentList(documents, totalDocuments, canDelete) {

        if (!documents?.forSubjectId) {

            return renderEmpty(
                "fa-file",
                "No Subject Selected",
                "Select a subject to view its documents.");
        }

        if (!documents?.length) {

            return `
<div class="px-8 py-20">

    ${renderEmpty(
                "fa-file",
                "No Documents",
                "No documents match the current selection.")}

</div>`;
        }

        return `
<div class="divide-y divide-slate-700">

${documents.map(doc => renderDocumentRow(doc, canDelete)).join("")}

</div>

<div class="border-t border-slate-700 bg-slate-800 px-8 py-4">

    <div class="text-sm text-slate-400">

        ${totalDocuments}
        document${totalDocuments === 1 ? "" : "s"}

    </div>

</div>`;
    }

    function renderDocumentRow(doc, canDelete) {

        return `
<div
    class="flex items-center justify-between gap-6 px-8 py-5 transition hover:bg-slate-800/60">

    <div class="min-w-0 flex-1 flex items-center gap-5">

        <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-slate-700 text-2xl text-slate-300">

            <i class="fa-solid ${getFileIcon(doc.extension)}"></i>

        </div>

        <div class="min-w-0 flex-1">

            <div class="flex items-center gap-3">

                <a
                    href="/documents/download/${doc.id}"
                    class="truncate text-lg font-semibold text-slate-100 transition hover:text-emerald-400">

                    ${escapeHtml(doc.title)}

                </a>

                ${renderDocumentStatusBadge(doc)}

            </div>

            <div class="mt-3 flex flex-wrap gap-x-6 gap-y-2 text-sm text-slate-400">

                <span>

                    <i class="fa-solid fa-user mr-2"></i>

                    ${escapeHtml(doc.uploadedBy)}

                </span>

                <span>

                    <i class="fa-solid fa-calendar mr-2"></i>

                    ${formatDate(doc.uploadedAt)}

                </span>

            ${doc.fileSize != null
                ? `
                <span>

                    <i class="fa-solid fa-hard-drive mr-2"></i>

                    ${formatFileSize(doc.fileSize)}

                </span>`
                : ""}

            </div>

        </div>

    </div>

    <div class="flex shrink-0 items-center gap-2">

        <a
        href="/documents/details/${doc.id}"
        class="flex h-[46px] w-[46px] items-center justify-center rounded-xl border border-slate-600 p-3 text-slate-300 transition hover:border-emerald-500 hover:text-emerald-400">

            <i class="fa-solid fa-eye"></i>

        </a>

        <a
            href="/documents/download/${doc.id}"
            class="flex h-[46px] w-[46px] items-center justify-center rounded-xl border border-slate-600 p-3 text-slate-300 transition hover:border-emerald-500 hover:text-emerald-400">

            <i class="fa-solid fa-download"></i>

        </a>

            ${canDelete
                ? `
            <button type="button"
                class="js-delete-document flex h-[46px] w-[46px] items-center justify-center rounded-xl border border-red-700 p-3 text-red-400 transition hover:bg-red-900/30"
                data-document-id="${doc.id}">

                <i class="fa-solid fa-trash"></i>

            </button>`
                : ""}

    </div>

</div>`;
    }

    function renderDocumentStatusBadge(doc) {

        const settings = StatusSettings[doc.status] ?? StatusSettings.Received;
        const hasProgress = Number.isFinite(doc.statusProgress);

        const text = hasProgress
            ? settings.text.replace("{{PROGRESS}}", doc.statusProgress.toFixed(2))
            : settings.text.replace("({{PROGRESS}}%)", "").trim();

        return `
<span class="js-document-status document-status-badge ${settings.className}"
      data-document-id="${escapeAttribute(doc.id)}">

    <i class="fa-solid ${settings.iconClass}"></i>

    ${escapeHtml(text)}

</span>`;
    }

    function renderDocumentListLoading(pageSize) {

        const rows = Array.from(
            { length: pageSize },
            (_, i) => `

<div class="flex items-center justify-between gap-6 px-8 py-5">

    <!-- ===================================================== -->
    <!-- INFO                                                  -->
    <!-- ===================================================== -->

    <div class="min-w-0 flex flex-1 items-center gap-5">

        <!-- File icon -->

        <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-slate-800 skeleton">

            <div class="h-7 w-7 rounded bg-slate-700"></div>

        </div>

        <!-- Text -->

        <div class="min-w-0 flex-1">

            <!-- Title + status -->

            <div class="flex items-center gap-3">

                <div class="h-6 w-64 max-w-[45%] rounded bg-slate-700 skeleton"></div>

                <div class="h-7 w-28 rounded-full bg-slate-800 skeleton"></div>

            </div>

            <!-- Metadata -->

            <div class="mt-3 flex flex-wrap gap-x-6 gap-y-2">

                <div class="h-4 w-28 rounded bg-slate-800 skeleton"></div>

                <div class="h-4 w-36 rounded bg-slate-800 skeleton"></div>

                <div class="h-4 w-20 rounded bg-slate-800 skeleton"></div>

            </div>

        </div>

    </div>

    <!-- ===================================================== -->
    <!-- ACTION BUTTONS                                        -->
    <!-- ===================================================== -->

    <div class="flex shrink-0 items-center gap-2">

        <div class="h-[46px] w-[46px] rounded-xl bg-slate-800 skeleton"></div>

        <div class="h-[46px] w-[46px] rounded-xl bg-slate-800 skeleton"></div>

        <div class="h-[46px] w-[46px] rounded-xl bg-slate-800 skeleton"></div>

    </div>

</div>

${i === pageSize - 1 ? "" : `<div class="border-b border-slate-700"></div>`}
`).join("");

        return `
<div class="divide-y divide-slate-700">

    ${rows}

</div>

<div class="border-t border-slate-700 bg-slate-800 px-8 py-4">

    <div class="h-4 w-32 rounded bg-slate-700 skeleton"></div>

</div>`;
    }

    function renderDocumentListLoadFailed() {

        return `
<div class="px-8 py-14">

    ${renderLoadFailed({
            title: "Unable to load documents",
            description: "The requested document list could not be retrieved.",
            retryClass: "js-retry-documents",
        })}

</div>

<div class="border-t border-slate-700 bg-slate-800 px-8 py-4">

    <div class="text-sm text-slate-500">

        0 documents

    </div>

</div>`;
    }

    /* =========================================================
       PAGINATION
       ========================================================= */

    function renderPagination(currentPage, totalPages) {

        if (!totalPages || totalPages <= 1)
            return "";

        const buttons = [];

        for (let page = 1; page <= totalPages; page++) {

            buttons.push(`
<button
    type="button"
    class="js-page rounded-xl px-4 py-2 text-sm font-medium transition ${page === currentPage
                    ? "bg-emerald-600 text-white"
                    : "bg-slate-700 text-slate-300 hover:bg-slate-600"}"
    data-page-index="${page}">

    ${page}

</button>`);
        }

        return `
<div class="flex items-center justify-center gap-2 px-8 py-6">

    <button
        type="button"
        class="js-prev rounded-xl bg-slate-700 px-4 py-2 text-slate-300 transition hover:bg-slate-600 disabled:opacity-40"
        data-page-index="${currentPage - 1}"
        ${currentPage === 1 ? "disabled" : ""}>

        <i class="fa-solid fa-chevron-left"></i>

    </button>

    ${buttons.join("")}

    <button
        type="button"
        class="js-next rounded-xl bg-slate-700 px-4 py-2 text-slate-300 transition hover:bg-slate-600 disabled:opacity-40"
        data-page-index="${currentPage + 1}"
        ${currentPage === totalPages ? "disabled" : ""}>

        <i class="fa-solid fa-chevron-right"></i>

    </button>

</div>`;
    }


    /* =========================================================
       TOAST
       ========================================================= */

    function renderToast({
        id,
        iconClass,
        title,
        borderClass,
        iconColorClass,
        titleColorClass,
    }) {

        return `
<div id="${id}"
     class="
        pointer-events-auto
        translate-y-5
        opacity-0
        rounded-xl
        border
        ${borderClass}
        bg-slate-900/95
        shadow-2xl
        backdrop-blur
        transition-all
        duration-300">

    <div class="flex items-center gap-3 px-4 py-3">

        <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-800">

            <i class="fa-solid ${iconClass} ${iconColorClass}"></i>

        </div>

        <div class="min-w-0 flex-1 text-center truncate font-medium ${titleColorClass}">

            ${escapeHtml(title)}

        </div>

    </div>

</div>
`;
    }

    /* =========================================================
       PUBLIC EXPORTS
       ========================================================= */

    return {

        renderUploadPrivilegeBadge,

        renderSubjectSidebar,
        renderSubjectSidebarLoading,
        renderSubjectSidebarLoadFailed,

        renderChapterSidebar,
        renderChapterSidebarLoading,
        renderChapterSidebarLoadFailed,

        renderMainSectionBreadcrumb,

        renderSubjectGrid,
        renderSubjectGridLoading,
        renderSubjectGridLoadFailed,

        renderSubjectDetails,
        renderSubjectDetailsLoading,
        renderSubjectDetailsLoadFailed,

        renderChapterDetails,
        renderChapterDetailsLoading,
        renderChapterDetailsLoadFailed,

        renderUploadSubjectSummary,
        renderUploadChapterTags,
        renderUploadChapterTagsLoading,
        renderUploadChapterTagsLoadFailed,
        renderUploadRow,

        renderDocumentList,
        renderDocumentStatusBadge,
        renderDocumentListLoading,
        renderDocumentListLoadFailed,

        renderPagination,

        renderToast,
    };

})();
