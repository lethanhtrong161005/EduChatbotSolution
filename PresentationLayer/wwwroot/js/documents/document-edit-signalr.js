"use strict";

const resConn =
    new signalR.HubConnectionBuilder()
        .withUrl(`/realtime`)
        .withAutomaticReconnect()
        .build();

resConn.on(
    "ResourceChanged",
    function (resUpd) {
        switch (resUpd.resourceType) {
            case "document":
                if (resUpd.resourceId === DocumentEditPage.documentId) {

                    if (confirm("This document has been modified. Refresh?"))
                        window.location.reload();
                }
                break;
            case "subject_membership":
                if (resUpd.action === "deleted" && resUpd.alternateResourceId && resUpd.alternateResourceId.length == 2
                    && resUpd.alternateResourceId[0] === DocumentEditPage.subjectId && resUpd.alternateResourceId[1] === DocumentEditPage.userId) {

                    alert("Sorry for the inconvenience. You no longer have access to this document.");
                    window.location.href = "/documents/library";
                }
        }
    }
);

resConn
    .start()
    .then(() => resConn.invoke("JoinPage", "document-edit", null))
    .then(() => window.connId = resConn.connectionId)
    .then(() =>
        $("<input>")
            .attr("type", "hidden")
            .attr("name", "CallerSignalRConnectionId")
            .val(connId)
            .appendTo($("form")))
    .catch(console.error);
