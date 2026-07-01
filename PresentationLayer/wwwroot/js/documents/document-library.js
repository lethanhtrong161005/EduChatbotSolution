"use strict";

(() => {

    /* =========================================================
       STATE
       ========================================================= */

    const state = {
        callerConnectionId: null,

        subjects: [],
        chapters: [],
        documents: [],

        selectedSubjectId: null,
        selectedChapterId: null,

        selectedSubject: null,
        selectedChapter: null,

        search: "",
        pageIndex: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 1,

        canUpload: false,
        showUploadPanel: true,

        initialized: false,
        loading: {
            subjects: false,
            chapters: false,
            documents: false,
        }
    };

    window.DocumentLibraryState = state;

    /* =========================================================
       DOM
       ========================================================= */

    const ui = {
        subjectGrid: $("#subjectGrid"),
        subjectSidebar: $("#subjectSidebar"),
        chapterSidebar: $("#chapterSidebar"),

        infoCard: $("#infoCard"),
        documentDrawer: $("#documentDrawer"),

        pagination: $("#pagination"),

        search: $("#searchInput"),

        uploadContainer: $("#uploadPanelContainer"),
        uploadPanel: $("#uploadPanel"),
        uploadOverlay: $("#uploadPanelOverlay"),
        uploadToggle: $("#showUploadBtn")
    };

    /* =========================================================
       STARTUP
       ========================================================= */

    $(document).ready(init);

    async function init() {

        await waitForConnection();

        bindEvents();

        await loadSubjects();

        state.initialized = true;
    }

    async function waitForConnection() {

        while (!window.ResourceConnectionId) {
            await delay(100);
        }

        state.callerConnectionId = window.ResourceConnectionId;
    }

    function delay(ms) {
        return new Promise(r => setTimeout(r, ms));
    }

    /* =========================================================
       EVENT WIRING
       ========================================================= */

    function bindEvents() {

        ui.search.on("input", onSearchChanged);

        ui.subjectGrid.on(
            "click",
            "[data-subject-id]",
            e => {

                const id =
                    Number($(e.currentTarget).data("subject-id"));

                selectSubject(id);
            });

        ui.subjectSidebar.on(
            "click",
            "[data-subject-id]",
            e => {

                const id =
                    Number($(e.currentTarget).data("subject-id"));

                selectSubject(id);
            });

        ui.chapterSidebar.on(
            "click",
            "[data-chapter-id]",
            e => {

                const id =
                    Number($(e.currentTarget).data("chapter-id"));

                selectChapter(id);
            });

        ui.pagination.on(
            "click",
            "[data-page]",
            e => {

                const page =
                    Number($(e.currentTarget).data("page"));

                if (page === state.pageIndex)
                    return;

                state.pageIndex = page;

                loadDocuments();
            });

        ui.uploadToggle.on(
            "click",
            () => {
                state.showUploadPanel = !state.showUploadPanel;
                updateUi_Upload();
            });

        $(document).on(
            "resource:changed",
            onRealtimeUpdate);
    }

    /* =========================================================
       NAVIGATION
       ========================================================= */

    async function selectSubject(subjectId) {

        if (subjectId === state.selectedSubjectId)
            return;

        state.selectedSubjectId = subjectId;
        state.selectedChapterId = null;

        state.selectedSubject =
            state.subjects.find(x => x.id === subjectId) ?? null;

        state.selectedChapter = null;

        state.pageIndex = 1;

        renderNavigationFrame();

        await Promise.all([
            loadSubjectDetails(),
            loadDocuments(),
        ]);
    }

    function selectChapter(chapterId) {

        if (chapterId === state.selectedChapterId)
            return;

        state.selectedChapterId = chapterId;

        state.selectedChapter =
            state.chapters.find(x => x.id === chapterId) ?? null;

        renderNavigationFrame();
        renderDetailsCard();

        state.pageIndex = 1;

        loadDocuments();
    }

    function onSearchChanged() {

        state.search = ui.search.val().trim();

        state.pageIndex = 1;

        loadDocuments();
    }

    /* =========================================================
       LOADING
       ========================================================= */

    async function loadSubjects() {

        state.loading.subjects = true;

        try {

            const response =
                await fetch(
                    "/documents/library?handler=Subjects",
                    {
                        headers: {
                            "CallerConnectionId": state.callerConnectionId,
                        }
                    });

            state.subjects = await response.json();

            renderSubjectGrid();

            if (state.selectedSubjectId) {

                const stillExists =
                    state.subjects.some(
                        x => x.id === state.selectedSubjectId);

                if (!stillExists) {

                    state.selectedSubjectId = null;
                    state.selectedSubject = null;

                    renderNavigationFrame();
                }
            }

        }
        finally {
            state.loading.subjects = false;
        }
    }

    async function loadSubjectDetails() {

        if (!state.selectedSubjectId)
            return;

        const response =
            await fetch(
                `/documents/library?handler=Subject&id=${state.selectedSubjectId}`,
                {
                    headers: {
                        "CallerConnectionId": state.callerConnectionId,
                    }
                });

        state.selectedSubject = await response.json();
        state.chapters = state.selectedSubject.chapters ?? [];
        state.canUpload = state.selectedSubject.canUpload ?? false;

        renderDetailsCard();
        updateState_Chapters();
        updateUi_Upload();
    }

    function updateState_Chapters() {

        state.chapters.sort((a, b) => {

            const an =
                a.chapterNumber ?? Number.MAX_SAFE_INTEGER;

            const bn =
                b.chapterNumber ?? Number.MAX_SAFE_INTEGER;

            if (an !== bn)
                return an - bn;

            return a.name.localeCompare(b.name);
        });

        if (state.selectedChapterId) {

            state.selectedChapter =
                state.chapters.find(
                    x => x.id === state.selectedChapterId)
                ?? null;
        }

        renderChapterSidebar();
    }

    function updateUi_Upload() {

        renderDetailsCard();
        ui.uploadToggle.toggleClass("hidden", !state.canUpload);
        ui.uploadPanel.toggleClass("hidden", !(state.canUpload && state.showUploadPanel));
    }

    async function loadChapters() {

        if (!state.selectedSubjectId)
            return;

        state.loading.chapters = true;

        try {

            const response =
                await fetch(
                    `/documents/library?handler=Chapters&${qs}`,
                    {
                        headers: {
                            "CallerConnectionId": state.callerConnectionId,
                        }
                    });

            state.chapters = await response.json();

            updateState_Chapters();
        }
        finally {
            state.loading.chapters = false;
        }
    }

    async function loadDocuments() {

        if (!state.selectedSubjectId)
            return;

        state.loading.documents = true;

        try {

            const qs = new URLSearchParams();

            qs.set("subjectId", state.selectedSubjectId);
            qs.set("pageIndex", state.pageIndex);
            qs.set("pageSize", state.pageSize);

            if (state.selectedChapterId)
                qs.set("chapterId", state.selectedChapterId);

            if (state.search)
                qs.set("search", state.search);

            const response =
                await fetch(
                    `/documents/library?handler=Documents&${qs}`,
                    {
                        headers: {
                            "CallerConnectionId": state.callerConnectionId,
                        }
                    });

            const result = await response.json();

            state.documents = result.items;
            state.pageIndex = result.page;
            state.pageSize = result.pageSize;
            state.totalCount = result.totalCount;
            state.totalPages = result.totalPages;

            renderDocuments();
            renderPagination();
        }
        finally {
            state.loading.documents = false;
        }
    }

    /* =========================================================
       REALTIME
       ========================================================= */

    async function onRealtimeUpdate(_, update) {

        switch (update.resourceType) {

            case ResourceType.Subject:
                await loadSubjects();

                if (state.selectedSubjectId)
                    await loadSubjectDetails();

                break;

            case ResourceType.Chapter:

                if (update.properties["subjectId"] === state.selectedSubjectId)
                    await loadSubjectDetails();

                break;

            case ResourceType.Document:
            case ResourceType.User:

                if (!state.selectedSubjectId)
                    break;

                await loadDocuments();

                break;

            case ResourceType.Membership:

                if (update.properties["userId"] !== Razor.userId)
                    break;

                await loadSubjects();

                break;
        }
    }

    /* ==========================================================
       RENDER ENTRY POINTS
       ========================================================== */

    function renderNavigationFrame() {

        renderSubjectGrid();
        renderSubjectSidebar();
        renderChapterSidebar();
        // renderDetailsCard();
        // renderDocuments();
        // renderPagination();
    }

    function renderSubjectGrid() {

        ui.subjectGrid.html(

            DocumentLibraryTemplates.renderSubjectGrid(
                state.subjects,
                state.selectedSubjectId));
    }

    function renderSubjectSidebar() {

        ui.subjectSidebar.html(

            DocumentLibraryTemplates.renderSubjectSidebar(
                state.subjects,
                state.selectedSubjectId,
                !!state.selectedSubject));
    }

    function renderChapterSidebar() {

        ui.chapterSidebar.html(

            DocumentLibraryTemplates.renderChapterSidebar(
                state.chapters,
                state.selectedChapterId,
                !!state.selectedSubject));
    }

    function renderDetailsCard() {

        ui.infoCard.html(

            DocumentLibraryTemplates.renderDetailsCard(
                state.selectedSubject,
                state.selectedChapter,
                state.canUpload));
    }

    function renderDocuments() {

        ui.documentDrawer.html(

            DocumentLibraryTemplates.renderDocumentDrawer(
                state.documents,
                state.totalCount));

        ui.documentDrawer
            .find(".js-delete-document")
            .off("click")
            .on("click", promptDeleteDocument);
    }

    function renderPagination() {

        ui.pagination.html(

            DocumentLibraryTemplates.renderPagination(
                state.pageIndex,
                state.totalPages));

        ui.pagination
            .find(".js-page")
            .off("click")
            .on(
                "click",
                async function () {

                    const page =
                        Number($(this).data("page"));

                    if (page === state.pageIndex)
                        return;

                    state.pageIndex = page;

                    await loadDocuments();
                });
    }

    /* ======================================================
       SEARCH
       ====================================================== */

    async function performSearch() {

        clearTimeout(searchTimer);

        searchTimer =
            setTimeout(
                async () => {

                    state.search =
                        ui.search
                            .val()
                            .trim();

                    state.pageIndex = 1;

                    await loadDocuments();
                },
                250);
    }

    /* ======================================================
       PAGINATION
       ====================================================== */

    async function refreshDocuments(resetPage = false) {

        if (resetPage)
            state.pageIndex = 1;

        await loadDocuments();
    }

    /* ======================================================
       DELETE
       ====================================================== */

    async function deleteDocument() {

        const id =
            $(this).data("id");

        if (!id)
            return;

        if (!confirm("Delete this document?"))
            return;

        const response =
            await fetch(
                `/documents/library/${id}`,
                {
                    method: "DELETE",
                    headers:
                    {
                        RequestVerificationToken:
                            antiForgery(),

                        CallerConnectionId:
                            connId,
                    }
                });

        if (!response.ok)
            return;

        if (
            state.documents.length === 1 &&
            state.pageIndex > 1) {
            state.pageIndex--;
        }

        await loadDocuments();
    }

    /* ======================================================
       UPLOAD
       ====================================================== */

    const uploadQueue = [];
    let activeUploads = 0;
    let outstandingUploads = 0;

    const MAX_CONCURRENT_UPLOADS = 3;

    function queueUploads(files) {

        if (!state.selectedChapterId) {
            alert("Select a chapter first.");
            return;
        }

        [...files]
            .filter(validateFile)
            .forEach(file => {

                file.uploadId =
                    crypto.randomUUID();

                uploadQueue.push(file);

                outstandingUploads++;

                $("#uploadQueue")
                    .append(
                        Templates.uploadRow(file));
            });

        processUploadQueue();
    }

    function processUploadQueue() {

        while (
            uploadQueue.length &&
            activeUploads < MAX_CONCURRENT_UPLOADS) {
            const file =
                uploadQueue.shift();

            upload(file);
        }

        if (outstandingUploads === 0) {
            loadDocuments();
        }
    }

    function upload(file) {

        activeUploads++;

        const form =
            new FormData();

        form.append(
            "chapterId",
            state.selectedChapterId);

        form.append(
            "files",
            file);

        const xhr =
            new XMLHttpRequest();

        xhr.open(
            "POST",
            "/documents/library?handler=Upload");

        xhr.setRequestHeader(
            "RequestVerificationToken",
            antiForgery());

        xhr.setRequestHeader(
            "CallerConnectionId",
            connId);

        xhr.upload.addEventListener(
            "progress",
            e => {

                if (!e.lengthComputable)
                    return;

                const pct =
                    Math.round(
                        e.loaded /
                        e.total *
                        100);

                updateUploadProgress(
                    file.uploadId,
                    pct);
            });

        xhr.addEventListener(
            "load",
            () => {

                if (
                    xhr.status >= 200 &&
                    xhr.status < 300) {
                    updateUploadStatus(
                        file.uploadId,
                        "Completed");
                }
                else {
                    updateUploadStatus(
                        file.uploadId,
                        "Failed",
                        true);
                }

                activeUploads--;
                outstandingUploads--;

                processUploadQueue();
            });

        xhr.addEventListener(
            "error",
            () => {

                updateUploadStatus(
                    file.uploadId,
                    "Failed",
                    true);

                activeUploads--;
                outstandingUploads--;

                processUploadQueue();
            });

        xhr.send(form);
    }

    function updateUploadProgress(id, percent) {

        const row =
            $(`#${id}`);

        row.find(".upload-progress-bar")
            .css(
                "width",
                `${percent}%`);

        row.find(".upload-percent")
            .text(`${percent}%`);
    }

    function updateUploadStatus(
        id,
        text,
        failed = false) {

        const row =
            $(`#${id}`);

        row.find(".upload-percent")
            .text(text);

        if (failed) {
            row.find(".upload-progress-bar")
                .addClass("bg-red-500");
        }
    }

    function validateFile(file) {

        const ext =
            file.name
                .split(".")
                .pop()
                .toLowerCase();

        return [
            "pdf",
            "docx",
            "pptx",
            "txt",
            "html"
        ].includes(ext);
    }

    /* ======================================================
       HELPERS
       ====================================================== */

    function antiForgery() {

        return $("input[name='__RequestVerificationToken']")
            .val();
    }

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

    function fileIcon(ext) {

        switch ((ext ?? "").toUpperCase()) {
            case "PDF":
                return "fa-file-pdf";

            case "DOCX":
                return "fa-file-word";

            case "PPTX":
                return "fa-file-powerpoint";

            case "TXT":
                return "fa-file-lines";

            case "HTML":
                return "fa-file-code";

            default:
                return "fa-file";
        }
    }

})();
