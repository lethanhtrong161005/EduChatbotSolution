window.ChatSignalR = (function () {

    let connection = null;

    async function start() {

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/chat/answer")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveToken",
            (assistantMessageId, token) => {

                $(document).trigger(
                    "chat:token",
                    [assistantMessageId, token]);
            });

        connection.on("GenerationCompleted",
            (assistantMessageId, chatMessageDto) => {

                $(document).trigger(
                    "chat:completed",
                    [assistantMessageId, chatMessageDto]);
            });

        connection.on("GenerationFailed",
            (assistantMessageId, error) => {

                $(document).trigger(
                    "chat:failed",
                    [assistantMessageId, error]);
            });

        await connection.start();
    }

    async function switchSession(oldSessionId, newSessionId) {

        if (oldSessionId)
            await connection.invoke("LeaveSession", oldSessionId);

        await connection.invoke("JoinSession", newSessionId);
    }

    return {
        start,
        switchSession,
    };

})();
