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
            case "user":
                if (resUpd.resourceId === ProfilePage.userId)
                    promptReload();
                break;
            case "subject_membership":
                if (resUpd.alternateResourceId && resUpd.alternateResourceId.length == 2 && resUpd.alternateResourceId[1] === ProfilePage.userId)
                    promptReload();
                break;
            case "document":
                if (resUpd.alternateResourceId && resUpd.alternateResourceId.length == 2 && resUpd.alternateResourceId[1] === ProfilePage.userId)
                    promptReload();
                break;
        }
    }
);

function promptReload() {

    if (confirm("This document has been modified. Refresh?"))
        window.location.reload();
}

resConn
    .start()
    .then(() => resConn.invoke("JoinPage", "profile", null))
    .then(() => window.connId = resConn.connectionId)
    .catch(console.error);