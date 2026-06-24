window.ChatSignalR = (function () {

    let connection = null;

    let activeSessionId = null;

    async function start() {

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/chat/answer")
            .withAutomaticReconnect()
            .build();

        connection.on("TitleGenerated",
            (sessionId, title) => {

                $(document).trigger(
                    "chat:title",
                    [sessionId, title]);
            });

        connection.on("StreamingStarted",
            (assistantMessageId, assistantMessageClientId) => {

                $(document).trigger(
                    "chat:stream",
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
    }

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
