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
            case "user":
                if (resUpd.resourceId === HomePage.userId)
                    promptReload();
                break;
        }
    }
);

function promptReload() {

    if (confirm("Your info has been modified. Refresh?"))
        window.location.reload();
}

resConn
    .start()
    .then(() => resConn.invoke("JoinGroup", "home", HomePage.userId))
    .then(() => resConn.invoke("JoinGroup", "home", null))
    .then(() => window.connId = resConn.connectionId)
    .catch(console.error);
