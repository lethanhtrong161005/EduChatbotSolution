"use strict";

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
                if (resUpd.resourceId === Razor.documentId)
                    promptReload();
                break;
            case ResourceType.Subject:
                if (resUpd.resourceId === Razor.subjectId)
                    promptReload();
                break;
            case ResourceType.User:
                if (resUpd.resourceId === Razor.uploaderId)
                    promptReload();
                break;
            case ResourceType.Chapter:
                if (Razor.chapterIds.includes(resUpd.resourceId))
                    promptReload();
                break;
            case ResourceType.DocumentChapter:
                if (resUpd.properties["documentId"] === Razor.documentId)
                    promptReload();
                break;
            case ResourceType.Membership:
                if (resUpd.resourceId === Razor.viewerMembershipId
                    && resUpd.action === "deleted") {
                    denyAccess();
                }
                break;
        }
    }
);

function promptReload() {

    if (confirm("This document has been modified. Refresh?"))
        window.location.reload();
}

function denyAccess() {

    alert("Sorry for the inconvenience. You no longer have access to this document.");
    window.location.href = "/documents/library";
}

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

        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Membership, Razor.viewerMembershipId));

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
