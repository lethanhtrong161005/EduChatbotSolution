window.Chat = (function () {


    /* ==========================================================
       State
       ========================================================== */

    let signalr;

    let selectedSubjectId = null;
    let selectedSubjectText = "All of Your Subjects";

    let activeSessionId = null;
    let activeSourcesClientId = null;
    let currentSession = null;

    let isSourcesPanelOpen = false;
    let isProfileMenuOpen = false;
    let isAttachmentMenuOpen = false;

    let messageContent = null;
    let inputEnabled = true;

    /* ==========================================================
       State Mutation -- Input Bar
       ========================================================== */

    function setMessageContent(val) {

        val = val?.trim() ?? "";

        messageContent = val;

        $("#chat-message-input").val(val);
        resizeInputDrawer();

        updateSendButton();
    }

    function setInputEnabled(val) {

        inputEnabled = val;

        // TODO: Add overlay over text input.

        updateSendButton();
    }

    function getCanSend() {
        return !!messageContent.trim() && inputEnabled;
    }

    /* ==========================================================
       Observers
       ========================================================== */

    function updateSendButton() {

        let canSend = getCanSend();

        $("#btn-send-message")
            .prop("disabled", !canSend)
            .toggleClass("opacity-50", !canSend)
            .toggleClass("bg-brand", canSend)
            .toggleClass("bg-background-disabled", !canSend)
            .toggleClass("hover:bg-brand-dark", canSend)
            .toggleClass("cursor-pointer", canSend)
            .toggleClass("cursor-not-allowed", !canSend);
    }

    /* ==========================================================
       Init
       ========================================================== */

    async function init() {

        activeSessionId =
            window.chatPage.activeSessionId ?? null;

        signalr = await ChatSignalR.start();

        loadSessionList();

        bindEvents();

        if (activeSessionId) {

            await loadSession(
                activeSessionId,
                false);
        }
        else {

            loadNewChat(false);
        }
    }

    /* ==========================================================
       Sidebar
       ========================================================== */

    async function loadSessionList() {

        const response = await $.getJSON({
            url: `/chat/sessions`,
            method: "GET",
        });

        const $list = $("#session-list");

        $list.html(
            ChatTemplates.renderSidebarSessionList(
                response)
        );

        highlightActiveSession();
    }

    /* ==========================================================
       Event Registration
       ========================================================== */

    function bindEvents() {

        bindNavigationEvents();

        bindProfileEvents();

        bindSubjectEvents();

        bindSourcesEvents();

        bindAttachmentEvents();

        bindModalEvents();

        bindMessageEvents();

        bindSignalREvents();
    }

    function bindNavigationEvents() {

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
            async function () {

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

    function bindProfileEvents() {

        $(document).on(
            "click",
            "#btn-profile-menu",
            function (e) {

                e.stopPropagation();

                toggleProfileMenu();
            });

        $(document).on(
            "click",
            function () {

                closeProfileMenu();
            });
    }

    function bindSourcesEvents() {

        $(document).on(
            "click",
            "#btn-close-sources",
            function () {

                closeSourcesPanel();
            });

        $(document).on(
            "click",
            ".citation-details-toggle",
            function () {

                const $card =
                    $(this).closest(".source-card");

                $card
                    .find(".citation-details")
                    .stop(true, true)
                    .slideToggle(150);
            });
    }

    function bindAttachmentEvents() {

        $(document).on(
            "click",
            "#btn-attach-file",
            function (e) {

                e.stopPropagation();

                toggleAttachmentMenu();
            });

        $(document).on(
            "click",
            "#btn-upload-file",
            function () {

                $("#chat-file-input")
                    .trigger("click");

                closeAttachmentMenu();
            });

        $(document).on(
            "click",
            "#btn-file-library",
            function () {

                openFileLibraryModal();
                closeAttachmentMenu();
            });

        $(document).on(
            "click",
            function () {

                closeAttachmentMenu();
            });
    }

    function bindModalEvents() {

        $(document).on(
            "click",
            "#btn-file-library-close",
            closeFileLibraryModal);

        $(document).on(
            "click",
            "#btn-file-library-cancel",
            function (e) {

                if (e.target === this) {

                    closeFileLibraryModal();

                }
            });

        $(document).on(
            "click",
            "#file-library-backdrop",
            function (e) {

                if (e.target === this) {

                    closeFileLibraryModal();

                }
            });
    }

    function bindSubjectEvents() {

        $(document).on(
            "click",
            "#btn-select-subject",
            function (e) {

                e.stopPropagation();

                $("#subject-dropdown")
                    .toggleClass("hidden");
            });

        $(document).on(
            "click",
            function () {

                $("#subject-dropdown")
                    .addClass("hidden");
            });

        $(document).on(
            "click",
            ".subject-dropdown-item",
            function (e) {

                e.stopPropagation();

                selectedSubjectId =
                    $(this).data("subject-id") || null;

                selectedSubjectText =
                    $(this).text();

                $("#selected-subject-text")
                    .text(selectedSubjectText);

                $("#subject-dropdown")
                    .addClass("hidden");
            });
    }

    function bindMessageEvents() {

        /* Type */

        $(document).on(
            "input",
            "#chat-message-input",
            function (e) {

                e.preventDefault();

                setMessageContent(e.currentTarget.value);

                resizeInputDrawer();
            });

        /* Send */

        $(document).on(
            "click",
            "#btn-send-message",
            sendCurrentMessage);

        $(document).on(
            "keydown",
            "#chat-message-input",
            function (e) {

                if (e.key === "Enter" &&
                    !e.shiftKey) {

                    e.preventDefault();

                    if (!getCanSend())
                        return;

                    sendCurrentMessage();
                }
            });

        /* Copy */

        $(document).on(
            "click",
            ".message-copy-btn",
            async function () {

                const msgClientId =
                    $(this).data("client-id");

                await copyMessage(
                    msgClientId);
            });

        /* Sources */

        $(document).on(
            "click",
            ".message-sources-btn",
            function () {

                const msgClientId =
                    $(this).data("client-id");

                openSourcesPanel(
                    msgClientId);
            });

        /* Regenerate */

        $(document).on(
            "click",
            ".message-regenerate-btn",
            function () {

                const msgClientId =
                    $(this).data("client-id");

                regenerateMessage(
                    msgClientId);
            });

        /* Variants */

        $(document).on(
            "click",
            ".response-prev-btn",
            function () {

                const msgClientId =
                    $(this).data("client-id");

                switchVariant(
                    msgClientId,
                    -1);
            });

        $(document).on(
            "click",
            ".response-next-btn",
            function () {

                const msgClientId =
                    $(this).data("client-id");

                switchVariant(
                    msgClientId,
                    1);
            });

        /* Inline citations */

        $(document).on(
            "click",
            ".message-inline-citation",
            function () {

                const msgClientId =
                    $(this).data("client-id");

                const citationIndex =
                    $(this).data("citation-index");

                openCitation(
                    msgClientId,
                    citationIndex);
            });

    }

    function bindSignalREvents() {

        $(document).on(
            "chat:token",
            function (_, assistantMessageClientId, token) {

                onReceiveToken(assistantMessageClientId, token);
            });

        $(document).on(
            "chat:completed",
            function (_, assistantMessageClientId, chatMessageDto) {

                onGenerationCompleted(assistantMessageClientId, chatMessageDto);
            });

        $(document).on(
            "chat:failed",
            function (_, [assistantMessageClientId, error]) {

                onGenerationFailed(assistantMessageClientId, error);
            });

    }

    /* ==========================================================
       New Chat
       ========================================================== */

    async function loadNewChat(pushHistory = true) {

        currentSession = null;
        activeSessionId = null;
        activeSourcesClientId = null;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                "/chat");
        }

        renderNewChat();

        clearInput();
        closeSourcesPanel();
    }

    function renderNewChat() {

        const subjects = [];

        $("#subjects-list .subject-option")
            .each(function () {

                subjects.push({
                    id: $(this).data("subject-id"),
                    code: $(this)
                        .find(".subject-code")
                        .text(),
                    name: $(this)
                        .find(".subject-name")
                        .text()
                });
            });

        $("#chat-main")
            .html(
                ChatTemplates.renderNewChat(
                    subjects));
    }

    /* ==========================================================
       Session Loading
       ========================================================== */

    async function loadSession(
        sessionId,
        pushHistory = true) {

        const dto =
            await $.getJSON(
                `/chat/session/${sessionId}`);

        await ChatSignalR.switchSession(activeSessionId, sessionId);

        activeSessionId =
            sessionId;

        currentSession =
            normalizeSession(dto);

        activeSourcesClientId =
            null;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                `/chat/${sessionId}`);
        }

        $("#chat-main")
            .html(
                ChatTemplates.renderChatSession(
                    currentSession));

        renderMessages(
            currentSession.messages);

        clearInput();
        closeSourcesPanel();

        highlightActiveSession();
    }

    function normalizeSession(dto) {

        dto.messages =
            (dto.messages ?? [])
                .map(normalizeMessage);

        return dto;
    }

    function normalizeMessage(message) {

        message.clientId ??= crypto.randomUUID();

        if (message.chatRole !== 2) {
            return message;
        }

        message._variants = [
            {
                content:
                    message.content ?? "",

                citations:
                    message.citations ?? []
            }
        ];

        message._activeVariant = 0;

        return message;
    }

    /* ==========================================================
       Message Rendering
       ========================================================== */

    function renderMessages(messages) {

        const $list =
            $("#message-list");

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

    function reRenderAssistantMessage(
        msgClientId) {

        const message =
            findMessage(msgClientId);

        if (!message) {
            return;
        }

        const $container =
            $(`[data-elem-id='${message.clientId}']`);

        if ($container.length === 0) {
            return;
        }

        $container.replaceWith(
            ChatTemplates.renderAssistantMessage(
                message));
    }

    /* ==========================================================
       Input Drawer
       ========================================================== */

    function resizeInputDrawer() {

        const textarea =
            $("#chat-message-input")[0];

        if (!textarea) {
            return;
        }

        textarea.style.height = "auto";

        const lineHeight = 24;
        const maxLines = 10;
        const maxHeight = lineHeight * maxLines;

        const targetHeight =
            Math.min(
                textarea.scrollHeight,
                maxHeight);

        textarea.style.height = `${targetHeight}px`;

        const isExpanded = textarea.scrollHeight > lineHeight + 8;

        const $drawer =
            $("#chat-input-drawer");

        if (isExpanded) {

            $drawer.addClass(
                "chat-input-drawer-open");

            $(textarea)
                .addClass(
                    "chat-input-expanded");
        }
        else {

            collapseInputDrawer();
        }

        textarea.style.overflowY =
            textarea.scrollHeight >
                maxHeight
                ? "auto"
                : "hidden";

    }

    function collapseInputDrawer() {

        $("#chat-input-drawer")
            .removeClass(
                "chat-input-drawer-open");

        const textarea =
            $("#chat-message-input")[0];

        if (!textarea) {
            return;
        }

        textarea.style.height =
            "";

        textarea.style.overflowY =
            "hidden";

        $(textarea)
            .removeClass(
                "chat-input-expanded");

    }

    /* ==========================================================
       Message Sending
       ========================================================== */

    async function sendCurrentMessage() {

        const $input = $("#chat-message-input");
        if ($input.length === 0)
            return;

        const content = ($input.val() ?? "").trim();
        if (!content)
            return;

        clearInput();
        setInputEnabled(false);

        if (!currentSession) {

            await createNewSession(content);
        }

        const userMessage =
            appendUserMessage(content);

        const assistantMessage =
            appendStreamingAssistantMessage();

        scrollToBottom();

        const response = await $.ajax({
            url: "/chat/generate",
            method: "POST",
            data: {
                sessionId: currentSession.id,
                userMessageClientId: userMessage.clientId,
                assistantMessageClientId: assistantMessage.clientId,
                content
            },
        });

        userMessage.id = response.userMessageId;
        userMessage.state = MessageState.Completed;

        $(`[data-elem-id='${userMessage.clientId}']`)
            .attr("data-message-id", userMessage.id);

        assistantMessage.id = response.assistantMessageId;
        assistantMessage.state = MessageState.Streaming;

        $(`[data-elem-id='${assistantMessage.clientId}']`)
            .attr("data-message-id", assistantMessage.id);

        setInputEnabled(true);
    }

    async function createNewSession(content) {

        const subjectId = selectedSubjectId;

        const response =
            await $.ajax({
                url: "/chat/session",
                method: "POST",
                data: {
                    subjectId,
                },
            });

        await loadSessionList();
        await loadSession(response.sessionId);
    }

    function appendUserMessage(content) {

        const message = {

            id: null,
            clientId: crypto.randomUUID(),
            chatRole: ChatRole.User,
            content,
            state: MessageState.Pending,
        };

        currentSession.messages.push(
            message);

        $("#message-list")
            .append(
                ChatTemplates.renderUserMessage(
                    message));

        return message;
    }

    function appendStreamingAssistantMessage() {

        const message = {

            id: null,
            clientId: crypto.randomUUID(),
            chatRole: ChatRole.Assistant,
            content: "",
            state: MessageState.Streaming,
            citations: [],
            _variants: [
                {
                    content: "",
                    citations: []
                }
            ],
            _activeVariant: 0
        };

        currentSession.messages.push(
            message);

        $("#message-list")
            .append(
                ChatTemplates.renderStreamingAssistantMessage(
                    message));

        return message;
    }

    /* ==========================================================
       Streaming Token Handling
       ========================================================== */

    function onReceiveToken(assistantMessageClientId, token) {

        if (!activeSessionId)
            return;

        const message =
            findMessage(
                assistantMessageClientId);

        if (!message) {
            return;
        }

        const variant =
            message._variants[
            message._activeVariant];

        variant.content += token;

        message.content =
            variant.content;

        updateStreamingMessageDom(
            message);

        scrollToBottomIfNeeded();
    }

    function updateStreamingMessageDom(
        message) {

        const $container =
            $(`[data-elem-id='${message.clientId}']`);

        if ($container.length === 0) {
            return;
        }

        const $content =
            $container.find(
                ".assistant-streaming");

        if ($content.length === 0) {
            return;
        }

        $content.html(
            marked.parse(
                message.content ?? ""));

    }

    function onGenerationCompleted(assistantMessageClientId, chatMessageDto) {

        if (!activeSessionId)
            return;

        const message =
            findMessage(
                assistantMessageClientId);

        if (!message) {
            return;
        }

        if (message.id != chatMessageDto.id) {
            return;
        }

        message.state = MessageState.Completed;

        message.sentAt = chatMessageDto.sentAt;

        const variant = message._variants[message._activeVariant];
        variant.content = chatMessageDto.content;
        variant.citations = chatMessageDto.citations ?? [];

        message.content = variant.content;
        message.citations = variant.citations ?? [];

        reRenderAssistantMessage(message.clientId);

        scrollToBottom();
    }

    function onGenerationFailed(assistantMessageClientId, error) {

        if (!activeSessionId)
            return;

        const message =
            findMessage(
                assistantMessageClientId);

        if (message) {

            message.state = MessageState.Failed;

            const variant =
                message._variants[
                message._activeVariant];

            variant.content +=

                `\n\n-- -\n\n⚠ Generation failed.\n\n${error}`

            message.content =
                variant.content;

            reRenderAssistantMessage(
                message.clientId);
        }
    }

    /* ==========================================================
       Variants
       ========================================================== */

    function switchVariant(msgClientId, delta) {

        const message =
            findMessage(msgClientId);

        if (!message) {
            return;
        }

        const count =
            message._variants.length;

        if (count <= 1) {
            return;
        }

        let nextIndex =
            message._activeVariant +
            delta;

        if (nextIndex < 0) {
            nextIndex = count - 1;
        }

        if (nextIndex >= count) {
            nextIndex = 0;
        }

        message._activeVariant =
            nextIndex;

        updateMessageFromVariant(
            message);

        reRenderAssistantMessage(
            message.clientId);

        if (activeSourcesClientId === message.clientId) {

            openSourcesPanel(
                message.clientId);
        }

    }

    function updateMessageFromVariant(
        message) {

        const variant =
            message._variants[
            message._activeVariant];

        message.content =
            variant.content;

        message.citations =
            variant.citations;

    }

    /* ==========================================================
       Copy Message
       ========================================================== */

    async function copyMessage(msgClientId) {

        const message =
            findMessage(msgClientId);

        if (!message) {
            return;
        }

        const variant =
            message._variants[
            message._activeVariant];

        await navigator.clipboard.writeText(
            variant.content ?? "");

    }

    /* ==========================================================
       Regenerate
       ========================================================== */

    function regenerateMessage(
        msgClientId) {

        const message =
            findMessage(msgClientId);

        if (!message) {
            return;
        }

        const variantNumber =
            message._variants.length + 1;

        /* TODO */

        const newVariant = {

            content:
                `${message.content}`
        }

        /* ---- */

        message._variants.push(
            newVariant);

        message._activeVariant =
            message._variants.length - 1;

        updateMessageFromVariant(
            message);

        reRenderAssistantMessage(
            message.clientId);

        if (activeSourcesClientId === message.clientId) {

            openSourcesPanel(
                message.clientId);
        }

    }

    /* ==========================================================
       Sources Panel
       ========================================================== */

    function openSourcesPanel(
        msgClientId) {

        activeSourcesClientId =
            msgClientId;

        isSourcesPanelOpen = true;

        const $panel =
            $("#sources-panel");

        const $body =
            $("#sources-panel-body");

        const message =
            findMessage(msgClientId);

        if (!message) {
            return;
        }

        const variant =
            message._variants[
            message._activeVariant];

        const citations =
            variant.citations ?? [];

        $body.empty();

        citations
            .sort(
                (a, b) =>
                    a.citationIndex -
                    b.citationIndex)
            .forEach(citation => {

                $body.append(
                    ChatTemplates.renderSourceCard(
                        citation));
            });

        $panel
            .removeClass("hidden");

        requestAnimationFrame(() => {

            $panel
                .removeClass("w-0 opacity-0")
                .addClass(
                    "w-[420px] opacity-100");
        });
    }

    function closeSourcesPanel() {

        isSourcesPanelOpen = false;

        const $panel =
            $("#sources-panel");

        $panel
            .removeClass(
                "w-[420px] opacity-100")
            .addClass(
                "w-0 opacity-0");

        setTimeout(
            () => {

                if (!isSourcesPanelOpen) {

                    $panel.addClass("hidden");

                }

            },
            200);
    }

    function flashCitation(
        citationIndex) {

        const $card =
            $(`.source-card[data-citation-index='${citationIndex}']`);

        if ($card.length === 0) {
            return;
        }

        $card[0]
            .scrollIntoView({
                block: "center",
                behavior: "smooth"
            });

        $card
            .removeClass(
                "source-card-flash");

        void $card[0].offsetWidth;

        $card
            .addClass(
                "source-card-flash");
    }

    /* ==========================================================
       Profile Menu
       ========================================================== */

    function toggleProfileMenu() {

        if (isProfileMenuOpen) {

            closeProfileMenu();

            return;
        }

        $("#profile-dropdown")
            .removeClass("hidden");

        isProfileMenuOpen = true;
    }

    function closeProfileMenu() {

        $("#profile-dropdown")
            .addClass("hidden");

        isProfileMenuOpen = false;
    }

    /* ==========================================================
       Attachment Menu
       ========================================================== */

    function toggleAttachmentMenu() {

        if (isAttachmentMenuOpen) {

            closeAttachmentMenu();

            return;
        }

        $("#attachment-menu")
            .removeClass("hidden");

        isAttachmentMenuOpen = true;
    }

    function closeAttachmentMenu() {

        $("#attachment-menu")
            .addClass("hidden");

        isAttachmentMenuOpen = false;
    }

    /* ==========================================================
       File Library Modal
       ========================================================== */

    function openFileLibraryModal() {

        $("#file-library-modal")
            .removeClass("hidden");
    }

    function closeFileLibraryModal() {

        $("#file-library-modal")
            .addClass("hidden");
    }

    /* ==========================================================
       Helpers
       ========================================================== */

    function highlightActiveSession() {

        $(".chat-session-item")
            .removeClass(
                "chat-session-item-active");

        if (!activeSessionId) {
            return;
        }

        $(`.chat-session-item[data-session-id='${activeSessionId}']`)
            .addClass(
                "chat-session-item-active");
    }

    function clearInput() {
        setMessageContent("");
    }

    function findMessage(msgClientId) {

        if (!currentSession) {
            return null;
        }

        return currentSession.messages.find(
            x => x.clientId === msgClientId);
    }

    function openCitation(
        msgClientId,
        citationIndex) {

        openSourcesPanel(
            msgClientId);

        setTimeout(
            () => {

                flashCitation(
                    citationIndex);

            },
            150);
    }

    const MessageState = Object.freeze({
        Pending: 0,
        Streaming: 1,
        Completed: 2,
        Failed: 3,
    });

    const ChatRole = Object.freeze({
        System: 0,
        User: 1,
        Assistant: 2,
    });

    /* ==========================================================
       Scrolling
       ========================================================== */

    function scrollToBottom() {

        const element =
            $("#message-list")[0];

        if (!element) {
            return;
        }

        element.scrollTop =
            element.scrollHeight;
    }

    function preserveScrollPosition(
        callback) {

        const container =
            $("#message-list")[0];

        if (!container) {

            callback();

            return;
        }

        const distanceFromBottom =
            container.scrollHeight -
            container.scrollTop -
            container.clientHeight;

        callback();

        container.scrollTop =
            container.scrollHeight -
            container.clientHeight -
            distanceFromBottom;

    }

    function isNearBottom() {

        const container =
            $("#message-list")[0];

        if (!container) {
            return true;
        }

        const remaining =
            container.scrollHeight -
            container.scrollTop -
            container.clientHeight;

        return remaining < 150;

    }

    function scrollToBottomIfNeeded() {

        if (isNearBottom()) {

            scrollToBottom();
        }
    }

    /* ==========================================================
       Public API
       ========================================================== */

    return {
        init,
        openSourcesPanel,
        closeSourcesPanel,
        flashCitation,
        reRenderAssistantMessage,
        scrollToBottom
    };


})();

$(async function () {

    await Chat.init();

});
