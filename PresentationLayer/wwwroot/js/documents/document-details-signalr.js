"use strict";

/* =============== STATUS BADGE =============== */

const conn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/documents/status?page=details&document-id=${Razor.documentId}`)
        .withAutomaticReconnect()
        .build();

conn.start()
    .catch(console.error);

conn.on(
    "UpdateStatus",
    function (update) {

        if (update.id !== Razor.documentId)
            return;

        updateBadge(update);
    });

function updateBadge(update) {

    const badge = document.getElementById("documentStatusBadge");
    const parseDiv = document.getElementById("docParserUsed");
    const chunkDiv = document.getElementById("docChunkCount");
    const embedDiv = document.getElementById("docEmbeddingModel");

    if (badge) {

        const settings =
            StatusSettings[StatusNames[update.status]];

        if (!settings)
            return;

        const text =
            update.progress
                ? settings.text.replace("{{PROGRESS}}", update.progress.toFixed(2))
                : settings.text.replace("({{PROGRESS}}%)", "").trim();

        badge.className =
            `document-status-badge ${settings.className}`;

        badge.innerHTML =
            `
        <i class="fas ${settings.iconClass}"></i>
        ${text}
        `;
    }

    if (parseDiv && update.parserUsed) {
        parseDiv.textContent = update.parserUsed;
    }
    if (chunkDiv && update.chunkCount) {
        chunkDiv.textContent = update.chunkCount;
    }
    if (embedDiv && update.embeddingModel) {
        embedDiv.textContent = update.embeddingModel;
    }

    // Automatically reload page to display extracted content when indexing is complete
    if (StatusNames[update.status] === "Indexed") {
        setTimeout(() => {
            window.location.reload();
        }, 1500);
    }
}

/* =============== RESOURCES =============== */

const resConn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/resource`)
        .withAutomaticReconnect()
        .build();

resConn.on(
    "ResourceChanged",
    function (resUpd) {
        switch (resUpd.resourceType) {
            case ResourceType.Document:
            case ResourceType.Chapter:
            case ResourceType.Subject:
            case ResourceType.User:
            case ResourceType.Membership:
                $(document).trigger("resource:changed", resUpd);
                break;
        }
    }
);

resConn
    .start()
    .then(async () => {

        const promises = [];

        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Document, Razor.documentId));
        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Subject, Razor.subjectId));
        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.User, Razor.uploaderId));

        promises.push(resConn.invoke(HubMethod.JoinResourceCollection, ResourceType.Document, Razor.documentId, ResourceType.DocumentChapter));

        for (const chapterId of Razor.chapterIds) {
            promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Chapter, chapterId));
        }

        if (Razor.viewerMembershipId) // Admins have no membership
            promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Membership, Razor.uploaderId));

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

/* =============== COMMENTS =============== */

const commentConn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/documents/comments`)
        .withAutomaticReconnect()
        .build();

commentConn.on(
    "TODO",
    () => { });

commentConn
    .start()
    .then(() => { })
    .catch(console.error);