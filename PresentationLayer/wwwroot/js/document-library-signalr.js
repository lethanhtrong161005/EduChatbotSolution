"use strict";

const conn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/documents/status?page=library`)
        .withAutomaticReconnect()
        .build();

conn.start()
    .catch(console.error);

conn.on(
    "UpdateStatus",
    function (docStatusUpd) {
        const $badge = $(`#status-badge-${docStatusUpd.id}`);
        if ($badge.length === 0) return;

        const settings = StatusSettings[StatusNames[docStatusUpd.status]];
        if (!settings) return;

        const text =
            docStatusUpd.progress
                ? settings.text.replace("{{PROGRESS}}", docStatusUpd.progress.toFixed(2))
                : settings.text.replace("({{PROGRESS}}%)", "").trim();


        $badge.html(`
            <i class="fas ${settings.iconClass}"></i>
            ${text}
        `);

        $badge.removeClass().addClass(`document-status-badge ${settings.className}`);
    }
);

const resConn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/realtime`)
        .withAutomaticReconnect()
        .build();

resConn.on(
    "ResourceChanged",
    function (resUpd) {
        switch (resUpd.resourceType) {
            case "subject":
            case "chapter":
            case "subject_membership":
            case "document":
                $(document).trigger("resource:changed", resUpd);
                break;
        }
    }
);

resConn
    .start()
    .then(() => resConn.invoke("JoinPage", "document-library", null))
    .then(() => window.connId = resConn.connectionId)
    .catch(console.error);
