window.ChatSignalR = (function () {

    let connection = null;

    async function start() {

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/chat/answer")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveToken",
            (assistantMessageClientId, token) => {

                $(document).trigger(
                    "chat:token",
                    [assistantMessageClientId, token]);
            });

        connection.on("GenerationCompleted",
            (assistantMessageClientId, chatMessageDto) => {

                $(document).trigger(
                    "chat:completed",
                    [assistantMessageClientId, chatMessageDto]);
            });

        connection.on("GenerationFailed",
            (assistantMessageClientId, error) => {

                $(document).trigger(
                    "chat:failed",
                    [assistantMessageClientId, error]);
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
