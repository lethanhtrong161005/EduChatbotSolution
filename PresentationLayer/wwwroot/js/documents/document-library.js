"use strict";

(() => {

    /* =========================================================
       ENUMS AND CONSTANTS
       ========================================================= */

    const MAX_CONCURRENT_UPLOADS = 3;
    const DEFAULT_PAGE_SIZE = 10;

    const MainSectionLevel = Object.freeze({

        SubjectGrid: 0,
        SubjectDetails: 1,
        ChapterDetails: 2,
    });

    const UploadStatus = Object.freeze({

        Pending: 0,
        Starting: 1,
        Active: 2,
        Succeeded: 3,
        Failed: -1,
        Aborted: -2,
    });

    const ListAction = Object.freeze({

        Add: "add",
        Update: "update",
        Delete: "delete",
    });

    const StateEvent = Object.freeze({

        Subjects: "state:subjects",
        Chapters: "state:chapters",
        Documents: "state:documents",

        SelectedSubjectId: "state:selectedSubjectId",
        SelectedSubject: "state:selectedSubject",

        SelectedChapterId: "state:selectedChapterId",
        SelectedChapter: "state:selectedChapter",

        MainSectionLevel: "state:mainSectionLevel",

        ShowUploadPanel: "state:showUploadPanel",

        Upload: Object.freeze({

            SelectedChapterIds: "state:upload:selectedChapterIds",

            List: "state:upload:list",

            CountsByStatus: "state:upload:countsByStatus",

            ShowQueue: "state:upload:showQueue",
            QueueMinimized: "state:upload:queueMinimized",
        }),

        File: Object.freeze({

            Query: "state:file:query",

            TotalCount: "state:file:totalCount",
            TotalPages: "state:file:totalPages",
        }),
    });

    const LoadEvent = Object.freeze({

        SubjectsStart: "load:subjects:start",
        SubjectsSucceed: "load:subjects:succeed",
        SubjectsFail: "load:subjects:fail",
        SubjectsAbort: "load:subjects:abort",

        SubjectDetailsStart: "load:subjectDetails:start",
        SubjectDetailsSucceed: "load:subjectDetails:succeed",
        SubjectDetailsFail: "load:subjectDetails:fail",
        SubjectDetailsAbort: "load:subjectDetails:abort",

        ChaptersStart: "load:chapters:start",
        ChaptersSucceed: "load:chapters:succeed",
        ChaptersFail: "load:chapters:fail",
        ChaptersAbort: "load:chapters:abort",

        ChapterDetailsStart: "load:chapterDetails:start",
        ChapterDetailsSucceed: "load:chapterDetails:succeed",
        ChapterDetailsFail: "load:chapterDetails:fail",
        ChapterDetailsAbort: "load:chapterDetails:abort",

        DocumentsStart: "load:documents:start",
        DocumentsSucceed: "load:documents:succeed",
        DocumentsFail: "load:documents:fail",
        DocumentsAbort: "load:documents:abort",
    });

    const OperationEvent = Object.freeze({

        UploadSucceed: "op:upload:succeed",
        UploadFail: "op:upload:fail",

        DeleteSucceed: "op:document:delete:succeed",
        DeleteFail: "op:document:delete:fail",
    });

    /* =========================================================
       STATE
       ========================================================= */

    const state = {

        subjects: [],
        chapters: [],
        documents: [],

        selectedSubjectId: null,
        selectedChapterId: null,

        selectedSubject: null,
        selectedChapter: null,

        mainSectionLevel: MainSectionLevel.SubjectGrid,

        showUploadPanel: true,

        upload: {

            selectedChapterIds: [],

            list: [],

            inFlightCount: function () { return this.list.filter(x => x.isInFlight).length; },
            settledCount: function () { return this.list.filter(x => x.isSettled).length; },
            outstandingCount: function () { return this.list.filter(x => x.isOutstanding).length; },

            showQueue: false,
            queueMinimized: false,
        },

        file: {

            search: "",
            pageSize: DEFAULT_PAGE_SIZE,
            pageIndex: 1,

            totalCount: 0,
            totalPages: 1,
        },
    };

    const core = {

        initialized: false,

        callerConnectionId: null,

        concurrencyToken: {
            subjects: null,
            chapters: null,
            documents: null,
            subjectDetails: null,
            chapterDetails: null,
        },

        old: {
            selectedSubjectId: null,
            selectedChapterId: null,
        },
    };

    window.DocumentLibraryState = state;

    /* =========================================================
       STATE TYPES
       ========================================================= */

    class UploadItem {

        constructor({
            file,
            subjectId,
            chapterIds,
        } = {}) {
            this.id = crypto.randomUUID();

            this.file = file;

            this.subjectId = String(subjectId);
            this.chapterIds = Object.freeze(
                Array.from(chapterIds ?? [], String));

            this.progress = 0;
            this.transfer = null;
            this.status = UploadStatus.Pending;
            this.error = null;
        }

        get isSettled() {
            return this.status === UploadStatus.Succeeded
                || this.status === UploadStatus.Failed
                || this.status === UploadStatus.Aborted;
        }

        get isOutstanding() {
            return this.status === UploadStatus.Pending
                || this.status === UploadStatus.Starting
                || this.status === UploadStatus.Active;
        }

        get isInFlight() {
            return this.status === UploadStatus.Starting
                || this.status === UploadStatus.Active;
        }

        get canAbort() {
            return this.isOutstanding;
        }

        get canRetry() {
            return this.status === UploadStatus.Failed
                || this.status === UploadStatus.Aborted;
        }

        get canCancel() {
            return this.isOutstanding;
        }

        get canRemove() {
            return this.isSettled;
        }

        get statusName() {
            return getKey(UploadStatus, this.status) ?? "Pending";
        }
    }

    /* =========================================================
       STATE MUTATORS
       ========================================================= */

    function mutateState_Subjects(val, raiseEvent = true) {

        state.subjects = Array.from(val ?? []);
        if (raiseEvent) {
            $(document).trigger(StateEvent.Subjects);
        }
    }

    function mutateState_SelectedSubjectId(val, raiseEvent = true) {

        core.old.selectedSubjectId = state.selectedSubjectId;
        state.selectedSubjectId = val ? val + "" : null;
        if (raiseEvent) {
            $(document).trigger(StateEvent.SelectedSubjectId);
        }
    }

    function mutateState_SelectedSubject(val, raiseEvent = true) {

        state.selectedSubject = val ?? null;
        if (raiseEvent) {
            $(document).trigger(StateEvent.SelectedSubject);
        }
    }

    function mutateState_Chapters(val, raiseEvent = true) {

        const chapters = Array.from(val ?? []);
        chapters.forSubjectId = val?.forSubjectId ?? null;
        state.chapters = chapters;

        if (raiseEvent) {
            $(document).trigger(StateEvent.Chapters);
        }
    }

    function mutateState_SelectedChapterId(val, raiseEvent = true) {

        core.old.selectedChapterId = state.selectedChapterId;
        state.selectedChapterId = val ? val + "" : null;
        if (raiseEvent) {
            $(document).trigger(StateEvent.SelectedChapterId);
        }
    }

    function mutateState_SelectedChapter(val, raiseEvent = true) {

        state.selectedChapter = val ?? null;
        if (raiseEvent) {
            $(document).trigger(StateEvent.SelectedChapter);
        }
    }

    function mutateState_MainSectionLevel(val, raiseEvent = true) {

        state.mainSectionLevel = getValue(MainSectionLevel, val) ?? MainSectionLevel.SubjectGrid;
        if (raiseEvent) {
            $(document).trigger(StateEvent.MainSectionLevel);
        }
    }

    function mutateState_ShowUploadPanel(val, raiseEvent = true) {

        state.showUploadPanel = !!val;
        if (raiseEvent) {
            $(document).trigger(StateEvent.ShowUploadPanel);
        }
    }

    function mutateState_Upload_SelectedChapterIds(val, raiseEvent = true) {

        state.upload.selectedChapterIds = Array.from(val ?? [], String);
        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.SelectedChapterIds);
        }
    }

    function mutateState_Upload_List_Enqueue(files, raiseEvent = true) {

        if (!files || !(files.length > 0)) return;

        const ids = [];

        for (const file of Array.from(files)) {

            const uploadItem = new UploadItem({
                file,
                subjectId: state.selectedSubjectId,
                chapterIds: state.upload.selectedChapterIds,
            });

            state.upload.list.push(uploadItem);
            ids.push(uploadItem.id);
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Add, items: ids });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return ids;
    }

    function mutateState_Upload_List_Dequeue(raiseEvent = true) {

        const uploadItem = state.upload.list.find(x => x.status === UploadStatus.Pending);
        if (!uploadItem) return;

        uploadItem.status = UploadStatus.Starting;

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: [uploadItem.id] });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItem;
    }

    function mutateState_Upload_List_Start(idTransfers, raiseEvent = true) {

        // idTransfers: [{ id: string(uuid), transfer: XMLHttpRequest }]
        if (!idTransfers || !(idTransfers.length > 0)) return;

        const transferLookup = new Map(Array.from(idTransfers).map(x => [x.id, x.transfer]));
        if (transferLookup.size === 0) return;

        const uploadItems = state.upload.list.filter(x => transferLookup.has(x.id) && x.status === UploadStatus.Starting);
        if (uploadItems.length === 0) return;

        for (const item of uploadItems) {
            const transfer = transferLookup.get(item.id);
            if (!transfer) continue;
            item.transfer = transfer;
            item.status = UploadStatus.Active;
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: uploadItems.map(x => x.id) });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Progress(idPercents, raiseEvent = true) {

        // idPercents: [{ id: string(uuid), percent: number }]
        if (!idPercents || !(idPercents.length > 0)) return;

        const percentLookup = new Map(Array.from(idPercents)
            .filter(x => x.percent >= 0 && x.percent <= 100)
            .map(x => [x.id, x.percent]));
        if (percentLookup.size === 0) return;

        const uploadItems = state.upload.list.filter(x => percentLookup.has(x.id) && x.status === UploadStatus.Active);
        if (uploadItems.length === 0) return;

        for (const item of uploadItems) {
            const percent = percentLookup.get(item.id);
            if (!percent) continue;
            item.progress = percent;
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: uploadItems.map(x => x.id) });
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Succeed(ids, raiseEvent = true) {

        if (!ids || !(ids.length > 0)) return;

        const idSet = new Set(Array.from(ids, String));
        const uploadItems = state.upload.list.filter(x => idSet.has(x.id) && x.status === UploadStatus.Active);
        if (uploadItems.length === 0) return;

        for (const item of uploadItems) {
            item.progress = 100;
            item.status = UploadStatus.Succeeded;
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: uploadItems.map(x => x.id) });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Fail(ids, raiseEvent = true) {

        if (!ids || !(ids.length > 0)) return;

        const idSet = new Set(Array.from(ids, String));
        const uploadItems = state.upload.list.filter(x => idSet.has(x.id) && x.isInFlight);
        if (uploadItems.length === 0) return;

        for (const item of uploadItems) {
            item.status = UploadStatus.Failed;
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: uploadItems.map(x => x.id) });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Abort(ids, raiseEvent = true) {

        if (!ids || !(ids.length > 0)) return;

        const idSet = new Set(Array.from(ids, String));
        const uploadItems = state.upload.list.filter(x => idSet.has(x.id) && x.canAbort);
        if (uploadItems.length === 0) return;

        for (const item of uploadItems) {
            item.status = UploadStatus.Aborted;
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: uploadItems.map(x => x.id) });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Retry(ids, raiseEvent = true) {

        if (!ids || !(ids.length > 0)) return;

        const idSet = new Set(Array.from(ids, String));
        const uploadItems = state.upload.list.filter(x => idSet.has(x.id) && x.canRetry);
        if (uploadItems.length === 0) return;

        for (const item of uploadItems) {
            item.progress = 0;
            item.transfer = null;
            item.status = UploadStatus.Pending;
            item.error = null;
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Update, items: uploadItems.map(x => x.id) });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Remove(ids, raiseEvent = true) {

        if (!ids || !(ids.length > 0)) return;

        const idSet = new Set(Array.from(ids, String));
        const uploadItems = state.upload.list.filter(x => idSet.has(x.id) && x.canRemove);
        if (uploadItems.length === 0) return;

        const removedIdSet = new Set(uploadItems.map(x => x.id));

        for (let i = state.upload.list.length - 1; i >= 0; i--) {
            if (removedIdSet.has(state.upload.list[i].id))
                state.upload.list.splice(i, 1);
        }

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Delete, items: uploadItems.map(x => x.id) });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }

        return uploadItems;
    }

    function mutateState_Upload_List_Clear(raiseEvent = true) {

        state.upload.list.length = 0;

        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.List, { action: ListAction.Delete, items: null /* all items */ });
            $(document).trigger(StateEvent.Upload.CountsByStatus);
        }
    }

    function mutateState_Upload_ShowQueue(val, raiseEvent = true) {

        state.upload.showQueue = !!val;
        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.ShowQueue);
        }
    }

    function mutateState_Upload_QueueMinimized(val, raiseEvent = true) {

        state.upload.queueMinimized = !!val;
        if (raiseEvent) {
            $(document).trigger(StateEvent.Upload.QueueMinimized);
        }
    }

    function mutateState_File_Search(val, raiseEvent = true) {

        state.file.search = val ? val + "" : "";
        if (raiseEvent) {
            $(document).trigger(StateEvent.File.Query);
        }
    }

    function mutateState_File_PageSize(val, raiseEvent = true) {

        state.file.pageSize = +val ? Math.max(val, 1) : DEFAULT_PAGE_SIZE;
        if (raiseEvent) {
            $(document).trigger(StateEvent.File.Query);
        }
    }

    function mutateState_File_PageIndex(val, raiseEvent = true) {

        state.file.pageIndex = +val ? Math.max(val, 1) : 1;
        if (raiseEvent) {
            $(document).trigger(StateEvent.File.Query);
        }
    }

    function mutateState_File_TotalCount(val, raiseEvent = true) {

        state.file.totalCount = +val ? Math.max(val, 0) : 0;
        if (raiseEvent) {
            $(document).trigger(StateEvent.File.TotalCount);
        }
    }

    function mutateState_File_TotalPages(val, raiseEvent = true) {

        state.file.totalPages = +val ? Math.max(val, 0) : 0;
        if (raiseEvent) {
            $(document).trigger(StateEvent.File.TotalPages);
        }
    }

    function mutateState_Documents(val, raiseEvent = true) {

        const documents = Array.from(val ?? []);

        documents.forSubjectId = val?.forSubjectId ?? null;
        documents.forChapterId = val?.forChapterId ?? null;
        documents.forSearch = val?.forSearch ?? "";

        state.documents = documents;

        if (raiseEvent) {
            $(document).trigger(StateEvent.Documents);
        }
    }

    /* =========================================================
       DOM
       ========================================================= */

    const ui = Object.freeze({

        root: $(document),

        uploadBadge: $("#uploadPrivilegeBadge"),

        subjectSidebar: $("#subjectSidebarContent"),
        chapterSidebar: $("#chapterSidebarContent"),

        selectSubjectRow: {
            sel: ".js-select-subject",
            elem: function () { return ui.subjectSidebar.find(this.sel); },
        },
        selectChapterRow: {
            sel: ".js-select-chapter",
            elem: function () { return ui.chapterSidebar.find(this.sel); },
        },

        mainBreadcrumb: $("#mainBreadcrumb"),

        mainBackBtn: $("#mainBackBtn"),
        mainForwardBtn: $("#mainForwardBtn"),

        subjectGrid: $("#subjectGridLayer"),
        subjectDetails: $("#subjectDetailsLayer"),
        chapterDetails: $("#chapterDetailsLayer"),

        selectSubjectCard: {
            sel: ".js-select-subject",
            elem: function () { return ui.subjectGrid.find(this.sel); },
        },

        uploadToggle: $("#showUploadBtn"),
        uploadChevron: $("#showUploadChevron"),

        uploadContainer: $("#uploadSectionContainer"),
        uploadPanel: $("#uploadSection"),

        uploadSubjectSummary: $("#uploadSubjectSummary"),

        dropZone: $("#dropZone"),
        fileInput: $("#fileInput"),
        browseBtn: $("#browseBtn"),

        uploadChapterTags: $("#uploadChapterTags"),

        chapterTag: {
            sel: ".js-chapter-tag",
            elem: function () { return ui.uploadPanel.find(this.sel); },
        },

        uploadQueue: $("#uploadQueue"),

        uploadHeader: $("#uploadHeader"),
        uploadQueueSummary: $("#uploadQueueSummary"),

        uploadSettledCount: $("#uploadSettledCount"),
        uploadTotalCount: $("#uploadTotalCount"),
        uploadTotalCountBadge: $("#uploadTotalCountBadge"),

        uploadTotalProgress: $("#uploadTotalProgress"),
        uploadTotalProgressRing: $("#uploadTotalProgressRing"),
        uploadTotalProgressIcon: $("#uploadTotalProgressIcon"),

        uploadMinimizeBtn: $("#uploadMinimizeBtn"),
        uploadMinimizeIcon: $("#uploadMinimizeIcon"),

        uploadItemSection: $("#uploadQueueItemSection"),
        uploadList: $("#uploadList"),

        uploadQuitBtn: $("#uploadQuitBtn"),
        uploadQuitIcon: $("#uploadQuitIcon"),
        uploadQuitLabel: $("#uploadQuitLabel"),

        uploadItem: {
            sel: ".js-upload-item",
            data: "data-upload-id",
            elem: function (id) { return ui.uploadList.find(`${this.sel}[${this.data}="${id}"]`); },
        },
        uploadPercent: {
            sel: ".js-upload-percent",
            elem: function (id) { return ui.uploadItem.elem(id).find(this.sel); },
        },
        uploadProgressBar: {
            sel: ".js-upload-progress-bar",
            elem: function (id) { return ui.uploadItem.elem(id).find(this.sel); },
        },
        uploadRetryBtn: {
            sel: ".js-retry-upload",
            elem: function (id) { return ui.uploadItem.elem(id).find(this.sel); },
        },
        uploadCancelBtn: {
            sel: ".js-cancel-upload",
            elem: function (id) { return ui.uploadItem.elem(id).find(this.sel); },
        },
        uploadRemoveBtn: {
            sel: ".js-remove-upload",
            elem: function (id) { return ui.uploadItem.elem(id).find(this.sel); },
        },

        documentDrawer: $("#documentDrawerSection"),

        search: $("#searchInput"),
        documentList: $("#documentList"),
        pagination: $("#documentPagination"),

        // TODO: By marker classes -- Document status badge

        deleteDocBtn: {
            sel: ".js-delete-document",
            elem: function () { return ui.documentList.find(this.sel); },
        },

        setPageBtn: {
            sel: ".js-page",
            elem: function () { return ui.pagination.find(this.sel); },
        },
        prevPageBtn: {
            sel: ".js-prev",
            elem: function () { return ui.pagination.find(this.sel); },
        },
        nextPageBtn: {
            sel: ".js-next",
            elem: function () { return ui.pagination.find(this.sel); },
        },

        retrySubjectsBtn: {
            sel: ".js-retry-subjects",
            elem: function () { return ui.root.find(this.sel); },
        },
        retrySubjectDetailsBtn: {
            sel: ".js-retry-subject-details",
            elem: function () { return ui.subjectDetails.find(this.sel); },
        },
        retryChaptersBtn: {
            sel: ".js-retry-chapters",
            elem: function () { return ui.root.find(this.sel); },
        },
        retryChapterDetailsBtn: {
            sel: ".js-retry-chapter-details",
            elem: function () { return ui.chapterDetails.find(this.sel); },
        },
        retryDocumentsBtn: {
            sel: ".js-retry-documents",
            elem: function () { return ui.documentDrawer.find(this.sel); },
        },

        toastContainer: $("#toastContainer"),
    });

    /* =========================================================
       STARTUP
       ========================================================= */

    $(document).ready(init);

    async function init() {

        if (core.initialized)
            return;

        try {
            removeBootstrap();

            await waitForConnection();

            bindEvents();
            initState();

            await loadSubjects();

            core.initialized = true;
        }
        catch (err) {
            console.error(err);
        }
    }

    function removeBootstrap() {

        for (const sheet of document.styleSheets) {
            if (sheet.href.includes("bootstrap")) {
                sheet.disabled = true;
            }
        }
    }

    async function waitForConnection() {

        while (!window.ResourceConnectionId) {
            await delay(100);
        }

        core.callerConnectionId = window.ResourceConnectionId;
    }

    function delay(ms) {
        return new Promise(r => setTimeout(r, ms));
    }

    function initState() {

        mutateState_SelectedSubjectId(null);
        mutateState_MainSectionLevel(MainSectionLevel.SubjectGrid);
        mutateState_ShowUploadPanel(true);
    }

    /* =========================================================
       EVENT BINDING
       ========================================================= */

    function bindEvents() {

        bindUiEvents();
        bindRealtimeEvents();
        bindStateEvents();
        bindLoadEvents();
        bindOperationEvents();
    }

    function bindUiEvents() {

        ui.subjectSidebar.on("click", ui.selectSubjectRow.sel, onSubjectSelected);
        ui.chapterSidebar.on("click", ui.selectChapterRow.sel, onChapterSelected);

        ui.mainBackBtn.on(
            "click",
            e => {
                mutateState_MainSectionLevel(Math.max(state.mainSectionLevel - 1, 0));
            });

        ui.mainForwardBtn.on(
            "click",
            e => {
                mutateState_MainSectionLevel(Math.min(state.mainSectionLevel + 1, MainSectionLevel.ChapterDetails));
            });

        ui.subjectGrid.on("click", ui.selectSubjectCard.sel, onSubjectSelected);

        ui.uploadToggle.on(
            "click",
            e => {
                mutateState_ShowUploadPanel(!state.showUploadPanel);
            });

        ui.dropZone.on(
            "dragenter dragover",
            e => {
                e.preventDefault();
                e.stopPropagation();
                $(e.currentTarget).addClass("dragover");
            }
        );

        ui.dropZone.on(
            "dragleave drop",
            e => {
                e.preventDefault();
                e.stopPropagation();
                $(e.currentTarget).removeClass("dragover");
            });

        ui.dropZone.on(
            "drop",
            e => {
                const files = [...e.originalEvent.dataTransfer.files];
                queueUploads(files);
            });

        ui.browseBtn.on(
            "click",
            e => ui.fileInput.trigger("click"));

        ui.fileInput.on(
            "change",
            e => {
                queueUploads(e.target.files);
                e.target.value = "";
            });

        ui.uploadPanel.on(
            "click",
            ui.chapterTag.sel,
            e => {
                const chapterId = $(e.currentTarget).data("chapter-id") + "";

                const selected = new Set(state.upload.selectedChapterIds);

                if (selected.has(chapterId))
                    selected.delete(chapterId);
                else
                    selected.add(chapterId);

                mutateState_Upload_SelectedChapterIds([...selected]);
            });

        ui.uploadList.on("click", ui.uploadRetryBtn.sel, onUploadItemRetry);
        ui.uploadList.on("click", ui.uploadCancelBtn.sel, onUploadItemCancel);
        ui.uploadList.on("click", ui.uploadRemoveBtn.sel, onUploadItemRemove);

        ui.uploadMinimizeBtn.on(
            "click",
            () => {
                mutateState_Upload_QueueMinimized(!state.upload.queueMinimized);
            });

        ui.uploadQuitBtn.on("click", onUploadQueueQuit);

        ui.search.on(
            "input",
            e => {
                mutateState_File_PageIndex(1, false);
                mutateState_File_Search($(e.target).val().trim());
            });

        ui.documentList.on("click", ui.deleteDocBtn.sel, deleteDocument);

        ui.pagination.on("click", ui.setPageBtn.sel, onPageIndexChanged);
        ui.pagination.on("click", ui.prevPageBtn.sel, onPageIndexChanged);
        ui.pagination.on("click", ui.nextPageBtn.sel, onPageIndexChanged);

        ui.root.on(
            "click",
            ui.retrySubjectsBtn.sel,
            onRetrySubjects);

        ui.subjectDetails.on(
            "click",
            ui.retrySubjectDetailsBtn.sel,
            onRetrySubjectDetails);

        ui.root.on(
            "click",
            ui.retryChaptersBtn.sel,
            onRetryChapters);

        ui.chapterDetails.on(
            "click",
            ui.retryChapterDetailsBtn.sel,
            onRetryChapterDetails);

        ui.documentDrawer.on(
            "click",
            ui.retryDocumentsBtn.sel,
            onRetryDocuments);
    }

    function bindRealtimeEvents() {

        $(document).on("resource:changed", onRealtimeResourceUpdate);
        $(document).on("resource:reconnected", onResourceHubReconnected);
        $(document).on("resource:disconnected", onResourceHubDisconnected);

        $(document).on("document:status", onDocumentStatusUpdate);
    }

    function bindStateEvents() {

        $(document).on(StateEvent.Subjects, updateState_RefindSelectedSubject);
        $(document).on(StateEvent.Subjects, updateUi_SubjectSidebar);
        $(document).on(StateEvent.Subjects, updateUi_SubjectGrid);

        $(document).on(StateEvent.SelectedSubjectId, updateState_ResolveSelectedSubject);
        $(document).on(StateEvent.SelectedSubjectId, updateState_ResetAndResolveChapters);
        $(document).on(StateEvent.SelectedSubjectId, updateState_ResetAndResolveDocuments);
        $(document).on(StateEvent.SelectedSubjectId, () => updateState_MainSectionLevel(MainSectionLevel.SubjectDetails));
        $(document).on(StateEvent.SelectedSubjectId, updateUi_SubjectSidebar);
        $(document).on(StateEvent.SelectedSubjectId, updateUi_MainBreadcrumb);
        $(document).on(StateEvent.SelectedSubjectId, updateUi_SubjectGrid);
        $(document).on(StateEvent.SelectedSubjectId, updateUi_DocumentDrawer);

        $(document).on(StateEvent.SelectedSubject, updateUi_SubjectDetails);
        $(document).on(StateEvent.SelectedSubject, updateUi_ChapterDetails);
        $(document).on(StateEvent.SelectedSubject, updateUi_UploadBadge);
        $(document).on(StateEvent.SelectedSubject, updateUi_UploadToggle);
        $(document).on(StateEvent.SelectedSubject, updateUi_UploadPanel_Visibility);
        $(document).on(StateEvent.SelectedSubject, updateUi_UploadPanel_SubjectSummary);

        $(document).on(StateEvent.Chapters, updateState_RefindSelectedChapter);
        $(document).on(StateEvent.Chapters, updateState_SortChapters);
        $(document).on(StateEvent.Chapters, updateUi_ChapterSidebar);
        $(document).on(StateEvent.Chapters, updateUi_UploadPanel_ChapterTags);

        $(document).on(StateEvent.SelectedChapterId, updateState_ResolveSelectedChapter);
        $(document).on(StateEvent.SelectedChapterId, updateState_ResetAndResolveDocuments);
        $(document).on(StateEvent.SelectedChapterId, () => updateState_MainSectionLevel(MainSectionLevel.ChapterDetails));
        $(document).on(StateEvent.SelectedChapterId, updateUi_ChapterSidebar);
        $(document).on(StateEvent.SelectedChapterId, updateUi_MainBreadcrumb);

        $(document).on(StateEvent.SelectedChapter, updateUi_ChapterDetails);

        $(document).on(StateEvent.MainSectionLevel, updateUi_MainSection);

        $(document).on(StateEvent.ShowUploadPanel, updateUi_UploadPanel_Visibility);

        $(document).on(StateEvent.Upload.SelectedChapterIds, updateUi_UploadPanel_ChapterTags);

        $(document).on(StateEvent.Upload.List, (_, { action, items } = {}) => updateUi_UploadQueue_Row(action, items));
        $(document).on(StateEvent.Upload.List, (_, { action } = {}) => updateState_ToggleUploadQueueOnListChange(action));

        $(document).on(StateEvent.Upload.CountsByStatus, updateUi_UploadQueue_Header);
        $(document).on(StateEvent.Upload.ShowQueue, updateUi_UploadQueue_Visibility);
        $(document).on(StateEvent.Upload.QueueMinimized, updateUi_UploadQueue_ItemSection);

        $(document).on(StateEvent.File.Query, updateState_PerformDocumentSearch);

        $(document).on(StateEvent.Documents, updateUi_DocumentList);
        $(document).on(StateEvent.Documents, updateUi_Pagination);

        $(document).on(StateEvent.File.TotalCount, updateUi_DocumentList);

        $(document).on(StateEvent.File.TotalPages, updateUi_Pagination);
    }

    function bindLoadEvents() {

        // Subjects
        $(document).on(LoadEvent.SubjectsStart, updateUi_SubjectSidebar_Loading);
        $(document).on(LoadEvent.SubjectsStart, updateUi_SubjectGrid_Loading);

        $(document).on(LoadEvent.SubjectsSucceed, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.SubjectsSucceed); });

        $(document).on(LoadEvent.SubjectsFail, updateUi_SubjectSidebar_LoadFailed);
        $(document).on(LoadEvent.SubjectsFail, updateUi_SubjectGrid_LoadFailed);
        $(document).on(LoadEvent.SubjectsFail, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.SubjectsFail); });

        $(document).on(LoadEvent.SubjectsAbort, (_, { keepState } = {}) => { if (!keepState) updateState_RollBackSubjects(); });
        $(document).on(LoadEvent.SubjectsAbort, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.SubjectsAbort); });

        // Subject Details
        $(document).on(LoadEvent.SubjectDetailsStart, updateUi_SubjectDetails_Loading);

        $(document).on(LoadEvent.SubjectDetailsSucceed, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.SubjectDetailsSucceed); });

        $(document).on(LoadEvent.SubjectDetailsFail, updateUi_SubjectDetails_LoadFailed);
        $(document).on(LoadEvent.SubjectDetailsFail, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.SubjectDetailsFail); });

        $(document).on(LoadEvent.SubjectDetailsAbort, (_, { keepState }) => { if (!keepState) updateState_RollBackSelectedSubject(); });
        $(document).on(LoadEvent.SubjectDetailsAbort, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.SubjectDetailsAbort); });

        // Chapters
        $(document).on(LoadEvent.ChaptersStart, updateUi_ChapterSidebar_Loading);
        $(document).on(LoadEvent.ChaptersStart, updateUi_UploadPanel_ChapterTags_Loading);

        $(document).on(LoadEvent.ChaptersSucceed, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.ChaptersSucceed); });

        $(document).on(LoadEvent.ChaptersFail, updateUi_ChapterSidebar_LoadFailed);
        $(document).on(LoadEvent.ChaptersFail, updateUi_UploadPanel_ChapterTags_LoadFailed);
        $(document).on(LoadEvent.ChaptersFail, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.ChaptersFail); });

        $(document).on(LoadEvent.ChaptersAbort, (_, { keepState } = {}) => { if (!keepState) updateState_RollBackChapters(); });
        $(document).on(LoadEvent.ChaptersAbort, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.ChaptersAbort); });

        // Chapter Details
        $(document).on(LoadEvent.ChapterDetailsStart, updateUi_ChapterDetails_Loading);

        $(document).on(LoadEvent.ChapterDetailsSucceed, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.ChapterDetailsSucceed); });

        $(document).on(LoadEvent.ChapterDetailsFail, updateUi_ChapterDetails_LoadFailed);
        $(document).on(LoadEvent.ChapterDetailsFail, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.ChapterDetailsFail); });

        $(document).on(LoadEvent.ChapterDetailsAbort, (_, { keepState } = {}) => { if (!keepState) updateState_RollBackSelectedChapter(); });
        $(document).on(LoadEvent.ChapterDetailsAbort, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.ChapterDetailsAbort); });

        // Documents
        $(document).on(LoadEvent.DocumentsStart, updateUi_DocumentList_Loading);

        $(document).on(LoadEvent.DocumentsSucceed, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.DocumentsSucceed); });

        $(document).on(LoadEvent.DocumentsFail, updateUi_DocumentList_LoadFailed);
        $(document).on(LoadEvent.DocumentsFail, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.DocumentsFail); });

        $(document).on(LoadEvent.DocumentsAbort, (_, { keepState } = {}) => { if (!keepState) updateState_RollBackDocuments(); });
        $(document).on(LoadEvent.DocumentsAbort, (_, { silent } = {}) => { if (!silent) updateUi_ShowToast(LoadEvent.DocumentsAbort); });
    }

    function bindOperationEvents() {
        $(document).on(OperationEvent.UploadSucceed, (_, { fileName } = {}) => { updateUi_ShowToast(OperationEvent.UploadSucceed, { title: fileName ? `Uploaded '${fileName}'` : null, }); });
        $(document).on(OperationEvent.UploadFail, (_, { fileName } = {}) => { updateUi_ShowToast(OperationEvent.UploadFail, { title: fileName ? `Failed to upload '${fileName}'` : null, }); });

        $(document).on(OperationEvent.DeleteSucceed, (_, { title } = {}) => { updateUi_ShowToast(OperationEvent.DeleteSucceed, { title: title ? `Deleted '${title}'` : null, }); });
        $(document).on(OperationEvent.DeleteFail, (_, { title } = {}) => { updateUi_ShowToast(OperationEvent.DeleteFail, { title: title ? `Failed to delete '${title}'` : null, }); });
    }

    /* =========================================================
       UI EVENT HANDLERS
       ========================================================= */

    function onSubjectSelected(e) {
        let subjectId = $(e.currentTarget).data("subject-id") + "";
        if (subjectId === state.selectedSubjectId) subjectId = null;
        mutateState_SelectedSubjectId(subjectId);
    }

    function onChapterSelected(e) {
        let chapterId = $(e.currentTarget).data("chapter-id") + "";
        if (chapterId === state.selectedChapterId) chapterId = null;
        mutateState_SelectedChapterId(chapterId);
    }

    function onUploadItemRetry(e) {

        const uploadId = $(e.currentTarget).attr(ui.uploadItem.data);
        if (!uploadId) return;

        const uploadItem = state.upload.list.find(
            x => x.id === uploadId);

        retryUploadItem(uploadItem);
    }

    function onUploadItemCancel(e) {

        const uploadId = $(e.currentTarget).attr(ui.uploadItem.data);
        if (!uploadId) return;

        const uploadItem = state.upload.list.find(
            x => x.id === uploadId);

        cancelUploadItem(uploadItem);
    }

    function onUploadItemRemove(e) {

        const uploadId = $(e.currentTarget).attr(ui.uploadItem.data);
        if (!uploadId) return;

        const uploadItem = state.upload.list.find(x => x.id === uploadId);

        removeUploadItem(uploadItem);
    }

    function onUploadQueueQuit() {

        const outstandingItems = state.upload.list.filter(
            x => x.isOutstanding);

        if (outstandingItems.length > 0) {
            cancelUploadItems(outstandingItems);
            return;
        }

        mutateState_Upload_List_Clear();
        mutateState_Upload_ShowQueue(false);
    }

    function onPageIndexChanged(e) {
        const pageIndex = $(e.currentTarget).data("page-index");
        if (pageIndex === state.file.pageIndex) return;
        mutateState_File_PageIndex(pageIndex);
    }

    function onRetrySubjects(e) {

        e.preventDefault();
        e.stopPropagation();

        loadSubjects();
    }

    function onRetrySubjectDetails(e) {

        e.preventDefault();
        e.stopPropagation();

        loadSubjectDetails();
    }

    function onRetryChapters(e) {

        e.preventDefault();
        e.stopPropagation();

        loadChapters();
    }

    function onRetryChapterDetails(e) {

        e.preventDefault();
        e.stopPropagation();

        loadChapterDetails();
    }

    function onRetryDocuments(e) {

        e.preventDefault();
        e.stopPropagation();

        loadDocuments();
    }

    /* =========================================================
       REALTIME EVENT HANDLERS
       ========================================================= */

    async function onRealtimeResourceUpdate(_, update) {

        switch (update.resourceType) {

            case ResourceType.Subject:

                await loadSubjects(false);

                if (update.resourceId === state.selectedSubjectId)
                    await loadSubjectDetails(false);

                break;

            case ResourceType.Chapter:

                await loadChapters(false);

                if (update.resourceId === state.selectedChapterId)
                    await loadChapterDetails(false);

                if (update.properties["subjectId"] === state.selectedSubjectId) {
                    await loadSubjectDetails(false);
                }

                break;

            case ResourceType.Document:
            case ResourceType.User:

                await loadDocuments(false);
                break;

            case ResourceType.Membership:

                if (update.properties["userId"] === Razor.userId) {
                    await loadSubjects(false);
                }

                break;
        }
    }

    function onResourceHubReconnected() {

        core.callerConnectionId = window.ResourceConnectionId;
    }

    function onResourceHubDisconnected() {

        core.callerConnectionId = "";
    }

    function onDocumentStatusUpdate(_, update) {

        // FIXME: Mutate document state instead
        const $badge = $(`#status-badge-${update.id}`);
        if ($badge.length === 0) return;

        const settings = StatusSettings[StatusNames[update.status]];
        if (!settings) return;

        const text =
            update.progress
                ? settings.text.replace("{{PROGRESS}}", update.progress.toFixed(2))
                : settings.text.replace("({{PROGRESS}}%)", "").trim();

        $badge.html(`
            <i class="fas ${settings.iconClass}"></i>
            ${text}
        `);

        $badge.removeClass().addClass(`document-status-badge ${settings.className}`);
    }

    /* =========================================================
       STATE MUTATION CASCADES
       ========================================================= */

    function updateState_RefindSelectedSubject() {

        if (!state.selectedSubjectId)
            return;

        const stillExists = state.subjects.some(x =>
            x.id === state.selectedSubjectId);

        if (!stillExists) {
            mutateState_SelectedSubjectId(null);
        }
    }

    function updateState_ResolveSelectedSubject() {

        if (state.selectedSubject?.id === state.selectedSubjectId)
            mutateState_SelectedSubject(state.selectedSubject);
        else
            loadSubjectDetails();
    }

    function updateState_ResetAndResolveChapters() {

        mutateState_SelectedChapterId(null, false);
        mutateState_SelectedChapter(null, false);
        mutateState_Upload_SelectedChapterIds([], false);

        if (state.chapters.forSubjectId === state.selectedSubjectId)
            mutateState_Chapters(state.chapters);
        else
            loadChapters();
    }

    function updateState_RefindSelectedChapter() {

        if (!state.selectedChapterId)
            return;

        const stillExists = state.chapters.some(x =>
            x.id === state.selectedChapterId);

        if (!stillExists) {
            mutateState_SelectedChapterId(null);
        }
    }

    function updateState_SortChapters() {

        const sorted = state.chapters.toSorted((a, b) =>
            a.chapterNumber - b.chapterNumber);

        sorted.forSubjectId = state.chapters.forSubjectId;

        mutateState_Chapters(sorted, false);
    }

    function updateState_ResolveSelectedChapter() {

        if (state.selectedChapter?.id === state.selectedChapterId)
            mutateState_SelectedChapter(state.selectedChapter);
        else
            loadChapterDetails();
    }

    function updateState_ResetAndResolveDocuments() {

        mutateState_File_Search("", false);
        mutateState_File_PageIndex(1, false);

        if (state.documents.forSubjectId === state.selectedSubjectId
            && state.documents.forChapterId === state.selectedChapterId)
            mutateState_Documents(state.documents);
        else
            loadDocuments();
    }

    function updateState_PerformDocumentSearch() {

        loadDocuments();
    }

    function updateState_MainSectionLevel(suggestedLevel) {

        let level = getValue(MainSectionLevel, suggestedLevel) ?? MainSectionLevel.SubjectGrid;

        if (level === MainSectionLevel.ChapterDetails
            && !state.selectedChapterId) {

            level = MainSectionLevel.SubjectDetails;
        }

        if (level === MainSectionLevel.SubjectDetails
            && !state.selectedSubjectId) {

            level = MainSectionLevel.SubjectGrid;
        }

        mutateState_MainSectionLevel(level);
    }

    function updateState_ToggleUploadQueueOnListChange(action) {

        if (action === ListAction.Add
            && !state.upload.showQueue) {

            mutateState_Upload_QueueMinimized(false);
            mutateState_Upload_ShowQueue(true);
            return;
        }

        if (action === ListAction.Delete
            && state.upload.list.length === 0
            && state.upload.showQueue) {

            mutateState_Upload_ShowQueue(false);
            return;
        }
    }

    function updateState_RollBackSubjects() {

        mutateState_Subjects(state.subjects);
    }

    function updateState_RollBackSelectedSubject() {

        mutateState_SelectedSubjectId(core.old.selectedSubjectId);
    }

    function updateState_RollBackChapters() {

        mutateState_Chapters(state.chapters);
    }

    function updateState_RollBackSelectedChapter() {

        mutateState_SelectedChapterId(core.old.selectedChapterId);
    }

    function updateState_RollBackDocuments() {

        mutateState_Documents(state.documents);
    }

    /* ==========================================================
       UI UPDATE ENTRY POINTS
       ========================================================== */

    function updateUi_UploadBadge() {

        ui.uploadBadge.html(
            DocumentLibraryTemplates.renderUploadPrivilegeBadge(
                state.selectedSubject?.isChief)
        );
    }

    function updateUi_SubjectSidebar() {

        ui.subjectSidebar.html(
            DocumentLibraryTemplates.renderSubjectSidebar(
                state.subjects,
                state.selectedSubjectId)
        );
    }

    function updateUi_SubjectSidebar_Loading() {

        ui.subjectSidebar.html(
            DocumentLibraryTemplates.renderSubjectSidebarLoading()
        );
    }

    function updateUi_SubjectSidebar_LoadFailed() {

        ui.subjectSidebar.html(
            DocumentLibraryTemplates.renderSubjectSidebarLoadFailed()
        );
    }

    function updateUi_ChapterSidebar() {

        ui.chapterSidebar.html(
            DocumentLibraryTemplates.renderChapterSidebar(
                state.chapters,
                state.selectedChapterId)
        );
    }

    function updateUi_ChapterSidebar_Loading() {

        ui.chapterSidebar.html(
            DocumentLibraryTemplates.renderChapterSidebarLoading()
        );
    }

    function updateUi_ChapterSidebar_LoadFailed() {

        ui.chapterSidebar.html(
            DocumentLibraryTemplates.renderChapterSidebarLoadFailed()
        );
    }

    function updateUi_MainBreadcrumb() {

        ui.mainBreadcrumb.html(
            DocumentLibraryTemplates.renderMainSectionBreadcrumb(
                state.subjects.find(x => x.id === state.selectedSubjectId),
                state.chapters.find(x => x.id === state.selectedChapterId))
        );
    }

    function updateUi_MainSection() {

        const active = "opacity-100 translate-x-0 pointer-events-auto";

        const hide = "opacity-0 pointer-events-none";
        const moveLeft = "absolute -translate-x-8";
        const moveRight = "absolute translate-x-8";

        const exitLeft = [hide, moveLeft].join(" ");
        const exitRight = [hide, moveRight].join(" ");
        const inactive = [hide, moveLeft, moveRight].join(" ");

        switch (state.mainSectionLevel) {

            case MainSectionLevel.SubjectGrid:

                ui.mainBackBtn.prop("disabled", true);
                ui.mainForwardBtn.prop("disabled", !state.selectedSubjectId);

                ui.subjectGrid.removeClass(inactive).addClass(active);
                ui.subjectDetails.removeClass(active).addClass(exitRight);
                ui.chapterDetails.removeClass(active).addClass(exitRight);

                break;

            case MainSectionLevel.SubjectDetails:

                ui.mainBackBtn.prop("disabled", false);
                ui.mainForwardBtn.prop("disabled", !state.selectedChapterId);

                ui.subjectGrid.removeClass(active).addClass(exitLeft);
                ui.subjectDetails.removeClass(inactive).addClass(active);
                ui.chapterDetails.removeClass(active).addClass(exitRight);

                break;

            case MainSectionLevel.ChapterDetails:

                ui.mainBackBtn.prop("disabled", false);
                ui.mainForwardBtn.prop("disabled", true);

                ui.subjectGrid.removeClass(active).addClass(exitLeft);
                ui.subjectDetails.removeClass(active).addClass(exitLeft);
                ui.chapterDetails.removeClass(inactive).addClass(active);

                break;

        }
    }

    function updateUi_SubjectGrid() {

        ui.subjectGrid.html(
            DocumentLibraryTemplates.renderSubjectGrid(
                state.subjects,
                state.selectedSubjectId)
        );
    }

    function updateUi_SubjectGrid_Loading() {

        ui.subjectGrid.html(
            DocumentLibraryTemplates.renderSubjectGridLoading()
        );
    }

    function updateUi_SubjectGrid_LoadFailed() {

        ui.subjectGrid.html(
            DocumentLibraryTemplates.renderSubjectGridLoadFailed()
        );
    }

    function updateUi_SubjectDetails() {

        ui.subjectDetails.html(
            DocumentLibraryTemplates.renderSubjectDetails(
                state.selectedSubject)
        );
    }

    function updateUi_SubjectDetails_Loading() {

        ui.subjectDetails.html(
            DocumentLibraryTemplates.renderSubjectDetailsLoading()
        );
    }

    function updateUi_SubjectDetails_LoadFailed() {

        ui.subjectDetails.html(
            DocumentLibraryTemplates.renderSubjectDetailsLoadFailed()
        );
    }

    function updateUi_ChapterDetails() {

        ui.chapterDetails.html(
            DocumentLibraryTemplates.renderChapterDetails(
                state.selectedSubject,
                state.selectedChapter)
        );
    }

    function updateUi_ChapterDetails_Loading() {

        ui.chapterDetails.html(
            DocumentLibraryTemplates.renderChapterDetailsLoading()
        );
    }

    function updateUi_ChapterDetails_LoadFailed() {

        ui.chapterDetails.html(
            DocumentLibraryTemplates.renderChapterDetailsLoadFailed()
        );
    }

    function updateUi_UploadToggle() {

        ui.uploadToggle.toggleClass("hidden", !state.selectedSubject?.isChief);
    }

    function updateUi_UploadPanel_Visibility() {

        const active = "border-emerald-700 bg-emerald-600/10 hover:border-emerald-500 hover:bg-emerald-600/20";
        const inactive = "border-slate-700 bg-slate-900 hover:border-slate-600 hover:bg-slate-800";

        const show = state.selectedSubject?.isChief && state.showUploadPanel;

        ui.uploadToggle.toggleClass(inactive, !show).toggleClass(active, show);
        ui.uploadChevron.toggleClass("rotate-180", show);
        ui.uploadContainer.toggleClass("m-0", !show);

        if (show)
            ui.uploadPanel.slideDown(300);
        else
            ui.uploadPanel.slideUp(300);
    }

    function updateUi_UploadPanel_SubjectSummary() {

        ui.uploadSubjectSummary.html(
            DocumentLibraryTemplates.renderUploadSubjectSummary(
                state.selectedSubject)
        );
    }

    function updateUi_UploadPanel_ChapterTags() {

        ui.uploadChapterTags.html(
            DocumentLibraryTemplates.renderUploadChapterTags(
                state.chapters,
                state.upload.selectedChapterIds)
        );
    }

    function updateUi_UploadPanel_ChapterTags_Loading() {

        ui.uploadChapterTags.html(
            DocumentLibraryTemplates.renderUploadChapterTagsLoading()
        );
    }

    function updateUi_UploadPanel_ChapterTags_LoadFailed() {

        ui.uploadChapterTags.html(
            DocumentLibraryTemplates.renderUploadChapterTagsLoadFailed()
        );
    }

    function updateUi_UploadQueue_Row(action, items) {

        const renderRow = uploadItem =>
            DocumentLibraryTemplates.renderUploadRow(uploadItem);

        /*
         * A null item collection means the caller did not target
         * individual rows. Render the queue's current canonical state.
         *
         * This also handles Clear(), because state.upload.list is empty
         * by the time the event is raised.
         */
        if (items == null) {

            ui.uploadList.html(
                state.upload.list
                    .map(renderRow)
                    .join(""));

            return;
        }

        const ids = Array.from(items, String);

        switch (action) {

            case ListAction.Add:
            case ListAction.Update:

                for (const id of ids) {

                    const uploadItem = state.upload.list.find(
                        x => x.id === id);

                    const currentRow = ui.uploadItem.elem(id);

                    if (!uploadItem) {
                        currentRow.remove();
                        continue;
                    }

                    const markup = renderRow(uploadItem);

                    if (currentRow.length > 0)
                        currentRow.replaceWith(markup);
                    else
                        ui.uploadList.append(markup);
                }

                break;

            case ListAction.Delete:

                for (const id of ids)
                    ui.uploadItem.elem(id).remove();

                break;

            default:

                ui.uploadList.html(
                    state.upload.list
                        .map(renderRow)
                        .join(""));

                break;
        }
    }

    function updateUi_UploadQueue_Header() {

        const uploadItems = state.upload.list;

        const total = uploadItems.length;
        const settled = state.upload.settledCount();
        const outstanding = state.upload.outstandingCount();

        const pending = uploadItems.filter(
            x => x.status === UploadStatus.Pending).length;

        const starting = uploadItems.filter(
            x => x.status === UploadStatus.Starting).length;

        const active = uploadItems.filter(
            x => x.status === UploadStatus.Active).length;

        const succeeded = uploadItems.filter(
            x => x.status === UploadStatus.Succeeded).length;

        const failed = uploadItems.filter(
            x => x.status === UploadStatus.Failed).length;

        const aborted = uploadItems.filter(
            x => x.status === UploadStatus.Aborted).length;

        const finishedPercent = total > 0
            ? Math.round(settled / total * 100)
            : 0;

        const HeaderView = Object.freeze({

            Neutral: {
                label: "No uploads",
                ringClass: "text-slate-400",
                iconClass: "fa-cloud-arrow-up",
                iconColorClass: "text-slate-400",
                badgeClass: "bg-slate-800 text-slate-300",
            },

            Active: {
                label: "Uploads in progress",
                ringClass: "text-sky-400",
                iconClass: "fa-cloud-arrow-up",
                iconColorClass: "text-sky-300",
                badgeClass: "bg-sky-500/10 text-sky-300",
            },

            Success: {
                label: "All uploads succeeded",
                ringClass: "text-emerald-400",
                iconClass: "fa-circle-check",
                iconColorClass: "text-emerald-300",
                badgeClass:
                    "bg-emerald-500/10 text-emerald-300",
            },

            Partial: {
                label: "Some uploads did not succeed",
                ringClass: "text-amber-400",
                iconClass: "fa-triangle-exclamation",
                iconColorClass: "text-amber-300",
                badgeClass: "bg-amber-500/10 text-amber-300",
            },

            Failure: {
                label: "No uploads succeeded",
                ringClass: "text-red-400",
                iconClass: "fa-circle-xmark",
                iconColorClass: "text-red-300",
                badgeClass: "bg-red-500/10 text-red-300",
            },
        });

        let headerView;

        if (total === 0) {
            headerView = HeaderView.Neutral;
        }
        else if (outstanding > 0) {
            headerView = HeaderView.Active;
        }
        else if (succeeded === total) {
            headerView = HeaderView.Success;
        }
        else if (succeeded > 0) {
            headerView = HeaderView.Partial;
        }
        else {
            headerView = HeaderView.Failure;
        }

        ui.uploadSettledCount.text(settled);
        ui.uploadTotalCount.text(total);

        ui.uploadTotalProgress
            .attr("aria-valuenow", finishedPercent)
            .attr(
                "aria-label",
                `${settled} of ${total} uploads finished. `
                + headerView.label);

        ui.uploadTotalProgressRing
            .attr(
                "stroke-dashoffset",
                100 - finishedPercent)
            .removeClass(
                "text-slate-400 "
                + "text-sky-400 "
                + "text-emerald-400 "
                + "text-amber-400 "
                + "text-red-400")
            .addClass(headerView.ringClass);

        ui.uploadTotalProgressIcon
            .removeClass(
                "fa-cloud-arrow-up "
                + "fa-circle-check "
                + "fa-triangle-exclamation "
                + "fa-circle-xmark "
                + "text-slate-400 "
                + "text-sky-300 "
                + "text-emerald-300 "
                + "text-amber-300 "
                + "text-red-300")
            .addClass(
                `${headerView.iconClass} `
                + headerView.iconColorClass);

        ui.uploadTotalCountBadge
            .removeClass(
                "bg-slate-800 text-slate-300 "
                + "bg-sky-500/10 text-sky-300 "
                + "bg-emerald-500/10 text-emerald-300 "
                + "bg-amber-500/10 text-amber-300 "
                + "bg-red-500/10 text-red-300")
            .addClass(headerView.badgeClass);

        let summary;

        if (total === 0) {

            summary = "No uploads";
        }
        else if (outstanding > 0) {

            const parts = [];
            const uploading = starting + active;

            if (uploading > 0)
                parts.push(`${uploading} uploading`);

            if (pending > 0)
                parts.push(`${pending} queued`);

            if (settled > 0)
                parts.push(`${settled} finished`);

            summary = parts.join(" · ");
        }
        else {

            const parts = [];

            if (succeeded > 0)
                parts.push(`${succeeded} uploaded`);

            if (failed > 0)
                parts.push(`${failed} failed`);

            if (aborted > 0)
                parts.push(`${aborted} cancelled`);

            summary = parts.length > 0
                ? parts.join(" · ")
                : "All uploads finished";
        }

        ui.uploadQueueSummary.text(summary);

        const cancelMode = outstanding > 0;

        const cancelClasses =
            "border-red-500/40 bg-red-500/10 "
            + "text-red-300 hover:border-red-400 "
            + "hover:bg-red-500/20 hover:text-red-200";

        const closeClasses =
            "border-slate-700 bg-slate-800 "
            + "text-slate-300 hover:border-slate-500 "
            + "hover:bg-slate-700 hover:text-white";

        ui.uploadQuitBtn
            .toggleClass("hidden", total === 0)
            .toggleClass("flex", total > 0)
            .toggleClass(cancelClasses, cancelMode)
            .toggleClass(closeClasses, !cancelMode)
            .attr(
                "aria-label",
                cancelMode
                    ? "Cancel all uploads"
                    : "Close upload queue")
            .attr(
                "title",
                cancelMode
                    ? "Cancel all uploads"
                    : "Close upload queue");

        ui.uploadQuitIcon
            .removeClass("fa-ban fa-xmark")
            .addClass(
                cancelMode
                    ? "fa-ban"
                    : "fa-xmark");

        ui.uploadQuitLabel.text(
            cancelMode
                ? "Cancel all uploads"
                : "Close upload queue");

        ui.uploadMinimizeBtn.prop(
            "disabled",
            total === 0);
    }

    function updateUi_UploadQueue_Visibility() {

        const show = state.upload.showQueue/*  && state.upload.list.length > 0 */;

        const shownClasses =
            "pointer-events-auto translate-y-0 scale-100 opacity-100";

        const hiddenClasses =
            "pointer-events-none translate-y-4 scale-[0.98] opacity-0";

        ui.uploadQueue
            .toggleClass(shownClasses, show)
            .toggleClass(hiddenClasses, !show)
            .attr(
                "aria-hidden",
                String(!show));
    }

    function updateUi_UploadQueue_ItemSection() {

        const minimized = state.upload.queueMinimized;

        const expandedClasses =
            "max-h-[32rem] opacity-100";

        const minimizedClasses =
            "pointer-events-none max-h-0 opacity-0";

        ui.uploadItemSection
            .toggleClass(expandedClasses, !minimized)
            .toggleClass(minimizedClasses, minimized)
            .attr(
                "aria-hidden",
                String(minimized));

        ui.uploadMinimizeBtn
            .attr(
                "aria-expanded",
                String(!minimized))
            .attr(
                "aria-label",
                minimized
                    ? "Expand upload queue"
                    : "Minimize upload queue")
            .attr(
                "title",
                minimized
                    ? "Expand upload queue"
                    : "Minimize upload queue");

        ui.uploadMinimizeIcon.toggleClass(
            "rotate-180",
            minimized);
    }

    function updateUi_DocumentDrawer() {

        const show = !!state.selectedSubjectId;

        const showClass = "opacity-100 translate-y-0 scale-100 max-h-[2000px]";
        const hideClass = "opacity-0 -translate-y-4 scale-[0.985] pointer-events-none max-h-0";

        ui.documentDrawer
            .toggleClass(showClass, show)
            .toggleClass(hideClass, !show);
    }

    function updateUi_DocumentList() {

        ui.documentList.html(
            DocumentLibraryTemplates.renderDocumentList(
                state.documents,
                state.file.totalCount,
                state.selectedSubject?.isChief)
        );
    }

    function updateUi_DocumentList_Loading() {

        ui.documentList.html(
            DocumentLibraryTemplates.renderDocumentListLoading(
                state.file.pageSize)
        );
    }

    function updateUi_DocumentList_LoadFailed() {

        ui.documentList.html(
            DocumentLibraryTemplates.renderDocumentListLoadFailed()
        );
    }

    function updateUi_Pagination() {

        ui.pagination.html(
            DocumentLibraryTemplates.renderPagination(
                state.file.pageIndex,
                state.file.totalPages)
        );
    }

    function updateUi_ShowToast(
        eventName,
        { title = null } = {}) {

        const ToastColors = {

            success: {
                borderClass: "border-emerald-600/50",
                iconColorClass: "text-emerald-400",
                titleColorClass: "text-emerald-300",
            },

            info: {
                borderClass: "border-sky-600/50",
                iconColorClass: "text-sky-400",
                titleColorClass: "text-sky-300",
            },

            warning: {
                borderClass: "border-amber-600/50",
                iconColorClass: "text-amber-400",
                titleColorClass: "text-amber-300",
            },

            error: {
                borderClass: "border-red-600/50",
                iconColorClass: "text-red-400",
                titleColorClass: "text-red-300",
            },
        };

        const Toasts = {

            [LoadEvent.SubjectsSucceed]:
            {
                color: "success",
                icon: "fa-folder-open",
                title: "Subjects loaded",
            },

            [LoadEvent.SubjectsFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "Failed to load subjects",
            },

            [LoadEvent.ChaptersSucceed]:
            {
                color: "success",
                icon: "fa-book-open",
                title: "Chapters loaded",
            },

            [LoadEvent.ChaptersFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "Failed to load chapters",
            },

            [LoadEvent.SubjectDetailsSucceed]:
            {
                color: "success",
                icon: "fa-book",
                title: "Subject details loaded",
            },

            [LoadEvent.SubjectDetailsFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "Failed to load subject",
            },

            [LoadEvent.ChapterDetailsSucceed]:
            {
                color: "success",
                icon: "fa-book-bookmark",
                title: "Chapter details loaded",
            },

            [LoadEvent.ChapterDetailsFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "Failed to load chapter",
            },

            [LoadEvent.DocumentsSucceed]:
            {
                color: "success",
                icon: "fa-file-lines",
                title: "Documents loaded",
            },

            [LoadEvent.DocumentsFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "Failed to load documents",
            },

            [OperationEvent.UploadSucceed]:
            {
                color: "success",
                icon: "fa-cloud-arrow-up",
                title: "File uploaded",
            },

            [OperationEvent.UploadFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "File upload failed",
            },

            [OperationEvent.DeleteSucceed]:
            {
                color: "success",
                icon: "fa-trash-can",
                title: "Document deleted",
            },

            [OperationEvent.DeleteFail]:
            {
                color: "error",
                icon: "fa-triangle-exclamation",
                title: "Document deletion failed",
            },
        };

        const baseConfig = Toasts[eventName];

        if (!baseConfig)
            return;

        const config = {
            ...baseConfig,
            title: title ?? baseConfig.title,
        };

        const MAX_TOASTS = 4;
        const LIFE_MS = 2500;
        const HIDE_MS = 300;

        const exitingClass = "toast-exiting";
        let liveChildren;

        while ((liveChildren = ui.toastContainer.children().not("." + exitingClass))
            .length >= MAX_TOASTS) {

            const oldest = liveChildren.first();

            oldest
                .removeClass("translate-y-0 opacity-100")
                .addClass("-translate-y-4 opacity-0")
                .addClass(exitingClass);

            setTimeout(() => oldest.remove(), HIDE_MS);
        }

        const colors = ToastColors[config.color];

        const toast = $(
            DocumentLibraryTemplates.renderToast({

                id: `toast-${crypto.randomUUID()}`,

                iconClass: config.icon,
                title: config.title,

                borderClass: colors.borderClass,
                iconColorClass: colors.iconColorClass,
                titleColorClass: colors.titleColorClass,
            })
        );

        ui.toastContainer.append(toast);

        setTimeout(
            () => {
                toast
                    .removeClass("translate-y-5 opacity-0")
                    .addClass("translate-y-0 opacity-100");
            }, 1000 / 60);

        setTimeout(
            () => {
                toast
                    .removeClass("translate-y-0 opacity-100")
                    .addClass("-translate-y-4 opacity-0")
                    .addClass(exitingClass);

                setTimeout(() => toast.remove(), HIDE_MS);
            }, LIFE_MS);
    }

    /* =========================================================
       LOADING
       ========================================================= */

    function raiseLoadEvent(raiseEvent, eventName, payload) {

        if (raiseEvent) {
            $(document).trigger(eventName, payload);
        }
    }

    function isCurrentLoad(resourceName, conTkn) {

        return core.concurrencyToken[resourceName] === conTkn;
    }

    async function loadSubjects(raiseEvent = true) {

        const conTkn = core.concurrencyToken.subjects = crypto.randomUUID();

        try {
            raiseLoadEvent(raiseEvent, LoadEvent.SubjectsStart);

            const subjectSummaryDtos = await $.ajax({

                url: "/documents/library"
                    + "?handler=Subjects",

                method: "GET",

                headers: {
                    CallerConnectionId: core.callerConnectionId,
                },
            });

            if (!isCurrentLoad("subjects", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.SubjectsAbort, { keepState: true, silent: true });
                return;
            }

            mutateState_Subjects(subjectSummaryDtos.map(normalizeSubject));

            raiseLoadEvent(raiseEvent, LoadEvent.SubjectsSucceed);
        }
        catch (err) {

            console.error("Error loading subjects:", err);

            if (isCurrentLoad("subjects", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.SubjectsFail);
            }
        }
    }

    async function loadSubjectDetails(raiseEvent = true) {

        const subjectId = state.selectedSubjectId;

        if (!subjectId) {
            mutateState_SelectedSubject(null);
            return;
        }

        const conTkn = core.concurrencyToken.subjectDetails = crypto.randomUUID();

        try {
            raiseLoadEvent(raiseEvent, LoadEvent.SubjectDetailsStart);

            const subjectDetailsDto = await $.ajax({

                url: "/documents/library"
                    + "?handler=Subject"
                    + `&id=${subjectId}`,

                method: "GET",

                headers: {
                    CallerConnectionId: core.callerConnectionId,
                },
            });

            if (!isCurrentLoad("subjectDetails", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.SubjectDetailsAbort, { keepState: true, silent: true });
                return;
            }

            mutateState_SelectedSubject(normalizeSubject(subjectDetailsDto));

            raiseLoadEvent(raiseEvent, LoadEvent.SubjectDetailsSucceed);
        }
        catch (err) {

            console.error("Error loading subject details:", err);

            if (isCurrentLoad("subjectDetails", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.SubjectDetailsFail);
            }
        }
    }

    async function loadChapters(raiseEvent = true) {

        const subjectId = state.selectedSubjectId;

        if (!subjectId) {

            const chapters = [];
            chapters.forSubjectId = null;

            mutateState_Chapters(chapters);
            return;
        }

        const conTkn = core.concurrencyToken.chapters = crypto.randomUUID();

        try {
            raiseLoadEvent(raiseEvent, LoadEvent.ChaptersStart);

            const chapterSummaryDtos = await $.ajax({

                url: "/documents/library"
                    + "?handler=Chapters"
                    + `&subjectId=${subjectId}`,

                method: "GET",

                headers: {
                    CallerConnectionId: core.callerConnectionId,
                },
            });

            if (!isCurrentLoad("chapters", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.ChaptersAbort, { keepState: true, silent: true });
                return;
            }

            const chapters = chapterSummaryDtos.map(normalizeChapter);
            chapters.forSubjectId = subjectId;

            mutateState_Chapters(chapters);

            raiseLoadEvent(raiseEvent, LoadEvent.ChaptersSucceed);
        }
        catch (err) {

            console.error("Error loading chapters:", err);

            if (isCurrentLoad("chapters", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.ChaptersFail);
            }
        }
    }

    async function loadChapterDetails(raiseEvent = true) {

        const chapterId = state.selectedChapterId;

        if (!chapterId) {

            mutateState_SelectedChapter(null);
            return;
        }

        const conTkn = core.concurrencyToken.chapterDetails = crypto.randomUUID();

        try {
            raiseLoadEvent(raiseEvent, LoadEvent.ChapterDetailsStart);

            const chapterDetailsDto = await $.ajax({

                url: "/documents/library"
                    + `?handler=Chapter&id=${chapterId}`,

                method: "GET",

                headers: {
                    CallerConnectionId: core.callerConnectionId,
                },
            });

            if (!isCurrentLoad("chapterDetails", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.ChapterDetailsAbort, { keepState: true, silent: true });
                return;
            }

            mutateState_SelectedChapter(normalizeChapter(chapterDetailsDto));

            raiseLoadEvent(raiseEvent, LoadEvent.ChapterDetailsSucceed);
        }
        catch (err) {

            console.error("Error loading chapter details:", err);

            if (isCurrentLoad("chapterDetails", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.ChapterDetailsFail);
            }
        }
    }

    async function loadDocuments(raiseEvent = true) {

        const subjectId = state.selectedSubjectId;

        if (!subjectId) {

            const documents = [];

            documents.forSubjectId = null;
            documents.forChapterId = null;
            documents.forSearch = "";

            mutateState_Documents(documents);
            return;
        }

        if (state.file.pageSize <= 0) {
            mutateState_File_PageSize(DEFAULT_PAGE_SIZE, false);
        }

        if (state.file.pageIndex <= 0) {
            mutateState_File_PageIndex(1, false);
        }

        const chapterId = state.selectedChapterId;
        const search = state.file.search;
        const pageSize = state.file.pageSize;
        const pageIndex = state.file.pageIndex;

        const conTkn = core.concurrencyToken.documents = crypto.randomUUID();

        try {
            const qs = new URLSearchParams();

            qs.set("subjectId", subjectId);
            qs.set("pageIndex", pageIndex);
            qs.set("pageSize", pageSize);

            if (chapterId) {
                qs.set("chapterId", chapterId);
            }

            if (search) {
                qs.set("search", search);
            }

            raiseLoadEvent(raiseEvent, LoadEvent.DocumentsStart);

            const pagedDocumentsResult =
                await $.ajax({

                    url: "/documents/library"
                        + `?handler=Documents&${qs}`,

                    method: "GET",

                    headers: {
                        CallerConnectionId: core.callerConnectionId,
                    },
                });

            if (!isCurrentLoad("documents", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.DocumentsAbort, { keepState: true, silent: true });
                return;
            }

            mutateState_File_Search(pagedDocumentsResult.search, false);
            mutateState_File_PageSize(pagedDocumentsResult.pageSize, false);
            mutateState_File_PageIndex(pagedDocumentsResult.pageIndex, false);
            mutateState_File_TotalCount(pagedDocumentsResult.totalCount, false);
            mutateState_File_TotalPages(pagedDocumentsResult.totalPages, false);

            const documents = pagedDocumentsResult.items.map(normalizeDocument);

            documents.forSubjectId = subjectId;
            documents.forChapterId = chapterId;
            documents.forSearch = pagedDocumentsResult.search ?? "";

            mutateState_Documents(documents);

            raiseLoadEvent(raiseEvent, LoadEvent.DocumentsSucceed);
        }
        catch (err) {

            console.error("Error loading documents:", err);

            if (isCurrentLoad("documents", conTkn)) {
                raiseLoadEvent(raiseEvent, LoadEvent.DocumentsFail);
            }
        }
    }

    /* =========================================================
       TYPE NORMALIZATION
       ========================================================= */

    function normalizeSubject(subject) {
        if (!subject) return null;
        subject.id += "";
        subject.chapters?.forEach(x => x.id += "");
        subject.documents?.forEach(x => x.id += "");
        return subject;
    }

    function normalizeChapter(chapter) {
        if (!chapter) return null;
        chapter.id += "";
        chapter.documents?.forEach(x => x.id += "");
        return chapter;
    }

    function normalizeDocument(document) {
        if (!document) return null;
        document.id += "";
        document.chapters?.forEach(x => x.id += "");
        return document;
    }

    /* ======================================================
       UPLOAD
       ====================================================== */

    function queueUploads(files) {

        if (!state.selectedSubjectId) {
            alert("Select a subject first.");
            return;
        }

        const validFiles = Array.from(files ?? []).filter(validateFile);
        const addedIds = mutateState_Upload_List_Enqueue(validFiles);
        if (!addedIds?.length) return;

        processUploadQueue();
    }

    function processUploadQueue() {

        if (state.upload.outstandingCount() === 0) {
            /*
             * The per-upload DTO reconciliation provides immediate feedback.
             * This final silent reload fetches canonical state 
             * and also discovers documents that survived 
             * a late client-side cancellation.
             */
            loadDocuments(false);
            return;
        }

        while (state.upload.inFlightCount() < MAX_CONCURRENT_UPLOADS) {
            const uploadItem = mutateState_Upload_List_Dequeue();
            if (!uploadItem) return;
            upload(uploadItem);
        }
    }

    function upload(uploadItem) {

        const form = new FormData();

        form.append("file", uploadItem.file);
        form.append("subjectId", uploadItem.subjectId);
        for (const chapterId of uploadItem.chapterIds)
            form.append("chapterIds", chapterId);

        const xhr = new XMLHttpRequest();

        xhr.open("POST", "/documents/library?handler=Upload");
        xhr.responseType = "json";
        xhr.setRequestHeader("RequestVerificationToken", getAntiForgery());
        xhr.setRequestHeader("CallerConnectionId", core.callerConnectionId);

        xhr.upload.addEventListener(
            "progress",
            e => {
                if (!e.lengthComputable) return;
                const percent = Math.round(e.loaded / e.total * 100);
                mutateState_Upload_List_Progress([{ id: uploadItem.id, percent }]);
            });

        xhr.addEventListener(
            "load",
            () => {
                if (xhr.status >= 200 && xhr.status < 300) {

                    const doc = normalizeDocument(xhr.response);

                    mutateState_Upload_List_Succeed([uploadItem.id]);

                    if (doc) {
                        reconcileUploadedDocument(doc, uploadItem);
                    }

                    $(document).trigger(OperationEvent.UploadSucceed, { fileName: uploadItem.file?.name });
                }
                else {
                    mutateState_Upload_List_Fail([uploadItem.id]);
                    $(document).trigger(OperationEvent.UploadFail, { fileName: uploadItem.file?.name });
                }

                processUploadQueue();
            });

        xhr.addEventListener(
            "error",
            () => {
                mutateState_Upload_List_Fail([uploadItem.id]);
                $(document).trigger(OperationEvent.UploadFail, { fileName: uploadItem.file?.name });

                processUploadQueue();
            });

        xhr.addEventListener(
            "abort",
            () => {
                mutateState_Upload_List_Abort([uploadItem.id]);
                processUploadQueue();
            });

        /*
         * Any asynchronous preparation inserted above this point must
         * recheck terminal state immediately before Start/send.
         */
        if (uploadItem.status === UploadStatus.Aborted) return;

        mutateState_Upload_List_Start([{ id: uploadItem.id, transfer: xhr }]);
        xhr.send(form);
    }

    function documentMatchesCurrentQuery(document, uploadItem) {

        if (!document || !uploadItem)
            return false;

        if (uploadItem.subjectId !== state.selectedSubjectId) {
            return false;
        }

        if (state.selectedChapterId
            && !document.chapters
                .some(chapter => chapter.id === state.selectedChapterId))
            return false;

        const search = state.file.search.trim().toLocaleLowerCase();

        if (search
            && !document.title.toLocaleLowerCase().includes(search)) {
            return false;
        }

        return true;
    }

    function reconcileUploadedDocument(document, uploadItem) {

        if (!documentMatchesCurrentQuery(document, uploadItem)) return;
        if (state.file.pageIndex !== 1) return;

        if (state.documents.forSubjectId !== state.selectedSubjectId) return;
        if (state.documents.forChapterId !== state.selectedChapterId) return;
        if ((state.documents.forSearch ?? "") !== (state.file.search ?? "")) return;

        const alreadyVisible = state.documents.some(item => item.id === document.id);
        if (alreadyVisible) return;

        const totalCount = state.file.totalCount + 1;
        const totalPages = Math.ceil(totalCount / state.file.pageSize);

        const documents = [document, ...state.documents]
            .sort((first, second) => {
                const firstDate = Date.parse(first.uploadedAt) || 0;
                const secondDate = Date.parse(second.uploadedAt) || 0;
                return secondDate - firstDate;
            })
            .slice(0, state.file.pageSize);

        documents.forSubjectId = state.selectedSubjectId;
        documents.forChapterId = state.selectedChapterId;
        documents.forSearch = state.file.search ?? "";

        mutateState_File_TotalCount(totalCount, false);
        mutateState_File_TotalPages(totalPages, false);
        mutateState_Documents(documents);
    }

    function retryUploadItem(uploadItem) {

        if (!uploadItem || !uploadItem.canRetry)
            return;

        const retriedItems = mutateState_Upload_List_Retry([uploadItem.id,]);
        if (!retriedItems?.length) return;

        processUploadQueue();
    }

    function cancelUploadItems(uploadItems) {

        const cancellableItems = Array.from(uploadItems ?? [])
            .filter(x => x?.canCancel);

        if (cancellableItems.length === 0)
            return;

        /*
         * Pending items have no XHR. Abort those in state first so
         * synchronous XHR abort callbacks cannot start them while
         * Cancel All is still iterating.
         */
        const directAbortIds = cancellableItems
            .filter(x => !x.transfer)
            .map(x => x.id);

        if (directAbortIds.length > 0)
            mutateState_Upload_List_Abort(directAbortIds);

        const transferItems = cancellableItems.filter(x => !!x.transfer);

        for (const uploadItem of transferItems)
            uploadItem.transfer.abort();

        /*
         * Transfer-backed items resume queue processing through their
         * XHR abort handlers. Direct-only cancellation needs to do it
         * here.
         */
        if (transferItems.length === 0
            && directAbortIds.length > 0) {
            processUploadQueue();
        }
    }

    function cancelUploadItem(uploadItem) {

        cancelUploadItems([uploadItem]);
    }

    function removeUploadItem(uploadItem) {

        if (!uploadItem || !uploadItem.canRemove)
            return;

        mutateState_Upload_List_Remove([uploadItem.id]);
    }

    /* ======================================================
       DELETE
       ====================================================== */

    async function deleteDocument() {

        const id = $(this).data("id");
        if (!id) return;

        const doc = state.documents.find(item => item.id === id);
        const documentTitle = doc?.title ?? "document";

        if (!confirm(`Delete "${documentTitle}"?`))
            return;

        try {
            await $.ajax(
                {
                    url: `/documents/library/${id}`,
                    method: "DELETE",
                    headers:
                    {
                        RequestVerificationToken: getAntiForgery(),
                        CallerConnectionId: core.callerConnectionId,
                    },
                });

            if (state.documents.length === 1
                && state.file.pageIndex > 1) {

                mutateState_File_PageIndex(state.file.pageIndex - 1, false);
            }

            await loadDocuments(false);

            $(document).trigger(OperationEvent.DeleteSucceed, { title: documentTitle });
        }
        catch (err) {
            console.error("Failed to delete document:", err);
            $(document).trigger(OperationEvent.DeleteFail, { title: documentTitle });
        }
    }

    /* ======================================================
       HELPERS
       ====================================================== */

    function validateFile(file) {

        const ext = file.name.split(".").pop().toLowerCase();

        return [
            "pdf",
            "docx",
            "pptx",
            "txt",
            "html",
        ].includes(ext);
    }

})();
