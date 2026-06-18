window.Chat = (function () {

    let pageDto = null;
    let activeSessionId = null;

    async function init() {

        activeSessionId =
            window.chatPage.activeSessionId ?? null;

        await ChatSignalR.start();

        bindEvents();

        if (activeSessionId) {
            await loadSession(activeSessionId, false);
        }
        else {
            loadNewChat(false);
        }
    }

    function bindEvents() {

        $(document).on(
            "click",
            "#btn-new-chat",
            () => {
                loadNewChat();
            });

        $(document).on(
            "click",
            ".chat-session-item",
            async function () {

                const sessionId =
                    $(this).data("session-id");

                await loadSession(sessionId);
            });

        window.addEventListener(
            "popstate",
            async function (e) {

                const path =
                    window.location.pathname;

                const match =
                    path.match(
                        /^\/chat\/([0-9a-fA-F-]+)$/);

                if (match) {

                    await loadSession(
                        match[1],
                        false);

                    return;
                }

                loadNewChat(false);
            });
    }

    async function loadNewChat(pushHistory = true) {

        activeSessionId = null;

        if (pushHistory) {
            history.pushState(
                {},
                "",
                "/chat");
        }

        renderNewChat();
    }

    function renderNewChat() {

        const subjects = [];

        $("#subjects-list .subject-option")
            .each(function () {

                subjects.push({
                    id: $(this).data("subject-id"),
                    code: $(this).find(".subject-code").text(),
                    name: $(this).find(".subject-name").text()
                });
            });

        $("#chat-main").html(
            ChatTemplates.renderNewChat(subjects));
    }

    async function loadSession(sessionId, pushHistory = true) {

        const dto =
            await $.getJSON(
                `/chat/session/${sessionId}`);

        activeSessionId = sessionId;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                `/chat/${sessionId}`);

        }

        $("#chat-main").html(
            ChatTemplates.renderChatSession(dto));

        renderMessages(dto.messages);
    }

    function renderMessages(messages) {

        const $list = $("#message-list");

        $list.empty();

        for (const message of messages) {

            if (message.chatRole === 1) {

                $list.append(
                    ChatTemplates.renderUserMessage(
                        message));
            }
            else {

                $list.append(
                    ChatTemplates.renderAssistantMessage(
                        message));
            }
        }

        scrollToBottom();
    }

    function scrollToBottom() {

        const element =
            $("#message-list")[0];

        if (!element) {
            return;
        }

        element.scrollTop =
            element.scrollHeight;
    }

    return {
        init
    };

})();

$(async function () {

    await Chat.init();

});
