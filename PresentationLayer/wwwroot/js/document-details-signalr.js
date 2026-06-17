"use strict";

const conn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/documents/status?page=details&document-id=${razor_docId}`)
        .withAutomaticReconnect()
        .build();

conn.start()
    .catch(console.error);

conn.on(
    "UpdateStatus",
    function (update) {

        if (update.id !== razor_docId)
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
}
