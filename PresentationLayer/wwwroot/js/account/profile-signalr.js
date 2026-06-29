"use strict";

// ── State ────────────────────────────────────────────────────

window.Page = {

    userId: Razor.userId,
    subjectIds: Razor.subjectIds,
    documentIds: Razor.documentIds,

    user: null,
    memberships: [],
    documents: [],
};

// ── Loading ────────────────────────────────────────────────────

let _concurrencyToken = null;

async function loadAll() {
    await Promise.all([
        loadProfile(_concurrencyToken = crypto.randomUUID()),
        loadMemberships(_concurrencyToken),
        loadDocuments(_concurrencyToken),
    ]);
}

async function loadProfile(conTkn) {

    const dto = await $.getJSON("/account/profile?handler=GetProfile");

    if (_concurrencyToken !== conTkn) return;

    Page.user = dto;

    renderProfile();
}

async function loadMemberships(conTkn) {

    const oldSubjectIds = [...Page.subjectIds];

    const memberships = await $.getJSON("/account/profile?handler=GetMemberships");

    if (_concurrencyToken !== conTkn) return;

    memberships.forEach(x => x.subjectId);

    Page.memberships = memberships;
    Page.subjectIds = memberships.map(x => x.subjectId);

    // Adjust subscriptions

    const newSubjectIds = [...Page.subjectIds];

    const { added, removed } = diff(oldSubjectIds, newSubjectIds);

    for (const id of added)
        await resConn.invoke(HubMethod.JoinResource, ResourceType.Subject, id);

    for (const id of removed)
        await resConn.invoke(HubMethod.LeaveResource, ResourceType.Subject, id);

    renderMemberships();
}

async function loadDocuments(conTkn) {

    const documents = await $.getJSON("/account/profile?handler=GetDocuments");

    if (_concurrencyToken !== conTkn) return;

    Page.documents = documents;
    Page.documentIds = Page.documents.map(x => x.id);

    renderDocuments();
}

// ── Rendering ────────────────────────────────────────────────────

function renderProfile() {

    const u = Page.user;

    document.getElementById("profileTitle").textContent =
        u.fullName;

    document.querySelector(".profile-hero-avatar").textContent =
        u.fullName.charAt(0).toUpperCase();

    document.querySelector(".info-grid").innerHTML = `
        <div class="info-item">
            <div class="info-icon">
                <i class="fas fa-user"></i>
            </div>
            <div>
                <div class="info-label">Full Name</div>
                <div class="info-value">${u.fullName}</div>
            </div>
        </div>

        <div class="info-item">
            <div class="info-icon">
                <i class="fas fa-envelope"></i>
            </div>
            <div>
                <div class="info-label">Email Address</div>
                <div class="info-value">${u.email}</div>
            </div>
        </div>

        <div class="info-item">
            <div class="info-icon">
                <i class="fas fa-user-tag"></i>
            </div>
            <div>
                <div class="info-label">Username</div>
                <div class="info-value">${u.userName}</div>
            </div>
        </div>

        <div class="info-item">
            <div class="info-icon">
                <i class="fas fa-phone"></i>
            </div>
            <div>
                <div class="info-label">Phone Number</div>
                <div class="info-value">
                    ${u.phoneNumber || "Not Provided"}
                </div>
            </div>
        </div>
    `;
}

function renderMemberships() {

    const tab =
        document.querySelector("#membership-tab");

    tab.innerHTML =
        `<i class="fas fa-graduation-cap me-2"></i>
         Memberships (${Page.memberships.length})`;

    const panel =
        document.querySelector("#membership-panel .profile-card");

    if (!Page.memberships.length) {
        panel.innerHTML = `
            <h2 class="card-title-custom">
                <i class="fas fa-university text-primary"></i>
                Assigned Course Memberships
            </h2>
            <div class="empty-state">
                <i class="fas fa-book-reader"></i>
                <p>You are not currently enrolled or assigned to any subjects.</p>
            </div>
        `;
        return;
    }

    panel.innerHTML = `
        <h2 class="card-title-custom">
            <i class="fas fa-university text-primary"></i>
            Assigned Course Memberships
        </h2>
        <div class="table-container">
        <table class="profile-table">
        <thead>
        <tr>
            <th>Subject Code</th>
            <th>Subject Name</th>
            <th>Assigned Role</th>
            <th>Date Joined</th>
        </tr>
        </thead>
        <tbody>
        ${Page.memberships.map(m => `
            <tr>
                <td><strong>${m.subjectCode}</strong></td>
                <td>${m.subjectName}</td>
                <td>
                    <span class="role-badge-custom ${m.role === 2 ? "role-chief" : m.role === 1 ? "role-lecturer" : "role-student"}">
                        <i class="fas ${m.role === 0 ? "fa-user-graduate" : "fa-user-tie"} me-1"></i>
                        ${m.role === 2 ? "Chief" : m.role === 1 ? "Lecturer" : "Student"}
                    </span>
                </td>
                <td>${new Date(m.assignedAt).toLocaleDateString()}</td>
            </tr>
        `).join("")}
        </tbody>
        </table>
        </div>
    `;
}

