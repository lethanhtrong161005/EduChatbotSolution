"use strict";

(() => {

    const resourceHub =
        new signalR.HubConnectionBuilder()
            .withUrl("/resource")
            .withAutomaticReconnect()
            .build();

    let started = false;

    async function start() {

        if (started)
            return;

        started = true;

        await resourceHub.start();

        window.ResourceConnectionId = resourceHub.connectionId;

        $(document).trigger("resourcehub:connected", {
            connectionId: resourceHub.connectionId
        });

        await joinInitialGroups();
    }

    async function joinInitialGroups() {

        const tasks = [];

        tasks.push(resourceHub.invoke(
            HubMethod.JoinResourceType,
            ResourceType.Subject));

        tasks.push(resourceHub.invoke(
            HubMethod.JoinResourceType,
            ResourceType.Chapter));

        tasks.push(resourceHub.invoke(
            HubMethod.JoinResourceType,
            ResourceType.Document));

        tasks.push(resourceHub.invoke(
            HubMethod.JoinResourceType,
            ResourceType.User));

        tasks.push(resourceHub.invoke(
            HubMethod.JoinResourceCollection,
            ResourceType.User,
            Razor.userId,
            ResourceType.Membership));

        await Promise.all(tasks);
    }

    resourceHub.on(
        "ResourceChanged",
        resourceUpdate => {

            $(document).trigger(
                "resource:changed",
                resourceUpdate);
        });

    resourceHub.onreconnected(async connectionId => {

        window.ResourceConnectionId = connectionId;

        await joinInitialGroups();

        $(document).trigger(
            "resourcehub:reconnected",
            {
                connectionId
            });
    });

    resourceHub.onclose(() => {

        $(document).trigger("resourcehub:disconnected");
    });

    //
    // ------------------------------------------------------------
    // Document indexing status hub
    // ------------------------------------------------------------
    //

    const statusHub =
        new signalR.HubConnectionBuilder()
            .withUrl("/documents/status?page=library")
            .withAutomaticReconnect()
            .build();

    statusHub.on(
        "UpdateStatus",
        update => {

            $(document).trigger(
                "document:statusChanged",
                update);
        });

    statusHub.start()
        .catch(console.error);

    //
    // ------------------------------------------------------------
    // Convenience wrappers used by document-library.js
    // ------------------------------------------------------------
    //

    window.LibraryRealtime = {

        async joinType(type) {

            await resourceHub.invoke(
                HubMethod.JoinResourceType,
                type);
        },

        async leaveType(type) {

            await resourceHub.invoke(
                HubMethod.JoinResourceType,
                type);
        },

        async joinResource(type, id) {

            if (!id)
                return;

            await resourceHub.invoke(
                HubMethod.JoinResource,
                type,
                id);
        },

        async leaveResource(type, id) {

            if (!id)
                return;

            await resourceHub.invoke(
                HubMethod.LeaveResource,
                type,
                id);
        },

        async joinCollection(primaryType, primaryId, foreignType) {

            if (!primaryId)
                return;

            await resourceHub.invoke(
                HubMethod.JoinResourceCollection,
                primaryType,
                primaryId,
                foreignType);
        },

        async leaveCollection(primaryType, primaryId, foreignType) {

            if (!primaryId)
                return;

            await resourceHub.invoke(
                HubMethod.LeaveResourceCollection,
                primaryType,
                primaryId,
                foreignType);
        }
    };

    start()
        .catch(console.error);

})();
