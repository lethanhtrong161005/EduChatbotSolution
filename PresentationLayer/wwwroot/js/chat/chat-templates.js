"use strict"

const ChatTemplates = (function () {

    function escapeHtml(text) {

        return $("<div>")
            .text(text ?? "")
            .html();
    }

    function renderSidebarSubjectList(subjects) {

        return subjects.map(x => `
            <div class="subject-item w-full text-left px-3 py-3"
                 data-subject-id="${x.id}">

                <div class="subject-code font-medium">
                    ${x.code}
                </div>

                <div class="subject-name text-xs text-muted">
                    ${x.name}
                </div>

            </div>
        `);
    }

    function renderSidebarSessionList(sessions) {

        return sessions.map(x => `
            <div class="chat-session-item group relative flex items-center justify-between w-full rounded-xl hover:bg-sidebar-hover transition cursor-pointer"
                 data-session-id="${x.id}">
                <div class="truncate flex-1 pl-4 pr-1 py-3 text-left">
                    ${escapeHtml(x.title)}
                </div>
                <button class="btn-delete-session hidden group-hover:flex items-center justify-center h-8 w-8 rounded-lg text-muted hover:text-error hover:bg-surface-hover transition mr-2 cursor-pointer border-0 bg-transparent outline-none"
                        data-session-id="${x.id}">
                    <i class="fa-regular fa-trash-can text-sm"></i>
                </button>
            </div>
        `);
    }

    function renderLandingSession(subjectHeaders) {

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

                <div
                    id="chat-message-input-container"
                    class="mt-10">

                    ${renderChatMessageInput(subjectHeaders)}

                </div>

            </div>

        </div>
    `;
    }

    function renderExistingSession(session) {

        return `
            <div class="flex h-full flex-col mx-auto max-w-[900px]">

                <div
                    id="message-list"
                    class="chat-scrollbar flex-1 overflow-y-auto px-8 py-8">
                </div>

                    <div
                        id="chat-message-input-container"
                        class="mt-4 mb-6 w-full">

                        ${renderChatMessageInput(null)}

                    </div>

                </div>

            </div>
        `;
    }

    function renderChatMessageInput(subjectHeaders) {

        const subjectOptions = subjectHeaders
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
                class="user-message mb-8 flex justify-end"
                data-message-id="${message.id ?? ""}"
                data-client-id="${message._clientId}">

                <div
                    class="max-w-3xl rounded-3xl bg-message-user px-5 py-3 text-white">

                    ${escapeHtml(message.getContent())}

                </div>

            </div>
        `;
    }

    function renderAssistantMessage(message) {

        message.responseCount = message._variants ? message._variants.length : 1;
        message.responseIndex = message._activeVariant ?? 0;

        return `
    <article
        class="assistant-message mb-8"
        data-message-id="${message.id ?? ""}"
        data-client-id="${message._clientId}">

        <div class="assistant-message-content max-w-none wrap-normal md:wrap-anywhere prose prose-sm md:prose-base prose-assistant">

            ${renderAssistantMessageContent(message)}

        </div>

        ${(message.status === ChatEnums.MessageStatus.Completed || message.status === ChatEnums.MessageStatus.Failed)
                ? renderAssistantMessageUtilityBar(message)
                : ""}

    </article>
            `;
    }

    function renderAssistantMessageContent(message) {

        switch (message.status) {

            case ChatEnums.MessageStatus.Pending:

                return `
                    <div class="assistant-loading flex gap-1 items-center min-h-6">
                        <span class="w-2 h-2 rounded-full bg-muted"></span>
                        <span class="w-2 h-2 rounded-full bg-muted"></span>
                        <span class="w-2 h-2 rounded-full bg-muted"></span>
                    </div>
                `;

            case ChatEnums.MessageStatus.Generating:

                return renderAnimatedText("Reading course materials...");

            case ChatEnums.MessageStatus.Streaming:

                if (!message.getContent().trim()) {
                    return renderAnimatedText("Thinking...");
                }

                return marked.parse(message.getContent() ?? "");

            case ChatEnums.MessageStatus.Completed:

                return marked.parse(message.getContent() ?? "");

            case ChatEnums.MessageStatus.Failed:

                let content;
                return `
                    ${marked.parse((content = message.getContent()) ? content + "\n\n-- -\n\n" : "")}
                    <div class="prose prose-error max-w-none wrap-normal md:wrap-anywhere">
                        \n\n⚠ Generation failed.\n\n
                        ${marked.parse(message.generationErrors ?? "")}
                    </div>
            `;
        }
    }

    function renderAnimatedText(text) {

        return `
            <div class="assistant-working flex flex-wrap items-center min-h-6">
                ${[...text].map((ch, i) => `
                    <span style="animation-delay:${i * 0.05}s">
                        ${ch === " " ? "&nbsp;" : ch}
                    </span>
                `).join("")}
            </div>
        `;
    }

    function renderInlineCitationMarker(content) {

        return content.replaceAll(
            /\[\[(\d+)\]\]/g,
            `<sup class="message-inline-citation" data-citation-index="$1">[$1]</sup>`);
    }

    function renderAssistantMessageUtilityBar(message) {

        if (message.status === ChatEnums.MessageStatus.Failed) {
            return `
                <div class="mt-3 flex items-center gap-1 text-muted text-sm">
                    <button class="message-retry-btn flex items-center gap-1 rounded-md px-2 py-1 cursor-pointer hover:bg-surface-hover text-error border-0 bg-transparent outline-none"
                            data-client-id="${message._clientId}">
                        <i class="fa-solid fa-arrows-rotate"></i>
                        <span class="font-semibold">Retry</span>
                    </button>
                </div>
            `;
        }

        const responseCount = message.responseCount ?? 1;
        const responseIndex = message.responseIndex ?? 0;
        const activeVariant = message._variants ? message._variants[responseIndex] : null;
        const isSelected = activeVariant ? activeVariant.isSelected : true;

        return `
            <div
                class="mt-3 flex items-center gap-1
                       text-muted text-sm">

            ${responseCount > 1
                ? `
                    <div
                        class="ml-2 flex items-center gap-2">

                        <button
                            class="response-prev-btn
                                   cursor-pointer hover:text-foreground border-0 bg-transparent outline-none"
                            data-client-id="${message._clientId}">

                            <i class="fa-solid fa-chevron-left"></i>

                        </button>

                        <span
                            class="font-medium">

                            ${responseIndex + 1}/${responseCount}

                        </span>

                        <button
                            class="response-next-btn
                                   cursor-pointer hover:text-foreground border-0 bg-transparent outline-none"
                            data-client-id="${message._clientId}">

                            <i class="fa-solid fa-chevron-right"></i>

                        </button>

                    </div>
                `
                : ""
            }

            <button
                class="message-copy-btn
                       rounded-md px-2 py-1
                       cursor-pointer hover:bg-surface-hover border-0 bg-transparent outline-none"
                data-client-id="${message._clientId}">

                <i class="fa-regular fa-copy"></i>
                <span class="ml-1">Copy</span>

            </button>

            <button
                class="message-sources-btn
                       rounded-md px-2 py-1
                       cursor-pointer hover:bg-surface-hover border-0 bg-transparent outline-none"
                data-client-id="${message._clientId}">

                <i class="fa-solid fa-book"></i>
                <span class="ml-1">Sources</span>

            </button>

            <button
                class="message-regenerate-btn
                       rounded-md px-2 py-1
                       cursor-pointer hover:bg-surface-hover border-0 bg-transparent outline-none"
                data-client-id="${message._clientId}">

                <i class="fa-solid fa-rotate-right"></i>
                <span class="ml-1">Regenerate</span>

            </button>

            ${(activeVariant && !isSelected)
                ? `
                    <button
                        class="message-select-btn
                               rounded-md px-2 py-1 text-brand font-semibold
                               cursor-pointer hover:bg-surface-hover border-0 bg-transparent outline-none ml-2"
                        data-client-id="${message._clientId}">
                        
                        <i class="fa-solid fa-check"></i>
                        <span class="ml-1">Select Response</span>
        
                    </button>
                `
                : ""
            }

        </div>
            `;
    }

    function renderSourceCard(citation) {

        const trailing =
            citation._snippet.length < citation.chunkText.length
                ? "..."
                : "";

        return `
            <section
                class="source-card rounded-xl border border-border bg-card overflow-hidden"
                data-citation-id="${citation.id}"
                data-citation-index="${citation.citationIndex}">

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

                        ${escapeHtml(citation.locationInDocument)}

                    </div>
                `
                : ""}

            ${citation._snippet
                ? `
                    <div class="mt-3 text-sm whitespace-pre-wrap">${escapeHtml(citation._snippet) + trailing}</div>
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
        renderSidebarSubjectList,
        renderSidebarSessionList,
        renderLandingSession,
        renderExistingSession,
        renderChatMessageInput,
        renderUserMessage,
        renderAssistantMessage,
        renderAssistantMessageContent,
        renderInlineCitationMarker,
        renderSourceCard,
        renderAttachmentChip,
        renderFileLibraryRow
    };

})();
