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
                if (resUpd.resourceId === ProfilePage.userId)
                    promptReload();
                break;
            case "subject":
                // TEST_ME
                if (ProfilePage.membershipIds.includes(resUpd.resourceId))
                    promptReload();
                break;
            case "membership":
                if (resUpd.properties["userId"] === ProfilePage.userId)
                    promptReload();
                break;
            case "document":
                if (resUpd.properties["uploaderId"] === ProfilePage.userId)
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
    .then(() => resConn.invoke("JoinGroup", "profile", ProfilePage.userId))
    .then(() => resConn.invoke("JoinGroup", "profile", null))
    .then(() => window.connId = resConn.connectionId)
    .catch(console.error);
