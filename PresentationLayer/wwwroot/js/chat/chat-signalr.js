window.ChatSignalR = (function () {

    let connection = null;

    async function start() {

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/chat/answer")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveToken",
            (sessionId, token) => {

                $(document).trigger(
                    "chat:token",
                    [sessionId, token]);
            });

        connection.on("GenerationCompleted",
            (sessionId, assistantMessageId) => {

                $(document).trigger(
                    "chat:completed",
                    [sessionId, assistantMessageId]);
            });

        connection.on("GenerationFailed",
            (sessionId, error) => {

                $(document).trigger(
                    "chat:failed",
                    [sessionId, error]);
            });

        await connection.start();
    }

    return {
        start
    };

})();
