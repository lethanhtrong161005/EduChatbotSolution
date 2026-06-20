window.ChatTemplates = (function () {

    function escapeHtml(text) {

        return $("<div>")
            .text(text ?? "")
            .html();
    }

    function renderSidebarSessionList(sessions) {

        return sessions.map(x => `
            <button class="chat-session-item w-full text-left px-4 py-3 cursor-pointer hover:bg-sidebar-hover transition"
                data-session-id="${x.id}">

                <div class="truncate">

                    ${x.title}

                </div>

            </button>
        `);
    }

    function renderNewChat(subjects) {

        return `
        <div class="flex h-full items-center justify-center">

            <div class="w-full max-w-3xl px-8">

                <div class="text-center">

                    <div class="mb-4 text-6xl">
                        🎓
                    </div>

                    <h1 class="text-4xl font-bold">
                        EduChatAI
                    </h1>

                    <p class="mt-4 text-muted">
                        Ask questions about your course materials.
                    </p>

                </div>

                <div class="mt-10">

                    ${renderChatMessageInput(subjects)}

                </div>

            </div>

        </div>
    `;
    }

    function renderChatSession(session) {

        return `
            <div class="flex h-full flex-col mx-auto max-w-[70%]">

                <div
                    id="message-list"
                    class="chat-scrollbar flex-1 overflow-y-auto px-8 py-8">
                </div>

                    <div class="mt-4 mb-6 w-full">

                        ${renderChatMessageInput(null)}

                    </div>

                </div>

            </div>
        `;
    }

    function renderChatMessageInput(subjects) {

        const subjectOptions = subjects
            ?.map(x => `
                <button
                    type="button"
                    class="subject-dropdown-item block w-full px-3 py-2 text-left cursor-pointer hover:bg-surface-hover"
                    data-subject-id="${x.id}">
                        ${escapeHtml(x.code)}-${escapeHtml(x.name)}
                </button>
            `)
            .join("");

        return `
            <div
                id="attachment-drawer"
                class="mb-3 hidden rounded-2xl border border-border bg-card p-3">

                <div
                    id="attachment-list"
                    class="flex flex-wrap gap-2">
                </div>

            </div>

            <div
                class="relative rounded-3xl border border-border bg-input">

                <textarea
                    id="chat-message-input"
                    rows="1"
                    placeholder="Message EduChatAI"
                    class="max-h-[240px] min-h-[52px] w-full resize-none bg-transparent align-middle pl-12 ${subjectOptions ? "pr-72" : "pr-24"} py-3 outline-none"></textarea>

                <button
                    id="btn-attach-file"
                    type="button"
                    class="absolute left-3 top-1/2 -translate-y-1/2 rounded-lg p-2 text-muted cursor-pointer hover:bg-surface-hover">

                    <i class="fa-solid fa-paperclip"></i>

                </button>

                ${subjectOptions
                ? `
                <div
                    class="absolute right-14 top-1/2 -translate-y-1/2">

                    <button
                        id="btn-select-subject"
                        type="button"
                        class="flex items-center gap-2 rounded-lg px-3 py-2 text-sm cursor-pointer hover:bg-surface-hover">

                        <span id="selected-subject-text">
                            All of Your Subjects
                        </span>

                        <i class="fa-solid fa-chevron-down text-xs"></i>

                    </button>

                    <div
                        id="subject-dropdown"
                        class="absolute bottom-full right-0 mb-2 hidden min-w-[260px] overflow-hidden rounded-xl border border-border bg-card shadow-lg">

                        <button
                            class="subject-dropdown-item block w-full px-3 py-2 text-left cursor-pointer hover:bg-surface-hover"
                            data-subject-id="">

                            All of Your Subjects

                        </button>

                        ${subjectOptions}

                    </div>

                </div>
            `
                : ""}

                <button
                    id="btn-send-message"
                    type="button"
                    class="absolute right-3 top-1/2 -translate-y-1/2 rounded-lg bg-brand px-3 py-2 text-white cursor-pointer hover:bg-brand-dark">

                    <i class="fa-solid fa-arrow-up"></i>

                </button>

            </div>
        `;
    }

    function renderUserMessage(message) {

        return `
            <div
                class="mb-8 flex justify-end"
                data-message-id="${message.id ?? ""}"
                data-elem-id="${message.clientId}">

                <div
                    class="max-w-3xl rounded-3xl bg-message-user px-5 py-3 text-white">

                    ${escapeHtml(message.content)}

                </div>

            </div>
        `;
    }

    function renderAssistantMessage(message) {

        const responseCount =
            message.responseCount ?? 1;

        const responseIndex =
            message.responseIndex ?? 0;

        return `
    <article
        class="assistant-message group"
        data-message-id="${message.id}"
        data-elem-id="${message.clientId}">

        <div
            class="prose prose-sm md:prose-base max-w-none
                   prose-headings:text-foreground
                   prose-p:text-foreground
                   prose-strong:text-foreground
                   prose-code:text-foreground
                   prose-pre:bg-surface-alt">

            ${marked.parse(message.content ?? "")}

        </div>

        <div
            class="mt-3 flex items-center gap-1
                   text-muted text-sm">

            ${responseCount > 1
                ? `
                    <div
                        class="ml-2 flex items-center gap-2">

                        <button
                            class="response-prev-btn
                                   cursor-pointer hover:text-foreground"
                            data-client-id="${message.clientId}">

                            <i class="fa-solid fa-chevron-left"></i>

                        </button>

                        <span
                            class="font-medium">

                            ${responseIndex + 1}/${responseCount}

                        </span>

                        <button
                            class="response-next-btn
                                   cursor-pointer hover:text-foreground"
                            data-client-id="${message.clientId}">

                            <i class="fa-solid fa-chevron-right"></i>

                        </button>

                    </div>
                `
                : ""}

            <button
                class="message-copy-btn
                       rounded-md px-2 py-1
                       cursor-pointer hover:bg-surface-hover"
                data-client-id="${message.clientId}">

                <i class="fa-regular fa-copy"></i>
                <span class="ml-1">Copy</span>

            </button>

            <button
                class="message-sources-btn
                       rounded-md px-2 py-1
                       cursor-pointer hover:bg-surface-hover"
                data-client-id="${message.clientId}">

                <i class="fa-solid fa-book"></i>
                <span class="ml-1">Sources</span>

            </button>

            <button
                class="message-regenerate-btn
                       rounded-md px-2 py-1
                       cursor-pointer hover:bg-surface-hover"
                data-client-id="${message.clientId}">

                <i class="fa-solid fa-rotate-right"></i>
                <span class="ml-1">Regenerate</span>

            </button>

        </div>

    </article>
            `;
    }

    function renderStreamingAssistantMessage(message) {

        return `
            <div
                class="assistant-message-group mb-8"
                data-message-id="${message.id ?? ""}"
                data-elem-id="${message.clientId}">

                <div
                    class="assistant-streaming
                           prose prose-sm md:prose-base max-w-none
                           prose-headings:text-foreground
                           prose-p:text-foreground
                           prose-strong:text-foreground
                           prose-code:text-foreground
                           prose-pre:bg-surface-alt">
                </div>

            </div>
        `;
    }

    function renderSourceCard(citation) {

        return `
            < section
        class="source-card
        rounded - xl
               border border - border
        bg - card
        overflow - hidden"
        data - citation - id="${citation.id}"
        data - citation - index="${citation.citationIndex}" >

        <div
            class="p-4 border-b border-border">

            <div
                class="font-medium text-sm">

                [${citation.citationIndex}]
                ${escapeHtml(citation.documentTitle)}

            </div>

            <div
                class="mt-2 flex items-center gap-4 text-sm">

                <button
                    class="citation-details-toggle
                           text-brand cursor-pointer hover:underline">

                    Details

                </button>

                <a
                    href="/documents/details/${citation.documentId}"
                    target="_blank"
                    rel="noopener noreferrer"
                    class="text-brand cursor-pointer hover:underline">

                    Show Document

                </a>

            </div>

        </div>

        <div
            class="citation-details hidden
                   p-4 bg-surface-alt">

            ${citation.locationInDocument
                ? `
                    <div
                        class="text-sm text-muted">

                        ${escapeHtml(
                    citation.locationInDocument)}

                    </div>
                `
                : ""}

            ${citation.contentSnippet
                ? `
                    <div
                        class="mt-3 text-sm whitespace-pre-wrap">

                        ${escapeHtml(
                    citation.contentSnippet)}

                    </div>
                `
                : ""}

        </div>

    </section >
            `;
    }

    function renderAttachmentChip(file) {

        return `
            <div
                class="flex items-center gap-2 rounded-xl border border-border bg-card px-3 py-2"
                data-file-id="${file.id ?? ""}">

                <i class="fa-regular fa-file"></i>

                <span class="max-w-[220px] truncate">

                    ${escapeHtml(file.fileName)}

                </span>

                <button
                    class="attachment-remove text-muted cursor-pointer hover:text-error">

                    <i class="fa-solid fa-xmark"></i>

                </button>

            </div>
        `;
    }

    function renderFileLibraryRow(file) {

        return `
            <button
                type="button"
                class="file-library-row flex w-full items-center justify-between rounded-xl px-4 py-3 text-left cursor-pointer hover:bg-surface-hover"
                data-file-id="${file.id}">

                <div>

                    <div class="font-medium">

                        ${escapeHtml(file.title)}

                    </div>

                    <div class="text-sm text-muted">

                        ${escapeHtml(file.subjectName ?? "")}

                    </div>

                </div>

            </button>
        `;
    }

    return {
        renderSidebarSessionList,
        renderNewChat,
        renderChatSession,
        renderUserMessage,
        renderAssistantMessage,
        renderStreamingAssistantMessage,
        renderSourceCard,
        renderAttachmentChip,
        renderFileLibraryRow
    };

})();
