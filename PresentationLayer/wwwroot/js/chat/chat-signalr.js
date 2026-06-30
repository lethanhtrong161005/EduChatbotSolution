window.ChatSignalR = (function () {

    let connection = null;

    async function start() {

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/chat/answer")
            .withAutomaticReconnect()
            .build();

        connection.on("ExchangeCreated",
            (userMessageId, userMessageClientId, assistantMessageId, assistantMessageClientId) => {

                $(document).trigger(
                    "chat:exchange",
                    [userMessageId, userMessageClientId, assistantMessageId, assistantMessageClientId]);
            });

        connection.on("GenerationStarted",
            (assistantMessageId, assistantMessageClientId) => {

                $(document).trigger(
                    "chat:started",
                    [assistantMessageId, assistantMessageClientId]);
            });

        connection.on("ReceiveToken",
            (assistantMessageId, assistantMessageClientId, token) => {

                $(document).trigger(
                    "chat:token",
                    [assistantMessageId, assistantMessageClientId, token]);
            });

        connection.on("GenerationCompleted",
            (assistantMessageId, assistantMessageClientId, chatMessageDto) => {

                $(document).trigger(
                    "chat:completed",
                    [assistantMessageId, assistantMessageClientId, chatMessageDto]);
            });

        connection.on("GenerationFailed",
            (assistantMessageId, assistantMessageClientId, error) => {

                $(document).trigger(
                    "chat:failed",
                    [assistantMessageId, assistantMessageClientId, error]);
            });

        await connection.start();

        window.chatConnectionId = connection.connectionId;
    }

    let activeSessionId = null;

    async function switchSession(sessionId) {

        if (activeSessionId)
            await connection.invoke(
                "SwitchSession",
                activeSessionId,
                activeSessionId = sessionId);
        else
            await connection.invoke(
                "JoinSession",
                activeSessionId = sessionId);
    }

    return {
        start,
        switchSession,
    };

})();

window.ResourceSignalR = (function () {

    let connection = null;

    let activeSessionId = null;

    async function start() {

        connection = new signalR.HubConnectionBuilder()
            .withUrl(`/resource`)
            .withAutomaticReconnect()
            .build();

        connection.on(
            "ResourceChanged",
            async function (resUpd) {
                switch (resUpd.resourceType) {
                    case ResourceType.Subject:
                    case ResourceType.Membership:
                    case ResourceType.ChatSession:
                        $(document).trigger("resource:changed", resUpd);
                        break;
                }
            }
        );

        await connection.start();

        window.callerConnectionId = connection.connectionId;
    }

    async function subscribeToResourceType(resourceType) {
        await connection.invoke(HubMethod.JoinResourceType, resourceType);
    }

    async function subscribeToResource(resourceType, resourceId) {
        await connection.invoke(HubMethod.JoinResource, resourceType, resourceId + "");
    }

    async function subscribeToResourceCollection(principalType, principalId, dependentType) {
        await connection.invoke(HubMethod.JoinResourceCollection, principalType, principalId + "", dependentType);
    }

    async function unsubscribeFromResourceType(resourceType) {
        await connection.invoke(HubMethod.LeaveResourceType, resourceType);
    }

    async function unsubscribeFromResource(resourceType, resourceId) {
        await connection.invoke(HubMethod.LeaveResource, resourceType, resourceId + "");
    }

    async function unsubscribeFromResourceCollection(principalType, principalId, dependentType) {
        await connection.invoke(HubMethod.LeaveResourceCollection, principalType, principalId + "", dependentType);
    }

    return {
        start,
        subscribeToResourceType,
        subscribeToResource,
        subscribeToResourceCollection,
        unsubscribeFromResourceType,
        unsubscribeFromResource,
        unsubscribeFromResourceCollection,
    };

})();
