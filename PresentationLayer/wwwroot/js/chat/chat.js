"use strict"

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

        updateUi_SidebarSessionList();
    }

    /* ==========================================================
       State Mutation -- Input Bar
       ========================================================== */

    function setMessageContent(val) {

        _messageContent = val ?? "";

        $("#chat-message-input").val(_messageContent);

        updateUi_InputDrawer();
        updateUi_SendButton();
    }

    function setInputEnabled(val) {

        _inputEnabled = val;

        updateUi_InputBar();
        updateUi_SendButton();
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
            updateUi_SubjectSelectList();
        }
        else { // Existing session
            const subjectId = _activeSession.subjectId;
            if (subjectId === undefined)
                return;

            if (subjectId === null) { // All accessible subjects
                _scopeSubjectHeaders = [..._subjectHeaders];
            }
            else {
                const subjectHeader = _subjectHeaders.find(
                    x => x.id === subjectId);

                if (!subjectHeader) {
                    _scopeSubjectHeaders = [];
                }
                else {
                    _scopeSubjectHeaders = [subjectHeader];
                }
            }
        }

        updateUi_SidebarSubjectList();
    }

    /* ==========================================================
       Observers -- UI
       ========================================================== */

    function updateUi_InputBar() {

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

    function updateUi_SendButton() {

        // On:
        // * _messageContent
        // * _inputEnabled

        const canSend = getCanSend();

        $("#btn-send-message")
            .prop("disabled", !canSend)
            .toggleClass("bg-brand", canSend)
            .toggleClass("hover:bg-brand-dark", canSend)
            .toggleClass("cursor-pointer", canSend)
            .toggleClass("bg-background-disabled", !canSend)
            .toggleClass("opacity-50", !canSend)
            .toggleClass("cursor-not-allowed", !canSend);
    }

    function updateUi_SidebarSubjectList() {

        // On:
        // * _scopeSubjectHeaders

        _scopeSubjectHeaders.sort((a, b) =>
            a.code - b.code);

        $("#subject-list").html(
            ChatTemplates.renderSidebarSubjectList(_scopeSubjectHeaders));
    }

    function updateUi_SidebarSessionList() {

        // On:
        // * _sessionHeaders[*].*

        _sessionHeaders.sort((a, b) =>
            b.lastMessageAt - a.lastMessageAt);

        const $list = $("#session-list");

        $("#session-list").html(
            ChatTemplates.renderSidebarSessionList(_sessionHeaders));

        updateUi_ActiveSessionHighlight();
    }

    function updateUi_ActiveSessionHighlight() {

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

    function updateUi_SubjectSelectList() {

        // On:
        // * _scopeSubjectHeaders

        _subjectHeaders.sort((a, b) =>
            a.code - b.code);

        $("#chat-message-input-container").html(
            ChatTemplates.renderChatMessageInput(_subjectHeaders));

        setMessageContent(_messageContent);
    }

    function updateUi_AssistantMessage(message) {

        // On:
        // * _activeSession.messages[#].*

        const $container =
            $(`[data-client-id='${message._clientId}']`);

        if ($container.length === 0)
            return;

        $container.replaceWith(
            ChatTemplates.renderAssistantMessage(message));
    }

    const StreamingMessageReRenderIntervalMs = 15;

    function updateUi_StreamingMessageContent(message) {

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

        $(document).on(
            "click",
            ".btn-delete-session",
            async function (e) {
                e.stopPropagation();
                const sessionId = $(this).data("session-id");
                if (confirm("Are you sure you want to delete this chat session?")) {
                    await deleteSession(sessionId);
                }
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

                if (e.key === "Enter" && !e.shiftKey) {
                    e.preventDefault();
                    sendCurrentMessage();
                }
            });

        /* Copy */

        $(document).on(
            "click",
            ".message-copy-btn",
            async function () {
                const msgClientId = $(this).data("client-id");
                await copyMessage(msgClientId);
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

        /* Retry */

        $(document).on(
            "click",
            ".message-retry-btn",
            function () {
                const msgClientId = $(this).data("client-id");
                retryMessage(msgClientId);
            });

        /* Select Response */

        $(document).on(
            "click",
            ".message-select-btn",
            function () {
                const msgClientId = $(this).data("client-id");
                selectVariant(msgClientId);
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

        $(document).on(
            "chat:variantCreated",
            function (_, userMessageId, assistantMessageId, assistantMessageClientId, variantNavigation) {

                onAssistantVariantCreated(userMessageId, assistantMessageId, assistantMessageClientId, variantNavigation);
            });

        $(document).on(
            "chat:variantSelected",
            function (_, userMessageId, assistantMessageId, variantNavigation) {

                onAssistantVariantSelected(userMessageId, assistantMessageId, variantNavigation);
            });
    }

    /* ==========================================================
       Sidebar
       ========================================================== */

    async function loadSubjectList(conTkn) {

        const oldSubjectHeaders = _subjectHeaders;

        const subjectHeaderDtos =
            await $.ajax({
                url: `/chat?handler=SubjectHeaders`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
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

        subjectHeader.id += "";
        return subjectHeader;
    }

    async function loadSessionList(conTkn) {

        const sessionHeaderDtos =
            await $.ajax({
                url: `/chat?handler=SessionHeaders`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
                },
            });

        if (_concurrencyToken != conTkn)
            return;

        _sessionHeaders = sessionHeaderDtos.map(normalizeSessionHeader);

        updateUi_SidebarSessionList();
    }

    function normalizeSessionHeader(sessionHeader) {

        return sessionHeader;
    }

    /* ==========================================================
       Session Loading
       ========================================================== */

    async function loadSession(
        conTkn,
        sessionId = null,
        pushHistory = true,
        preserveInput = false) {

        if (!sessionId)
            await loadLandingSession(pushHistory);
        else
            await loadExistingSession(conTkn, sessionId, pushHistory);

        if (_concurrencyToken != conTkn)
            return;

        updateState_ScopeSubjectHeaders();
        updateUi_ActiveSessionHighlight();

        if (!preserveInput) {
            clearInput();
        }
        const isGenerating = _activeSession?.messages?.some(m =>
            m.chatRole === ChatEnums.ChatRole.Assistant && (
                m.status === ChatEnums.MessageStatus.Pending ||
                m.status === ChatEnums.MessageStatus.Generating ||
                m.status === ChatEnums.MessageStatus.Streaming
            )
        ) ?? false;

        if (isGenerating) {
            setInputEnabled(false);
        } else {
            setInputEnabled(true);
        }

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
                url: `/chat?handler=Session&id=${sessionId}`,
                method: "GET",
                headers: {
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
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

        /* FIXME:
         * regenerateMessage() optimistically appends a temporary variant, replaces the wrapper’s client ID, and waits for the HTTP result. 
         * 
         * Server sends AssistantVariantCreated to every other connection.
         * Any duplicate browser tab viewing the session immediately changes its local wrapper’s _clientId to the originating tab’s client ID.
         * 
         * _clientId is not truly client-local anymore—it is a generation correlation ID. Rename to clarify.
         */
        message._clientId ??= crypto.randomUUID();

        if (message.variantNavigation?.variants?.length > 0) {
            message._variants = message.variantNavigation.variants.map(v => {
                const isCurrent = v.messageId === message.id;
                return {
                    id: v.messageId,
                    variantIndex: v.variantIndex,
                    isSelected: v.isSelected,
                    status: v.status,
                    content: isCurrent ? (message.content ?? "") : "",
                    citations: isCurrent ? (message.citations ?? []) : [],
                    generationErrors: isCurrent ? (message.generationErrors ?? null) : null,
                    sentAt: isCurrent ? (message.sentAt ?? null) : null,
                    _loaded: isCurrent
                };
            });
            message._activeVariant = message.variantNavigation.variants.findIndex(v => v.messageId === message.id);
            if (message._activeVariant === -1) {
                message._activeVariant = 0;
            }
        } else {
            message._variants = [
                {
                    id: message.id,
                    variantIndex: message.variantIndex ?? 1,
                    isSelected: message.isSelectedVariant ?? true,
                    status: message.status,
                    content: message.content ?? "",
                    citations: message.citations ?? [],
                    generationErrors: message.generationErrors ?? null,
                    sentAt: message.sentAt ?? null,
                    _loaded: true
                }
            ];
            message._activeVariant = 0;
        }

        const variantProps = [
            "id",
            "content",
            "citations",
            "status",
            "generationErrors",
            "sentAt"
        ];

        variantProps.forEach(prop => {
            delete message[prop];
            Object.defineProperty(message, prop, {
                get() {
                    return this._variants[this._activeVariant]?.[prop];
                },
                set(val) {
                    if (this._variants[this._activeVariant]) {
                        this._variants[this._activeVariant][prop] = val;
                    }
                },
                configurable: true,
                enumerable: true
            });
        });

        message.getContent = function () {
            return this.content ?? "";
        };

        message.setContent = function (val) {
            this.content = val;
        };

        message.getCitations = function () {
            return this.citations ?? [];
        };

        message.setCitations = function (val) {
            this.citations = val;
        };

        message._variants.forEach(v => {
            if (v._loaded && v.citations) {
                v.citations = v.citations.map(normalizeCitation);
            }
        });

        if (message.chatRole === ChatEnums.ChatRole.Assistant) {
            message.setContent(
                processCitations(message.getContent()));
        }

        return message;
    }

    function normalizeCitation(citation) {

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

    function updateUi_InputDrawer() {

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
                assistantMessage.sentAt = genChatRes.assistantMessageSentAt;
                assistantMessage.status = genChatRes.assistantMessageStatus
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
                url: "/chat?handler=Session",
                method: "POST",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
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
                if (_subjectHeaders.some(x => x.id === resUpd.resourceId))
                    await loadSubjectList(_concurrencyToken = crypto.randomUUID());
                break;
            case ResourceType.Membership:
                if (resUpd.properties["userId"] === Razor.userId)
                    await loadSubjectList(_concurrencyToken = crypto.randomUUID());
                break;
            case ResourceType.ChatSession:
                if (resUpd.properties["userId"] === Razor.userId) {
                    await loadSessionList(_concurrencyToken = crypto.randomUUID());
                    if (_activeSession?.id === resUpd.resourceId) {
                        if (resUpd.action === ResourceAction.Deleted) {
                            await loadSession(_concurrencyToken = crypto.randomUUID(), null);
                        } else if (resUpd.action === ResourceAction.Updated) {
                            await loadSession(_concurrencyToken = _concurrencyToken, _activeSession.id, false, true);
                        }
                    }
                }
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
                    ChatConnectionId: chatConnectionId,
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

        message.status = ChatEnums.MessageStatus.Generating;
        message.generationErrors = null;
        updateUi_AssistantMessage(message);
        setInputEnabled(false);
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
            updateUi_AssistantMessage(message);
        }

        message.setContent(message.getContent() + token);

        if (token) {
            updateUi_StreamingMessageContent(message);
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

        updateUi_AssistantMessage(message);
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

        updateUi_AssistantMessage(message);
        scrollToBottom();

        setInputEnabled(true);
    }

    function onAssistantVariantCreated(userMessageId, assistantMessageId, assistantMessageClientId, variantNavigation) {

        if (!_activeSession?.id)
            return;

        const message = _activeSession.messages.find(m => m.chatRole === ChatEnums.ChatRole.Assistant && m.inReplyToMessageId === userMessageId);
        if (!message)
            return;

        updateVariantsFromNavigation(message, variantNavigation, assistantMessageId);

        const oldVariantClientId = message._clientId;
        const $oldContainer = $(`[data-client-id='${oldVariantClientId}']`);
        if ($oldContainer.length > 0) {
            $oldContainer.attr("data-client-id", assistantMessageClientId);
        }

        message._clientId = assistantMessageClientId;
        updateUi_AssistantMessage(message);

        const activeVariant = message._variants[message._activeVariant];
        if (activeVariant && (
            activeVariant.status === ChatEnums.MessageStatus.Pending ||
            activeVariant.status === ChatEnums.MessageStatus.Generating ||
            activeVariant.status === ChatEnums.MessageStatus.Streaming
        )) {
            setInputEnabled(false);
        }
    }

    function onAssistantVariantSelected(userMessageId, assistantMessageId, variantNavigation) {

        if (!_activeSession?.id)
            return;

        const message = _activeSession.messages.find(m => m.chatRole === ChatEnums.ChatRole.Assistant && m.inReplyToMessageId === userMessageId);
        if (!message)
            return;

        message.variantNavigation = variantNavigation;

        message._variants.forEach(v => {
            const matchedOption = variantNavigation.variants.find(o => o.messageId === v.id);
            if (matchedOption) {
                v.isSelected = matchedOption.isSelected;
            }
        });

        const selectedIndex = variantNavigation.variants.findIndex(v => v.isSelected);
        if (selectedIndex !== -1) {
            message._activeVariant = selectedIndex;
        }

        updateUi_AssistantMessage(message);
    }

    /* ==========================================================
       Variants
       ========================================================== */

    async function switchVariant(msgClientId, delta) {

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

        const targetVariant = message._variants[nextIndex];
        if (!targetVariant._loaded) {
            try {
                const dto = await $.ajax({
                    url: `/chat?handler=Variant&sessionId=${_activeSession.id}&messageId=${targetVariant.id}`,
                    method: "GET",
                    headers: {
                        CallerConnectionId: callerConnectionId,
                        ChatConnectionId: chatConnectionId,
                    }
                });

                targetVariant.content = dto.content ?? "";
                targetVariant.citations = (dto.citations ?? []).map(normalizeCitation);
                targetVariant.status = dto.status;
                targetVariant._loaded = true;
            } catch (err) {
                console.error("Failed to load variant content", err);
                alert("Failed to load variant.");
                return;
            }
        }

        message._activeVariant = nextIndex;

        updateMessageFromVariant(message);

        updateUi_AssistantMessage(message);

        if (_activeSourcesClientId === message._clientId) {

            openSourcesPanel(message._clientId);
        }

    }

    function updateMessageFromVariant(message) {
        // No-op. Handled dynamically via property proxies on the message wrapper.
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

    function updateVariantsFromNavigation(message, variantNavigation, activeMessageId) {
        message.variantNavigation = variantNavigation;

        const oldVariantsMap = {};
        message._variants.forEach(v => {
            if (v.id) oldVariantsMap[v.id] = v;
        });

        message._variants = variantNavigation.variants.map(v => {
            const isCurrent = v.messageId === activeMessageId;
            const old = oldVariantsMap[v.messageId];
            return {
                id: v.messageId,
                variantIndex: v.variantIndex,
                isSelected: v.isSelected,
                status: v.status,
                content: old ? old.content : "",
                citations: old ? old.citations : [],
                generationErrors: old ? old.generationErrors : null,
                sentAt: old ? old.sentAt : null,
                _loaded: old ? old._loaded : isCurrent
            };
        });

        message._activeVariant = variantNavigation.variants.findIndex(v => v.messageId === activeMessageId);
        if (message._activeVariant === -1) {
            message._activeVariant = 0;
        }

        updateUi_AssistantMessage(message);
    }

    /* ==========================================================
       Regenerate
       ========================================================== */

    async function regenerateMessage(msgClientId) {
        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

        setInputEnabled(false);

        const newVariantClientId = crypto.randomUUID();

        const tempNewVariant = {
            id: null,
            variantIndex: message._variants.length + 1,
            isSelected: true,
            status: ChatEnums.MessageStatus.Pending,
            content: "",
            citations: [],
            generationErrors: null,
            sentAt: null,
            _loaded: true
        };

        message._variants.push(tempNewVariant);
        const oldActiveVariantIndex = message._activeVariant;
        message._activeVariant = message._variants.length - 1;

        updateMessageFromVariant(message);

        const oldVariantClientId = message._clientId;
        const $oldContainer = $(`[data-client-id='${oldVariantClientId}']`);
        if ($oldContainer.length > 0) {
            $oldContainer.attr("data-client-id", newVariantClientId);
        }

        message._clientId = newVariantClientId;

        updateUi_AssistantMessage(message);

        try {
            const response = await $.ajax({
                url: `/chat?handler=Regenerate&sessionId=${_activeSession.id}`,
                method: "POST",
                contentType: "application/json",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
                },
                data: JSON.stringify({
                    messageId: message._variants[oldActiveVariantIndex].id,
                    assistantMessageClientId: newVariantClientId,
                })
            });

            updateVariantsFromNavigation(message, response.variantNavigation, response.assistantMessageId);

            if (_activeSourcesClientId === oldVariantClientId) {
                _activeSourcesClientId = newVariantClientId;
            }
        } catch (err) {
            console.error(err);
            alert("Failed to regenerate message.");
            message._variants.pop();
            message._activeVariant = oldActiveVariantIndex;

            const $newContainer = $(`[data-client-id='${newVariantClientId}']`);
            if ($newContainer.length > 0) {
                $newContainer.attr("data-client-id", oldVariantClientId);
            }
            message._clientId = oldVariantClientId;

            updateMessageFromVariant(message);
            updateUi_AssistantMessage(message);
            setInputEnabled(true);
        }
    }

    /* ==========================================================
       Delete Session, Retry, Select Variant
       ========================================================== */

    async function deleteSession(sessionId) {
        try {
            await $.ajax({
                url: `/chat?handler=Session&sessionId=${sessionId}`,
                method: "DELETE",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
                }
            });
            if (_activeSession?.id === sessionId) {
                await loadSession(_concurrencyToken = crypto.randomUUID(), null);
            }
            await loadSessionList(_concurrencyToken = crypto.randomUUID());
        } catch (err) {
            console.error(err);
            alert("Failed to delete chat session.");
        }
    }

    async function retryMessage(msgClientId) {
        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

        message.status = ChatEnums.MessageStatus.Pending;
        message.generationErrors = null;
        updateUi_AssistantMessage(message);
        setInputEnabled(false);

        try {
            const response = await $.ajax({
                url: `/chat?handler=Retry&sessionId=${_activeSession.id}`,
                method: "POST",
                contentType: "application/json",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
                },
                data: JSON.stringify({
                    messageId: message.id,
                    assistantMessageClientId: message._clientId,
                })
            });

            updateVariantsFromNavigation(message, response.variantNavigation, response.assistantMessageId);
        } catch (err) {
            console.error(err);
            alert("Failed to retry message.");
            message.status = ChatEnums.MessageStatus.Failed;
            message.generationErrors = "Request failed.";
            updateUi_AssistantMessage(message);
            setInputEnabled(true);
        }
    }

    async function selectVariant(msgClientId) {
        const message = findMessageByClientId(msgClientId);
        if (!message)
            return;

        const activeVariant = message._variants[message._activeVariant];
        if (activeVariant.isSelected)
            return;

        try {
            await $.ajax({
                url: `/chat?handler=SelectedVariant&sessionId=${_activeSession.id}`,
                method: "PUT",
                contentType: "application/json",
                headers: {
                    RequestVerificationToken: getAntiForgery(),
                    CallerConnectionId: callerConnectionId,
                    ChatConnectionId: chatConnectionId,
                },
                data: JSON.stringify({
                    messageId: activeVariant.id,
                })
            });

            message._variants.forEach((v, idx) => {
                v.isSelected = (idx === message._activeVariant);
            });

            updateUi_AssistantMessage(message);
        } catch (err) {
            console.error(err);
            alert("Failed to select variant.");
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
            .sort((a, b) => a.citationIndex - b.citationIndex)
            .forEach(citation => {
                $content.append(
                    ChatTemplates.renderSourceCard(citation));
            });

        $panel.removeClass("hidden");

        requestAnimationFrame(() => {

            $panel
                .removeClass("w-0 opacity-0")
                .addClass(
                    "w-[420px] opacity-100");
        });
    }

    function closeSourcesPanel() {

        _isSourcesPanelOpen = false;

        const $panel = $("#sources-panel");

        $panel
            .removeClass("w-[420px] opacity-100")
            .addClass("w-0 opacity-0");

        setTimeout(() => {

            if (!_isSourcesPanelOpen) {
                $panel.addClass("hidden");
            }
        }, 200);
    }

    function flashCitation(citationIndex) {

        const $card = $(`.source-card[data-citation-index='${citationIndex}']`);

        if ($card.length === 0)
            return;

        $card[0]
            .scrollIntoView({
                block: "center",
                behavior: "smooth",
            });

        $card
            .removeClass("source-card-flash")
            .addClass("source-card-flash");
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
