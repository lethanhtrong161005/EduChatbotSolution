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
            case ResourceType.User:
                if (resUpd.resourceId === Razor.userId)
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
    .then(() => resConn.invoke(HubMethod.JoinResource, ResourceType.User, Razor.userId))
    .then(() => window.connId = resConn.connectionId)
    .then(() =>
        $("<input>")
            .attr("type", "hidden")
            .attr("name", "CallerConnectionId")
            .val(connId)
            .appendTo($("form")))
    .catch(console.error);
