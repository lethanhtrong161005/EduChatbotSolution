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
            case "document":
                if (resUpd.resourceId === DocumentEditPage.documentId)
                    promptReload();
                break;
            case "chapter":
                if (resUpd.resourceId === DocumentEditPage.chapterId)
                    promptReload();
                break;
            case "subject":
                if (resUpd.resourceId === DocumentEditPage.subjectId)
                    promptReload();
                break;
            case "user":
                if (resUpd.resourceId === DocumentEditPage.uploaderId)
                    promptReload();
                break;
            case "membership":
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
    .then(() => resConn.invoke("JoinGroup", "document-edit", DocumentEditPage.documentId))
    .then(() => resConn.invoke("JoinGroup", "document-edit", null))
    .then(() => window.connId = resConn.connectionId)
    .then(() =>
        $("<input>")
            .attr("type", "hidden")
            .attr("name", "CallerSignalRConnectionId")
            .val(connId)
            .appendTo($("form")))
    .catch(console.error);
