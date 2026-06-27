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
                if (resUpd.resourceId === DocumentEditPage.documentId)
                    promptReload();
                break;
            case ResourceType.Chapter:
                if (resUpd.resourceId === DocumentEditPage.chapterId)
                    promptReload();
                break;
            case ResourceType.Subject:
                if (resUpd.resourceId === DocumentEditPage.subjectId)
                    promptReload();
                break;
            case ResourceType.User:
                if (resUpd.resourceId === DocumentEditPage.uploaderId)
                    promptReload();
                break;
            case ResourceType.Membership:
                if (resUpd.action === "deleted"
                    && resUpd.properties["subjectId"] === DocumentEditPage.subjectId
                    && resUpd.properties["userId"] === DocumentEditPage.userId) {
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

        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Document, Page.documentId));
        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Chapter, Page.chapterId));
        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Subject, Page.subjectId));
        promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.User, Page.uploaderId));

        if (Page.viewerMembershipId)
            promises.push(resConn.invoke(HubMethod.JoinResource, ResourceType.Membership, Page.uploaderId));

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