function renderDocuments() {

    const tab = document.querySelector("#docs-tab");

    tab.innerHTML =
        `<i class="fas fa-folder-open me-2"></i>
         My Documents (${Page.documents.length})`;

    const panel = document.querySelector("#docs-panel .profile-card");

    if (!Page.documents.length) {
        panel.innerHTML = `
            <h2 class="card-title-custom">
                <i class="fas fa-copy text-primary"></i>
                Uploaded Course Documents
            </h2>

            <div class="empty-state">
                <i class="fas fa-folder-open"></i>
                <p>You have not uploaded any documents yet.</p>
            </div>
        `;
        return;
    }

    panel.innerHTML = `
        <h2 class="card-title-custom">
            <i class="fas fa-copy text-primary"></i>
            Uploaded Course Documents
        </h2>
        <div class="table-container">
        <table class="profile-table">
        <thead>
        <tr>
            <th>Document Title</th>
            <th>Subject & Chapter</th>
            <th>File Details</th>
            <th>Uploaded At</th>
            <th>Status</th>
            <th>Assignment Status</th>
        </tr>
        </thead>
        <tbody>
        ${Page.documents.map(d => `
            <tr>
                <td>
                    <a href="/Documents/Details?id=${d.id}"
                       class="text-primary font-weight-bold text-decoration-none">
                        ${d.title}
                    </a>
                </td>
                <td>
                    <span class="badge bg-secondary me-1">${d.subjectCode}</span>
                    <span class="text-muted small">${d.chapterName}</span>
                </td>
                 <td>
                    <span class="badge bg-dark">${d.fileType}</span>
                    <span class="text-muted small ms-1">
                        ${d.fileSize ? `${Math.round(10 * d.fileSize / 1024) / 10} KB` : "N/A"}
                    </span>
                </td>
                <td>${new Date(d.uploadedAt).toLocaleString()}</td>
                <td>
                    <span class="status-badge ${d.status === 6 ? "status-badge-indexed" : d.status === -1 ? "status-badge-failed" : d.status === 0 ? "status-badge-uploaded" : "status-badge-indexing"}">
                        ${d.status}
                     </span>
                </td>
                <td>
                    ${d.isNoLongerAssigned
            ? `
                        <span class="status-badge-custom status-warning" title="Warning: You are no longer assigned to this subject.">
                            <i class="fas fa-exclamation-triangle"></i> Not Assigned
                        </span>
                    `
            : `
                        <span class="status-badge-custom status-active">
                            <i class="fas fa-check-circle"></i> Assigned
                        </span>
                    `}
                </td>
            </tr>
        `).join("")}
        </tbody>
        </table>
        </div>
    `;
}

// ── Helper ────────────────────────────────────────────────────

function diff(oldIds, newIds) {

    const oldSet = new Set([...oldIds]);
    const newSet = new Set([...newIds]);

    return {
        added: newIds.filter(x => !oldSet.has(x)),
        removed: oldIds.filter(x => !newSet.has(x)),
    };
}

// ── SignalR ────────────────────────────────────────────────────

const resConn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/resource`)
        .withAutomaticReconnect()
        .build();

resConn.on(
    "ResourceChanged",
    async function (resUpd) {
        switch (resUpd.resourceType) {
            case ResourceType.User:
                if (resUpd.resourceId === Page.userId)
                    await loadProfile(_concurrencyToken = crypto.randomUUID());
                break;
            case ResourceType.Subject:
                if (Page.subjectIds.includes(resUpd.resourceId)) {
                    await Promise.all([loadMemberships(_concurrencyToken = crypto.randomUUID()), loadDocuments(_concurrencyToken)]);
                }
                break;
            case ResourceType.Membership:
                if (resUpd.properties["userId"] === Page.userId)
                    await Promise.all([loadMemberships(_concurrencyToken = crypto.randomUUID()), loadDocuments(_concurrencyToken)]);
                break;
            case ResourceType.Document:
                if (resUpd.properties["uploaderId"] === Page.userId)
                    await loadDocuments(_concurrencyToken = crypto.randomUUID());
                break;
        }
    }
);

resConn
    .start()
    .then(async () => {
        const promises = [];

        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.User, Page.userId));
        promises.push(resConn.invoke(HubMethod.JoinResourceCollection, ResourceType.User, Page.userId, ResourceType.Membership));
        promises.push(resConn.invoke(HubMethod.JoinResourceCollection, ResourceType.User, Page.userId, ResourceType.Document));

        for (const subjectId of Page.subjectIds) {
            promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Subject, subjectId));
        }

        await Promise.all(promises);
    })
    .then(() => window.connId = resConn.connectionId)
    .then(() =>
        $("<input>")
            .attr("type", "hidden")
            .attr("name", "CallerConnectionId")
            .val(connId)
            .appendTo($("form")))
    .catch(console.error);
