window.Chat = (function () {

    /* ==========================================================
       State
       ========================================================== */

    let _subjectHeaders = [];
    let _scopeSubjectHeaders = [];

    let _sessionHeaders = [];

    let _activeSession = null;

    let _dropdownSelectedSubjectId = null;
    let _dropdownSelectedSubjectText = "All of Your Subjects";

    let _isSourcesPanelOpen = false;
    let _activeSourcesClientId = null;

    let _isProfileMenuOpen = false;
    let _isAttachmentMenuOpen = false;

    let _messageContent = "";
    let _inputEnabled = true;

    let _concurrencyToken = null;

    /* ==========================================================
       State Mutation -- Active Session & Session List
       ========================================================== */

    function setActiveSessionLastMessageAt(val) {

        if (!val || !_activeSession
            || val === _activeSession.lastMessageAt)
            return;

        _activeSession.lastMessageAt = val;

        const sessionHeader = _sessionHeaders.
            find(x => x.id === _activeSession.id);

        if (!sessionHeader)
            return;

        sessionHeader.lastMessageAt = val;

        updateDom_SidebarSessionList();
    }

    /* ==========================================================
       State Mutation -- Input Bar
       ========================================================== */

    function setMessageContent(val) {

        _messageContent = val ?? "";

        $("#chat-message-input").val(_messageContent);

        updateDom_InputDrawer();
        updateDom_SendButton();
    }

    function setInputEnabled(val) {

        _inputEnabled = val;

        updateDom_InputBar();
        updateDom_SendButton();
    }

    function getCanSend() {
        return !!_messageContent?.trim() && _inputEnabled;
    }

    /* ==========================================================
       Observers -- State
       ========================================================== */

    function updateState_ScopeSubjectHeaders() {

        // On:
        // * subjectHeaders
        // * activeSession

        if (!_activeSession) { // Landing session
            _scopeSubjectHeaders = [];
            updateDom_SubjectSelectList();
        }
        else { // Existing session
            const subjectId = _activeSession.subjectId;
            if (subjectId === undefined)
                return;

            if (subjectId === null) { // All accessible subjects
                _scopeSubjectHeaders = [..._subjectHeaders];
            }
            else {
                const subjectHeader = _subjectHeaders
                    .find(x => x.id === subjectId);

                if (!subjectHeader) {
                    _scopeSubjectHeaders = [];
                }
                else {
                    _scopeSubjectHeaders = [subjectHeader];
                }
            }
        }

        updateDom_SidebarSubjectList();
    }

    /* ==========================================================
       Observers -- UI
       ========================================================== */

    function updateDom_InputBar() {

        // On:
        // * _inputEnabled

        $("#chat-message-input")
            .prop("disabled", !_inputEnabled)
            .toggleClass("cursor-not-allowed", !_inputEnabled);

        $("#btn-attach-file")
            .prop("disabled", !_inputEnabled)
            .toggleClass("hover:bg-surface-hover", _inputEnabled)
            .toggleClass("cursor-pointer", _inputEnabled)
            .toggleClass("opacity-50", !_inputEnabled)
            .toggleClass("cursor-not-allowed", !_inputEnabled);
    }

    function updateDom_SendButton() {

        // On:
        // * _messageContent
        // * _inputEnabled

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

    function updateDom_SidebarSubjectList() {

        // On:
        // * _scopeSubjectHeaders

        _scopeSubjectHeaders.sort((a, b) =>
            a.code - b.code);

        $("#subject-list").html(
            ChatTemplates.renderSidebarSubjectList(_scopeSubjectHeaders));
    }

    function updateDom_SidebarSessionList() {

        // On:
        // * _sessionHeaders[*].*

        _sessionHeaders.sort((a, b) =>
            b.lastMessageAt - a.lastMessageAt);

        const $list = $("#session-list");

        $("#session-list").html(
            ChatTemplates.renderSidebarSessionList(_sessionHeaders));

        updateDom_ActiveSessionHighlight();
    }

    function updateDom_ActiveSessionHighlight() {

        // On:
        // * _activeSession.id

        $(".chat-session-item")
            .removeClass("chat-session-item-active");

        if (!_activeSession?.id) {
            return;
        }

        $(`.chat-session-item[data-session-id='${_activeSession.id}']`)
            .addClass("chat-session-item-active");
    }

    function updateDom_SubjectSelectList() {

        // On:
        // * _scopeSubjectHeaders

        _subjectHeaders.sort((a, b) =>
            a.code - b.code);

        $("#chat-message-input-container").html(
            ChatTemplates.renderChatMessageInput(_subjectHeaders));

        clearInput();
    }

    function updateDom_AssistantMessage(message) {

        // On:
        // * _activeSession.messages[#].*

        const $container =
            $(`[data-client-id='${message._clientId}']`);

        if ($container.length === 0) {
            return;
        }

        $container.replaceWith(
            ChatTemplates.renderAssistantMessage(message));
    }

    const StreamingMessageReRenderIntervalMs = 15;

    function updateDom_StreamingMessageContent(message) {

        // On:
        // * _activeSession.messages[#].content

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

    /* ==========================================================
       Init
       ========================================================== */

    async function init() {

        await ResourceSignalR.start();
        await ChatSignalR.start();

        bindEvents();

        const conTkn = _concurrencyToken = crypto.randomUUID();

        await loadSubjectList(conTkn);
        await loadSessionList(conTkn);
        await loadSession(conTkn, Razor.activeSessionId ?? null, false);

        await subscribeToResourceGroups();
    }

    async function subscribeToResourceGroups() {

        // Subjects are subscribed to whenever they are loaded

        await ResourceSignalR.subscribeToResourceCollection(ResourceType.User, Razor.userId, ResourceType.Membership);
        await ResourceSignalR.subscribeToResourceCollection(ResourceType.User, Razor.userId, ResourceType.ChatSession);
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
                loadSession(_concurrencyToken = crypto.randomUUID());
            });

        $(document).on(
            "click",
            ".chat-session-item",
            async function () {

                const sessionId =
                    $(this).data("session-id");

                await loadSession(_concurrencyToken = crypto.randomUUID(), sessionId);
            });

        window.addEventListener(
            "popstate",
            async function () {

                const path = window.location.pathname;

                const match = path.match(
                    /^\/chat\/([0-9a-fA-F-]+)$/);

                await loadSession(
                    _concurrencyToken = crypto.randomUUID(),
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

                _dropdownSelectedSubjectId =
                    $(this).data("subject-id") || null;

                _dropdownSelectedSubjectText =
                    $(this).text();

                $("#selected-subject-text")
                    .text(_dropdownSelectedSubjectText);

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
            "resource:changed",
            async function (_, resUpd) {

                onResourceChanged(resUpd);
            }
        );

        $(document).on(
            "chat:exchange",
            function (_, userMessageId, userMessageClientId, assistantMessageId, assistantMessageClientId) {

                onExchangeCreated(userMessageId, userMessageClientId, assistantMessageId, assistantMessageClientId);
            });

        $(document).on(
            "chat:started",
            function (_, assistantMessageId, assistantMessageClientId) {

                onGenerationStarted(assistantMessageId, assistantMessageClientId);
            });

        $(document).on(
            "chat:token",
            function (_, assistantMessageId, assistantMessageClientId, token) {

                onReceiveToken(assistantMessageId, assistantMessageClientId, token);
            });

        $(document).on(
            "chat:completed",
            function (_, assistantMessageId, assistantMessageClientId, chatMessageDto) {

                onGenerationCompleted(assistantMessageId, assistantMessageClientId, chatMessageDto);
            });

        $(document).on(
            "chat:failed",
            function (_, assistantMessageId, assistantMessageClientId, error) {

                onGenerationFailed(assistantMessageId, assistantMessageClientId, error);
            });
    }

    /* ==========================================================
       Sidebar
       ========================================================== */

    async function loadSubjectList(conTkn) {

        const oldSubjectHeaders = _subjectHeaders;

        const subjectHeaderDtos =
            await $.ajax({
                url: `/chat?handler=GetSubjectHeaders`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                },
            });

        if (_concurrencyToken != conTkn)
            return;

        const newSubjectHeaders =
            subjectHeaderDtos
                .map(normalizeSubjectHeader)
                .sort((a, b) => a.code - b.code);

        await adjustSubscriptions_Subject(oldSubjectHeaders, newSubjectHeaders);

        _subjectHeaders = newSubjectHeaders;

        updateState_ScopeSubjectHeaders();
    }

    async function adjustSubscriptions_Subject(oldSubjectHeaders, newSubjectHeaders) {

        const { added, removed } = diffById(oldSubjectHeaders, newSubjectHeaders);

        await Promise.all([
            ...added.map(x =>
                ResourceSignalR.subscribeToResource(ResourceType.Subject, x.id)),
            ...removed.map(x =>
                ResourceSignalR.unsubscribeFromResource(ResourceType.Subject, x.id)),
        ]);
    }

    function normalizeSubjectHeader(subjectHeader) {

        subjectHeader._clientId ??= crypto.randomUUID();

        return subjectHeader;
    }

    async function loadSessionList(conTkn) {

        const sessionHeaderDtos =
            await $.ajax({
                url: `/chat?handler=GetSessionHeaders`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                },
            });

        if (_concurrencyToken != conTkn)
            return;

        _sessionHeaders = sessionHeaderDtos.map(normalizeSessionHeader);

        updateDom_SidebarSessionList();
    }

    function normalizeSessionHeader(sessionHeader) {

        sessionHeader._clientId ??= crypto.randomUUID();

        return sessionHeader;
    }

    /* ==========================================================
       Session Loading
       ========================================================== */

    async function loadSession(
        conTkn,
        sessionId = null,
        pushHistory = true) {

        if (!sessionId)
            await loadLandingSession(pushHistory);
        else
            await loadExistingSession(conTkn, sessionId, pushHistory);

        if (_concurrencyToken != conTkn)
            return;

        updateState_ScopeSubjectHeaders();
        updateDom_ActiveSessionHighlight();

        clearInput();
        setInputEnabled(true);

        closeSourcesPanel();
    }

    /* WARN: Always call through loadSession(null) */
    async function loadLandingSession(pushHistory) {

        _activeSession = null;
        _activeSourcesClientId = null;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                "/chat");
        }

        $("#chat-main").html(
            ChatTemplates.renderLandingSession(_subjectHeaders));
    }

    /* WARN: Always call through loadSession(sessionId) */
    async function loadExistingSession(conTkn, sessionId, pushHistory) {

        const dto =
            await $.ajax({
                url: `/chat?handler=GetSession&id=${sessionId}`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                },
            });

        if (_concurrencyToken !== conTkn)
            return;

        await ChatSignalR.switchSession(sessionId);

        if (_concurrencyToken !== conTkn)
            return;

        _activeSession = normalizeSession(dto);
        _activeSourcesClientId = null;

        if (pushHistory) {

            history.pushState(
                {},
                "",
                `/chat/${sessionId}`);
        }

        $("#chat-main").html(
            ChatTemplates.renderExistingSession(_activeSession));

        renderMessages(_activeSession.messages);
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

        if (message.chatRole === ChatEnums.ChatRole.Assistant) {

            message.setContent(
                processCitations(message.getContent()));
        }

        return message;
    }

    function normalizeCitation(citation) {

        citation._clientId ??= crypto.randomUUID();

        citation._snippet = citation.chunkText.substring(0, 200);

        return citation;
    }

    function processCitations(content) {

        return ChatTemplates.renderInlineCitationMarker(content);
    }

    function renderMessages(messages) {

        const $list = $("#message-list");

        $list.empty();

        for (const message of messages) {

            if (message.chatRole === ChatEnums.ChatRole.User) {
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
       Input Drawer
       ========================================================== */

    function updateDom_InputDrawer() {

        // On:
        // * _messageContent

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

        const content = _messageContent.trim();

        clearInput();
        setInputEnabled(false);

        if (!_activeSession) {
            try {
                await createNewSession(content);
            } catch (err) {
                console.log(err);
                alert("Failed to create new chat session.", err);
                setInputEnabled(true);
                return;
            }
        }

        const userMessage =
            appendUserMessage(content);

        const assistantMessage =
            appendStreamingAssistantMessage();

        scrollToBottom();

        const cachedLastMessagAt = _activeSession.lastMessageAt;
        setActiveSessionLastMessageAt(Date.now());

        try {
            const genChatRes = await $.ajax({
                url: "/chat?handler=Generate",
                method: "POST",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
                },
                data: {
                    sessionId: _activeSession.id,
                    userMessageClientId: userMessage._clientId,
                    assistantMessageClientId: assistantMessage._clientId,
                    content,
                },
            });

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
        } catch (err) {

            alert("Problem sending chat message.", err);
            // setActiveSessionLastMessageAt(cachedLastMessagAt);
            setInputEnabled(true);
            return;
        }
    }

    async function createNewSession(messageContent) {

        const subjectId = _dropdownSelectedSubjectId;

        const response =
            await $.ajax({
                url: "/chat?handler=CreateSession",
                method: "POST",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                },
                data: {
                    subjectId,
                    messageContent,
                },
            });

        const conTkn = _concurrencyToken = crypto.randomUUID();

        await loadSessionList(conTkn);
        await loadSession(conTkn, response.sessionId);
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

        _activeSession.messages.push(message);

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

        _activeSession.messages.push(message);

        $("#message-list")
            .append(
                ChatTemplates.renderAssistantMessage(message));

        return message;
    }

    /* ==========================================================
       SignalR Handlers
       ========================================================== */

    async function onResourceChanged(resUpd) {

        switch (resUpd.resourceType) {
            case ResourceType.Subject:
                await loadSubjectList(_concurrencyToken = crypto.randomUUID());
                break;
            case ResourceType.Membership:
                if (resUpd.properties["userId"] === Razor.userId)
                    await loadSubjectList(_concurrencyToken = crypto.randomUUID());
                break;
            case ResourceType.ChatSession:
                if (resUpd.properties["userId"] === Razor.userId)
                    await loadSessionList(_concurrencyToken = crypto.randomUUID());
                break;
        }
    }

    async function onExchangeCreated(userMessageId, userMessageClientId, assistantMessageId, assistantMessageClientId) {

        if (!_activeSession?.id)
            return;

        const conTkn = _concurrencyToken = crypto.randomUUID();

        const dto =
            await $.ajax({
                url: `/chat?handler=GetSession&id=${_activeSession.id}`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                },
            });

        if (_concurrencyToken !== conTkn)
            return;

        _activeSession = normalizeSession(dto);

        renderMessages(_activeSession.messages);
    }

    function onGenerationStarted(assistantMessageId, assistantMessageClientId) {

        if (!_activeSession?.id)
            return;

        const message = findMessageByBothIds(assistantMessageId, assistantMessageClientId);
        if (!message)
            return;

        if (message.status !== ChatEnums.MessageStatus.Pending) {
            // TODO: Figure out what this means
            //
            // > Did another client of the same session
            //   initiate retry/regeneration?
            //
            // > Race condition?
            //   Content token arrived before start signal?
            //
            return;
        }

        message.status = ChatEnums.MessageStatus.Generating;
        updateDom_AssistantMessage(message);
    }

    function onReceiveToken(assistantMessageId, assistantMessageClientId, token) {

        if (!_activeSession?.id)
            return;

        const message = findMessageByBothIds(assistantMessageId, assistantMessageClientId);
        if (!message)
            return;

        if (message.status === ChatEnums.MessageStatus.Pending
            || message.status === ChatEnums.MessageStatus.Generating) {

            message.status = ChatEnums.MessageStatus.Streaming;
            updateDom_AssistantMessage(message);
        }

        message.setContent(message.getContent() + token);

        if (token) {
            updateDom_StreamingMessageContent(message);
            scrollToBottomIfNeeded();
        }
    }

    function onGenerationCompleted(assistantMessageId, assistantMessageClientId, chatMessageDto) {

        if (!_activeSession?.id)
            return;

        const message = findMessageByBothIds(assistantMessageId, assistantMessageClientId);
        if (!message) {
            return;
        }

        if (chatMessageDto.id !== message.id) {
            return;
        }

        message.status = ChatEnums.MessageStatus.Completed;

        message.setContent(
            processCitations(chatMessageDto.content));

        message.setCitations(
            (chatMessageDto.citations ?? [])
                .map(normalizeCitation));

        updateDom_AssistantMessage(message);
        scrollToBottom();

        setInputEnabled(true);
    }

    function onGenerationFailed(assistantMessageId, assistantMessageClientId, error) {

        if (!_activeSession?.id)
            return;

        const message = findMessageByBothIds(assistantMessageId, assistantMessageClientId);
        if (!message)
            return;

        message.status = ChatEnums.MessageStatus.Failed;
        message.generationErrors = error;

        updateDom_AssistantMessage(message);
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

        updateDom_AssistantMessage(message);

        if (_activeSourcesClientId === message._clientId) {

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

        updateDom_AssistantMessage(message);

        if (_activeSourcesClientId === message._clientId) {

            openSourcesPanel(message._clientId);
        }
    }

    /* ==========================================================
       Sources Panel
       ========================================================== */

    function openSourcesPanel(msgClientId) {

        _activeSourcesClientId = msgClientId;
        _isSourcesPanelOpen = true;

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
                    ChatTemplates.renderSourceCard(citation));
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

        _isSourcesPanelOpen = false;

        const $panel =
            $("#sources-panel");

        $panel
            .removeClass(
                "w-[420px] opacity-100")
            .addClass(
                "w-0 opacity-0");

        setTimeout(
            () => {

                if (!_isSourcesPanelOpen) {

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

        if (_isProfileMenuOpen) {

            closeProfileMenu();
            return;
        }

        $("#profile-dropdown")
            .removeClass("hidden");

        _isProfileMenuOpen = true;
    }

    function closeProfileMenu() {

        $("#profile-dropdown")
            .addClass("hidden");

        _isProfileMenuOpen = false;
    }

    /* ==========================================================
       Attachment Menu
       ========================================================== */

    function toggleAttachmentMenu() {

        if (_isAttachmentMenuOpen) {

            closeAttachmentMenu();
            return;
        }

        $("#attachment-menu")
            .removeClass("hidden");

        _isAttachmentMenuOpen = true;
    }

    function closeAttachmentMenu() {

        $("#attachment-menu")
            .addClass("hidden");

        _isAttachmentMenuOpen = false;
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

    function clearInput() {
        setMessageContent("");
    }

    function findMessageByBothIds(messageId, msgClientId) {

        if (!_activeSession)
            return null;

        return findMessageById(messageId)
            ?? findMessageByClientId(msgClientId);
    }

    function findMessageById(messageId) {

        if (!_activeSession)
            return null;

        return _activeSession.messages
            .find(x => x.id === messageId);
    }

    function findMessageByClientId(msgClientId) {

        if (!_activeSession) {
            return null;
        }

        return _activeSession.messages
            .find(x => x._clientId === msgClientId);
    }

    function getAntiForgery() {
        return $("input[name='__RequestVerificationToken']").val() ?? "";
    }

    function diffById(oldItems, newItems) {

        const oldIds = new Set(oldItems.map(x => x.id));
        const newIds = new Set(newItems.map(x => x.id));

        return {
            added: newItems.filter(x => !oldIds.has(x.id)),
            removed: oldItems.filter(x => !newIds.has(x.id)),
        };
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
    };


})();

$(async function () {

    await Chat.init();

});
