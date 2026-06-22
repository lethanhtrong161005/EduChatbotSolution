window.Chat = (function () {

    /* ==========================================================
       State
       ========================================================== */

    let sessionHeaders = [];

    let activeSessionId = null;
    let activeSession = null;

    let selectedSubjectId = null;
    let selectedSubjectText = "All of Your Subjects";

    let isSourcesPanelOpen = false;
    let activeSourcesClientId = null;

    let isProfileMenuOpen = false;
    let isAttachmentMenuOpen = false;

    let messageContent = "";
    let inputEnabled = true;

    /* ==========================================================
       State Mutation -- Session List
       ========================================================== */

    function setActiveSessionLastMessageAt(val) {

        if (!val || !activeSession
            || val === activeSession.lastMessageAt)
            return;

        activeSession.lastMessageAt = val;

        updateSidebarSessionList();
    }

    /* ==========================================================
       State Mutation -- Input Bar
       ========================================================== */

    function setMessageContent(val) {

        val = val ?? "";

        messageContent = val;

        $("#chat-message-input").val(val);
        resizeInputDrawer();

        updateSendButton();
    }

    function setInputEnabled(val) {

        inputEnabled = val;

        updateInputBar();
        updateSendButton();
    }

    function getCanSend() {
        return !!messageContent?.trim() && inputEnabled;
    }

    /* ==========================================================
       UI Observers
       ========================================================== */
    function updateInputBar() {

        $("#chat-message-input")
            .prop("disabled", !inputEnabled)
            .toggleClass("cursor-not-allowed", !inputEnabled);

        $("#btn-attach-file")
            .prop("disabled", !inputEnabled)
            .toggleClass("hover:bg-surface-hover", inputEnabled)
            .toggleClass("cursor-pointer", inputEnabled)
            .toggleClass("opacity-50", !inputEnabled)
            .toggleClass("cursor-not-allowed", !inputEnabled);
    }

    function updateSendButton() {

        let canSend = getCanSend();

        $("#btn-send-message")
            .prop("disabled", !canSend)
            .toggleClass("bg-brand", canSend)
            .toggleClass("hover:bg-brand-dark", canSend)
            .toggleClass("cursor-pointer", canSend)
            .toggleClass("bg-background-disabled", !canSend)
            .toggleClass("opacity-50", !canSend)
            .toggleClass("cursor-not-allowed", !canSend);
    }


    function updateSidebarSessionList() {

        sessionHeaders.sort((a, b) =>
            b.lastMessageAt - a.lastMessageAt);

        const $list = $("#session-list");

        $list.html(
            ChatTemplates.renderSidebarSessionList(sessionHeaders));
    }

    /* ==========================================================
       Init
       ========================================================== */

    async function init() {

        activeSessionId =
            window.chatPage.activeSessionId ?? null;

        await ChatSignalR.start();

        bindEvents();

        await loadSessionList();
        await loadSession(activeSessionId, false);
    }

    /* ==========================================================
       Sidebar
       ========================================================== */

    async function loadSessionList() {

        const sessionHeaderDtos = await $.getJSON({
            url: `/chat/sessions`,
            method: "GET",
        });

        sessionHeaders = sessionHeaderDtos;

        updateSidebarSessionList();

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
                loadSession();
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
            "popstatus",
            async function () {

                const path = window.location.pathname;

                const match = path.match(
                    /^\/chat\/([0-9a-fA-F-]+)$/);

                await loadSession(
                    match ? match[1] : null,
                    false);
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

                const msgClientId = $(this).data("client-id");

                openSourcesPanel(msgClientId);
            });

        /* Inline citations */

        $(document).on(
            "click",
            ".message-inline-citation",
            function () {

                const msgClientId =
                    $(this)
                        .closest(".assistant-message")
                        .data("client-id");

                const citationIndex =
                    $(this)
                        .data("citation-index");

                openSourcesPanel(msgClientId);

                setTimeout(
                    () => {

                        flashCitation(citationIndex);
                    },
                    150);
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

                const msgClientId = $(this).data("client-id");

                switchVariant(msgClientId, -1);
            });

        $(document).on(
            "click",
            ".response-next-btn",
            function () {

                const msgClientId = $(this).data("client-id");

                switchVariant(msgClientId, 1);
            });
    }

    function bindSignalREvents() {

        $(document).on(
            "chat:token",
            function (_, assistantMessageId, token) {

                onReceiveToken(assistantMessageId, token);
            });

        $(document).on(
            "chat:completed",
            function (_, assistantMessageId, chatMessageDto) {

                onGenerationCompleted(assistantMessageId, chatMessageDto);
            });

        $(document).on(
            "chat:failed",
            function (_, assistantMessageId, error) {

                onGenerationFailed(assistantMessageId, error);
            });

    }

    /* ==========================================================
       Session Loading
       ========================================================== */

    async function loadSession(
        sessionId = null,
        pushHistory = true) {

        if (!sessionId)
            await loadNewSession(pushHistory);
        else
            await loadExistingSession(sessionId, pushHistory);

        highlightActiveSession();

        clearInput();
        setInputEnabled(true);

        closeSourcesPanel();
    }

    async function loadNewSession(pushHistory) {

        activeSession = null;
        activeSessionId = null;
        activeSourcesClientId = null;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                "/chat");
        }

        renderNewSession();
    }

    function renderNewSession() {

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
                ChatTemplates.renderNewSession(
                    subjects));
    }

    async function loadExistingSession(sessionId, pushHistory) {

        const dto = await $.getJSON(`/chat/session/${sessionId}`);

        await ChatSignalR.switchSession(activeSessionId, sessionId);

        activeSessionId = sessionId;
        activeSession = normalizeSession(dto);
        activeSourcesClientId = null;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                `/chat/${sessionId}`);
        }

        $("#chat-main")
            .html(
                ChatTemplates.renderExistingSession(
                    activeSession));

        renderMessages(activeSession.messages);
    }

    function normalizeSession(dto) {

        dto.messages =
            (dto.messages ?? [])
                .map(normalizeMessage);

        return dto;
    }

    function normalizeMessage(message) {

        message._clientId ??= crypto.randomUUID();

        message.getContent = function () {

            return this._variants[this._activeVariant].content;
        };

        message.setContent = function (val) {

            this._variants[this._activeVariant].content = val;
        };

        message.getCitations = function () {

            return this._variants[this._activeVariant].citations;
        };

        message.setCitations = function (val) {

            this._variants[this._activeVariant].citations = val;
        };

        /* TODO */

        message._variants = [
            {
                content:
                    message.content ?? "",

                citations:
                    message.citations ?? []
            }
        ];

        message._activeVariant = 0;

        message.citations =
            (message.citations ?? [])
                .map(normalizeCitation);

        /* --- */

        return message;
    }

    function normalizeCitation(citation) {

        citation._clientId ??= crypto.randomUUID();

        citation._snippet = citation.chunkText.substring(0, 200);

        return citation;
    }

    function renderMessages(messages) {

        const $list =
            $("#message-list");

        $list.empty();

        for (const message of messages) {

            if (message.chatRole === 1) {

                $list.append(
                    ChatTemplates.renderUserMessage(message));
            }
            else {

                $list.append(
                    ChatTemplates.renderAssistantMessage(message));
            }
        }

        scrollToBottom();
    }

    /* ==========================================================
       Message Rendering
       ========================================================== */

    function updateAssistantMessage(msgClientId) {

        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

        const $container =
            $(`[data-client-id='${message._clientId}']`);

        if ($container.length === 0) {
            return;
        }

        $container.replaceWith(
            ChatTemplates.renderAssistantMessage(message));
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

        if (!getCanSend())
            return;

        const $input = $("#chat-message-input");
        if ($input.length === 0)
            return;

        const content = ($input.val() ?? "").trim();
        if (!content)
            return;

        clearInput();
        setInputEnabled(false);

        if (!activeSession) {

            await createNewSession();
        }

        const userMessage =
            appendUserMessage(content);

        const assistantMessage =
            appendStreamingAssistantMessage();

        scrollToBottom();

        const cachedLastMessagAt = activeSession.lastMessageAt;
        setActiveSessionLastMessageAt(Date.now());

        let genChatRes;

        try {
            genChatRes = await $.ajax({
                url: "/chat/generate",
                method: "POST",
                data: {
                    sessionId: activeSession.id,
                    userMessageClientId: userMessage._clientId,
                    assistantMessageClientId: assistantMessage._clientId,
                    content,
                },
            });
        } catch (err) {

            alert(err);
            // setActiveSessionLastMessageAt(cachedLastMessagAt);
        }

        if (userMessage._clientId === genChatRes.userMessageClientId) {

            userMessage.id = genChatRes.userMessageId;
            userMessage.setContent(genChatRes.userMessageContent);
            userMessage.sentAt = genChatRes.userMessageSentAt;
            userMessage.status = genChatRes.userMessageStatus
                ?? ChatEnums.MessageStatus.Completed;

            $(`[data-client-id='${userMessage._clientId}']`)
                .attr("data-message-id", userMessage.id);
        }

        if (assistantMessage._clientId === genChatRes.assistantMessageClientId) {

            assistantMessage.id = genChatRes.assistantMessageId;
            assistantMessage.sentAt = genChatRes.assistantSentAt;
            assistantMessage.status = genChatRes.assistantStatus
                ?? ChatEnums.MessageStatus.Pending;

            $(`[data-client-id='${assistantMessage._clientId}']`)
                .attr("data-message-id", assistantMessage.id);
        }
    }

    async function createNewSession() {

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

        const message = normalizeMessage(
            {
                id: null,
                _clientId: crypto.randomUUID(),
                chatRole: ChatEnums.ChatRole.User,
                content, // TODO: Impl. variants
                sentAt: null,
                status: ChatEnums.MessageStatus.Pending,
            });

        activeSession.messages.push(
            message);

        $("#message-list")
            .append(
                ChatTemplates.renderUserMessage(message));

        return message;
    }

    function appendStreamingAssistantMessage() {

        const message = normalizeMessage(
            {
                id: null,
                _clientId: crypto.randomUUID(),
                chatRole: ChatEnums.ChatRole.Assistant,
                content: "", // TODO: Impl. variants
                sentAt: null,
                status: ChatEnums.MessageStatus.Pending,
            });

        activeSession.messages.push(
            message);

        $("#message-list")
            .append(
                ChatTemplates.renderAssistantMessage(message));

        return message;
    }

    /* ==========================================================
       Streaming Token Handling
       ========================================================== */

    function onReceiveToken(assistantMessageId, token) {

        if (!activeSessionId)
            return;

        const message = findMessageById(assistantMessageId);
        if (!message)
            return;

        if (message.status === ChatEnums.MessageStatus.Pending) {

            message.status = ChatEnums.MessageStatus.Streaming;

            updateAssistantMessage(message._clientId);
        }

        message.setContent(message.getContent() + token);

        if (token) {

            updateStreamingMessageContent(message);
            scrollToBottomIfNearBottom();
        }
    }

    const StreamingMessageReRenderIntervalMs = 15;

    function updateStreamingMessageContent(message) {

        if (Date.now() - message._lastRenderAt < StreamingMessageReRenderIntervalMs)
            return;

        const $container = $(`[data-client-id='${message._clientId}']`);
        if ($container.length === 0)
            return;

        const $content = $container.find(".assistant-message-content");
        if ($content.length === 0)
            return;

        $content.html(
            ChatTemplates.renderAssistantMessageContent(message));

        message._lastRenderAt = Date.now();
    }

    function onGenerationCompleted(assistantMessageId, chatMessageDto) {

        if (!activeSessionId)
            return;

        const message = findMessageById(assistantMessageId);
        if (!message) {
            return;
        }

        if (chatMessageDto.id !== message.id) {
            return;
        }

        message.status = ChatEnums.MessageStatus.Completed;

        message.setContent(chatMessageDto.content);
        message.setCitations(
            (chatMessageDto.citations ?? [])
                .map(c => normalizeCitation(c)));

        updateAssistantMessage(message._clientId);
        scrollToBottom();

        setInputEnabled(true);
    }

    function onGenerationFailed(assistantMessageId, error) {

        if (!activeSessionId)
            return;

        const message = findMessageById(assistantMessageId);
        if (!message)
            return;

        message.status = ChatEnums.MessageStatus.Failed;

        message.generationErrors = error;

        updateAssistantMessage(message._clientId);
        scrollToBottom();

        setInputEnabled(true);
    }

    /* ==========================================================
       Variants
       ========================================================== */

    function switchVariant(msgClientId, delta) {

        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

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

        updateMessageFromVariant(message);

        updateAssistantMessage(message._clientId);

        if (activeSourcesClientId === message._clientId) {

            openSourcesPanel(message._clientId);
        }

    }

    function updateMessageFromVariant(message) {

        const variant =
            message._variants[
            message._activeVariant];

        message.content = variant.content;
        message.citations = variant.citations;
    }

    /* ==========================================================
       Copy Message
       ========================================================== */

    async function copyMessage(msgClientId) {

        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

        await navigator.clipboard.writeText(
            message.getContent() ?? "");
    }

    /* ==========================================================
       Regenerate
       ========================================================== */

    function regenerateMessage(
        msgClientId) {

        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

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

        updateMessageFromVariant(message);

        updateAssistantMessage(message._clientId);

        if (activeSourcesClientId === message._clientId) {

            openSourcesPanel(message._clientId);
        }
    }

    /* ==========================================================
       Sources Panel
       ========================================================== */

    function openSourcesPanel(msgClientId) {

        activeSourcesClientId = msgClientId;
        isSourcesPanelOpen = true;

        const $panel = $("#sources-panel");
        const $content = $("#sources-panel-content");

        const message = findMessageByClientId(msgClientId);

        if (!message) {
            return;
        }

        const citations = message.getCitations() ?? [];

        $content.empty();

        citations
            .sort(
                (a, b) =>
                    a.citationIndex -
                    b.citationIndex)
            .forEach(citation => {

                $content.append(
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

    function flashCitation(citationIndex) {

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

    function findMessageById(messageId) {

        if (!activeSession)
            return null;

        return activeSession.messages.find(
            x => x.id === messageId);
    }

    function findMessageByClientId(msgClientId) {

        if (!activeSession) {
            return null;
        }

        return activeSession.messages.find(
            x => x._clientId === msgClientId);
    }

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
            container.scrollHeight
            - container.scrollTop
            - container.clientHeight;

        return remaining < 100;
    }

    function scrollToBottomIfNearBottom() {

        if (isNearBottom()) {

            scrollToBottom();
        }
    }

    /* ==========================================================
       Public API
       ========================================================== */

    return {
        init,
    };


})();

$(async function () {

    await Chat.init();

});
