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
        .withUrl(`/resource`)
        .withAutomaticReconnect()
        .build();

resConn.on(
    "ResourceChanged",
    function (resUpd) {
        switch (resUpd.resourceType) {
            case ResourceType.Subject:
            case ResourceType.Chapter:
            case ResourceType.Document:
            case ResourceType.User: // Uploader details
            case ResourceType.Membership: // of current user
                $(document).trigger("resource:changed", resUpd);
                break;
        }
    }
);

resConn
    .start()
    .then(async () => {

        const promises = [];

        promises.push(resConn.invoke(HubMethod.JoinResourceType, ResourceType.Subject));
        promises.push(resConn.invoke(HubMethod.JoinResourceType, ResourceType.Chapter));
        promises.push(resConn.invoke(HubMethod.JoinResourceType, ResourceType.Document));
        promises.push(resConn.invoke(HubMethod.JoinResourceType, ResourceType.User));
        promises.push(resConn.invoke(HubMethod.JoinResourceCollection, ResourceType.User, Razor.userId, ResourceType.Membership));

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
