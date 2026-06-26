document.addEventListener("DOMContentLoaded", () => {

    /* BROWSE DOCUMENT LIBRARY */

    const fileTableBody = document.getElementById("fileTableBody");
    const subjectSelect = document.getElementById("subjectSelect");
    const chapterSelect = document.getElementById("chapterSelect");
    const searchInput = document.getElementById("searchInput");

    const initRow =
        `
        <tr>
            <td colspan="7">
                Select a subject.
            </td>
        </tr>
        `;
    const loadingRow =
        `
        <tr>
            <td colspan="7">
                Loading...
            </td>
        </tr>
        `;
    const emptyRow =
        `
        <tr>
            <td colspan="7">
                No documents found.
            </td>
        </tr>
        `;

    let currentFiles = [];
    let concurrencyToken = 0;

    subjectSelect.addEventListener(
        "input",
        loadLibrary);

    chapterSelect.addEventListener(
        "input",
        refreshTable);

    searchInput.addEventListener(
        "input",
        refreshTable);

    init();

    async function init() {

        subjectSelect.disabled = true;

        if (!window.connId) {
            setTimeout(init, 100);
            return;
        }

        await loadSubjects(++concurrencyToken);

        subjectSelect.disabled = false;
    }

    async function loadSubjects(conTkn) {

        const cachedId = subjectSelect.value;

        subjectSelect.disabled = true;

        subjectSelect.innerHTML =
            "<option value=''>Loading...</option>";

        const response = await fetch(`/documents/library?handler=GetSubjects`, {
            method: "GET",
            headers: {
                "CallerSignalRConnectionId": connId,
            }
        });

        const subjects = await response.json();

        if (conTkn != concurrencyToken)
            return;

        subjectSelect.innerHTML =
            "<option value=''>Select Subject</option>";

        subjects.forEach(subject => {

            subjectSelect.insertAdjacentHTML(
                "beforeend",
                `
                <option value="${subject.id}"${subject.id === parseInt(cachedId) ? " selected" : ""}>
                    ${subject.code} :: ${subject.name}
                </option>
                `);
        });

        subjectSelect.disabled = false;
    }

    async function loadLibrary() {

        let subjectId = subjectSelect.value;

        if (!subjectId) {
            clearTable();
            return;
        };

        const conTkn = ++concurrencyToken;

        await getCanUpload(subjectId, conTkn);
        await loadChapters(subjectId, conTkn);
        await loadDocuments(subjectId, conTkn)

        refreshTable();
    }

    function refreshTable() {

        if (!subjectSelect.value) {
            clearTable();
            return;
        }

        const chapterId =
            parseInt(chapterSelect.value);

        const searchTerm =
            searchInput.value.toLowerCase();

        let files =
            [...currentFiles];

        if (chapterId) {
            files = files.filter(
                x => x.chapterId === chapterId);
        }

        if (searchTerm) {
            files = files.filter(
                x => x.title
                    .toLowerCase()
                    .includes(searchTerm));
        }

        displayFiles(files);
    }

    function clearTable() {

        currentFiles = [];

        fileTableBody.innerHTML = initRow;

        chapterSelect.innerHTML =
            "<option value=''>Select Chapter</option>";

        chapterSelect.disabled = true;

        showUploadBtn.classList.add("d-none");
        uploadPanel.classList.add("d-none");
    }

    async function getCanUpload(subjectId, conTkn) {

        uploadPanelOverlay.classList.remove("d-none");

        const response = await fetch(`/documents/library?handler=CanUpload&subjectId=${subjectId}`, {
            method: "GET",
            headers: {
                "CallerSignalRConnectionId": connId,
            }
        });

        const { canUpload } = await response.json();

        if (conTkn != concurrencyToken)
            return;

        if (canUpload) {
            showUploadBtn.classList.remove("d-none");
            uploadPanel.classList.remove("d-none");
        } else {
            showUploadBtn.classList.add("d-none");
            uploadPanel.classList.add("d-none");
        }

        uploadPanelOverlay.classList.add("d-none");
    }

    async function loadChapters(subjectId, conTkn) {

        const cachedId = chapterSelect.value;

        chapterSelect.disabled = true;

        chapterSelect.innerHTML =
            "<option value=''>Loading...</option>";

        const response = await fetch(`/documents/library?handler=GetChapters&subjectId=${subjectId}`, {
            method: "GET",
            headers: {
                "CallerSignalRConnectionId": connId,
            }
        });

        const chapters = await response.json();

        if (conTkn != concurrencyToken)
            return;

        chapters.sort((a, b) => a.chapterNumber - b.chapterNumber);

        chapterSelect.innerHTML =
            "<option value=''>Select Chapter</option>";

        chapters.forEach(chapter => {

            chapterSelect.insertAdjacentHTML(
                "beforeend",
                `
                <option value="${chapter.id}"${chapter.id === parseInt(cachedId) ? " selected" : ""}>
                    ${chapter.chapterNumber} • ${chapter.name}
                </option>
                `);
        });

        chapterSelect.disabled = false;
    }

    async function loadDocuments(subjectId, conTkn) {

        fileTableBody.innerHTML = loadingRow;

        const response = await fetch(`/documents/library?handler=GetFiles&subjectId=${subjectId}`, {
            method: "GET",
            headers: {
                "CallerSignalRConnectionId": connId,
            }
        });

        currentFiles = await response.json();

        if (conTkn != concurrencyToken)
            return;

        displayFiles(currentFiles);
    }

    function displayFiles(files) {

        fileTableBody.innerHTML = "";

        if (!files.length) {
            fileTableBody.innerHTML = emptyRow;
            return;
        }

        files.forEach(file => {

            fileTableBody.insertAdjacentHTML(
                "beforeend",
                `
<div class="document-card">

    <div class="document-main">

        <div class="document-icon">
            <i class="fas ${getFileIcon(file.extension)}"></i>
        </div>

        <div class="document-info">

         <div class="document-title-row">

                <a href="/documents/download/${file.id}"
                class="document-name">

                    ${file.title + file.extension}

                </a>

                <span
                    id="status-badge-${file.id}"
                    class="document-status-badge status-${file.status.toLowerCase()}">

                    <i class="fas ${StatusSettings[file.status].iconClass}"></i>

                    ${file.status}

                </span>

            </div>

            <div class="document-meta">

                <span>${file.uploadedBy}</span>

                <span>${formatDateTime(file.uploadedAt)}</span>

            </div>

        </div>

    </div>

    <div class="document-actions">

        <a href="/documents/download/${file.id}"
           class="icon-btn">

            <i class="fas fa-download"></i>

        </a>

        <a href="/documents/details/${file.id}"
           class="icon-btn">

            <i class="fas fa-eye"></i>

        </a>

        <button class="icon-btn danger"
            id="delete-file-${file.id}"
            data-file-id=${file.id}>

            <i class="fas fa-trash"></i>

        </button>

    </div>

</div>
    `);

            fileTableBody
                .querySelector(`#delete-file-${file.id}`)
                .addEventListener(
                    "click",
                    promptDeleteDocument);
        });
    }

    async function promptDeleteDocument() {

        fileId = deleteBtn.dataset.fileId;
        if (!fileId) return;

        const conf = confirm("Are you sure you wish to delete this file?");
        if (!conf) return;

        var response = await fetch(`/documents/library/${file.id}`, {
            method: "DELETE",
            headers: {
                "RequestVerificationToken": getAntiForgery(),
                "CallerSignalRConnectionId": connId,
            }
        });

        if (response.ok) {

            loadDocuments(subjectSelect.value, ++concurrencyToken)
                .then(refreshTable);
        }
    }

    function formatDateTime(dateString) {

        return new Intl.DateTimeFormat(
            "en-GB",
            {
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit"
            })
            .format(new Date(dateString));
    }

    /* UPLOAD DOCUMENT */

    const uploadPanel =
        document.getElementById("uploadPanel");

    const showUploadBtn =
        document.getElementById("showUploadBtn");

    const browseBtn =
        document.getElementById("browseBtn");

    const fileInput =
        document.getElementById("fileInput");

    const dropZone =
        document.getElementById("dropZone");

    const uploadQueue =
        document.getElementById("uploadQueue");

    showUploadBtn?.addEventListener(
        "click",
        () => {
            uploadPanel.classList.toggle("d-none");
        });

    browseBtn?.addEventListener(
        "click",
        () => fileInput.click());

    fileInput?.addEventListener(
        "change",
        e => {
            uploadFiles(e.target.files);
            e.target.value = "";
        });

    ["dragenter", "dragover"]
        .forEach(eventName =>
            dropZone?.addEventListener(
                eventName,
                e => {
                    e.preventDefault();
                    dropZone.classList.add("dragover");
                }));

    ["dragleave", "drop"]
        .forEach(eventName =>
            dropZone?.addEventListener(
                eventName,
                e => {
                    e.preventDefault();
                    dropZone.classList.remove("dragover");
                }));

    dropZone?.addEventListener(
        "drop",
        e => {
            uploadFiles(e.dataTransfer.files);
        });

    const MAX_CONCURRENT_UPLOADS = 3;

    let uploadQueueItems = [];
    let activeUploads = 0;
    let outstandingUploads = 0;

    function uploadFiles(files) {

        const chapterId = chapterSelect.value;
        if (!chapterId) {
            alert("You must first select a chapter!");
            return;
        }

        let dupes = [];
        let map = new Map();

        currentFiles.forEach(f => {
            map.set(f.title + f.extension, f);
        });

        [...files].forEach(f => {

            if (map.get(f.name)) {
                dupes.push(f.name);
            }

        });

        if (dupes.length > 0) {

            const dupeNames = dupes
                .map(fname => "    > " + fname)
                .reduce((fnamelist, fname) => fnamelist + '\n' + fname);

            if (!confirm(`
These files appear to already exist in the knowledge base.
${dupeNames}
Are you sure you wish to upload them?
            `))
                return;
        }

        uploadQueue.innerHTML = "";

        [...files]
            .filter(validateFile)
            .forEach((file, idx) => {

                uploadQueueItems.push(
                    {
                        chapterId,
                        file
                    });

                createUploadRow(file);

                outstandingUploads++;
            });

        processUploadQueue();
    }

    function createUploadRow(file) {

        const uploadId = crypto.randomUUID();

        file.uploadId = uploadId;

        const row = document.createElement("div");

        row.id = uploadId;
        row.className = "upload-row";
        row.innerHTML = `
                <div class="upload-file">

                    <div class="upload-file-header">

                        <span class="upload-file-name">
                            ${file.name}
                        </span>

                        <span class="upload-file-percent">
                            Queued
                        </span>

                    </div>

                    <div class="upload-progress">

                        <div class="upload-progress-bar"></div>

                    </div>

                </div>
            `;

        uploadQueue.appendChild(row);
    }

    function processUploadQueue() {
        while (uploadQueueItems.length > 0
            && activeUploads < MAX_CONCURRENT_UPLOADS) {

            activeUploads++;

            const { file, chapterId } = uploadQueueItems.shift();

            updateStatus(
                file.uploadId,
                "Starting...");

            const formData = new FormData();
            formData.append("chapterId", chapterId);
            formData.append("files", file);

            const xhr = new XMLHttpRequest();
            xhr.open(
                "POST",
                "/documents/library?handler=Upload");

            xhr.setRequestHeader("RequestVerificationToken", getAntiForgery());
            xhr.setRequestHeader("CallerSignalRConnectionId", connId);

            xhr.upload.addEventListener(
                "progress",
                e => {

                    if (!e.lengthComputable)
                        return;

                    const percent = Math.round(e.loaded / e.total * 100);

                    updateProgress(file.uploadId, percent);
                });

            xhr.addEventListener(
                "load",
                () => {
                    if (xhr.status >= 200 &&
                        xhr.status < 300) {

                        updateProgress(
                            file.uploadId,
                            100);

                        updateStatus(
                            file.uploadId,
                            "Upload Complete");
                    }
                    else {
                        updateStatus(
                            file.uploadId,
                            "Upload Failed",
                            "crimson");
                    }

                    activeUploads--;
                    outstandingUploads--;

                    processUploadQueue();
                });

            xhr.addEventListener(
                "error",
                () => {
                    updateStatus(
                        file.uploadId,
                        "Upload Failed");

                    activeUploads--;
                    outstandingUploads--;

                    processUploadQueue();
                });

            xhr.send(formData);
        }

        if (outstandingUploads === 0) {

            loadDocuments(subjectSelect.value, ++concurrencyToken)
                .then(refreshTable);
        }
    }

    const allowedExtensions =
        [
            "pdf",
            "docx",
            "pptx",
            "txt",
            "html"
        ];

    function validateFile(file) {

        const extension = file.name
            .split(".")
            .pop()
            .toLowerCase();

        if (!allowedExtensions.includes(extension)) {
            alert(`${file.name} is not supported.`);
            return false;
        }

        return true;
    }

    function updateProgress(uploadId, percent) {
        const fileRow = document.getElementById(uploadId);
        if (!fileRow) return;

        fileRow.querySelector(".upload-progress-bar")
            .style.width = `${percent}%`;

        fileRow.querySelector(".upload-file-percent")
            .textContent = `${percent}%`;
    }

    function updateStatus(uploadId, status, color = null) {
        const fileRow = document.getElementById(uploadId);
        if (!fileRow) return;

        fileRow.querySelector(".upload-file-percent")
            .textContent = status;

        if (color)
            fileRow.querySelector(".upload-progress-bar")
                .style.background = "crimson";
    }

    function getFileIcon(type) {

        switch (type?.toUpperCase()) {

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

    /* SIGNALR EVENTS */

    $(document).on(
        "resource:changed",
        async function (_, resUpd) {
            switch (resUpd.resourceType) {
                case "subject":
                    await loadSubjects(++concurrencyToken);
                    refreshTable();
                    break;
                case "chapter":
                    await loadChapters(subjectSelect.value, ++concurrencyToken);
                    refreshTable();
                    break;
                case "document":
                case "user": // Uploader details may have changed
                    await loadDocuments(subjectSelect.value, ++concurrencyToken);
                    refreshTable();
                    break;
                case "membership": // Current user may have gained/lost membership(s)
                    if (resUpd.properties["userId"] === DocumentLibraryPage.userId) {
                        await loadSubjects(++concurrencyToken);
                        refreshTable();
                    }
                    break;
            }
        }
    );

    function getAntiForgery() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    }
});
