window.ChatTemplates = (function () {

    function escapeHtml(text) {
        return $("<div>").text(text ?? "").html();
    }

    function renderNewChat(subjects) {

        const subjectOptions = subjects
            .map(x => `
                <option value="${x.id}">
                    ${escapeHtml(x.code)} - ${escapeHtml(x.name)}
                </option>
            `)
            .join("");

        return `
            <div class="h-full flex items-center justify-center">

                <div class="w-full max-w-3xl px-8">

                    <div class="text-center mb-10">

                        <div class="text-5xl mb-4">
                            🎓
                        </div>

                        <h1 class="text-3xl font-bold">
                            EduChatAI
                        </h1>

                        <p class="mt-3 text-muted">
                            Ask questions about your course materials.
                        </p>

                    </div>

                    <div class="space-y-3">

                        <textarea
                            id="new-chat-message"
                            rows="4"
                            class="w-full rounded-xl border border-border bg-input p-4"
                            placeholder="Ask anything..."></textarea>

                        <div class="flex gap-3">

                            <select
                                id="new-chat-subject"
                                class="rounded-lg border border-border px-3 py-2">

                                <option value="">
                                    All My Subjects
                                </option>

                                ${subjectOptions}

                            </select>

                            <button
                                id="btn-send-first-message"
                                class="rounded-lg bg-brand px-5 py-2 text-white">

                                Send

                            </button>

                        </div>

                    </div>

                </div>

            </div>
        `;
    }

    function renderChatSession(session) {

        return `
            <div class="flex flex-col h-full">

                <div
                    id="message-list"
                    class="flex-1 overflow-y-auto px-6 py-6 space-y-6">
                </div>

                <div class="border-t border-border p-4">

                    <div class="max-w-4xl mx-auto">

                        <textarea
                            id="chat-message-input"
                            rows="3"
                            class="w-full rounded-xl border border-border p-4"
                            placeholder="Message EduChatAI"></textarea>

                    </div>

                </div>

            </div>
        `;
    }

    function renderUserMessage(message) {

        return `
            <div class="flex justify-end">

                <div
                    class="max-w-3xl rounded-2xl bg-message-user text-white px-4 py-3">

                    ${escapeHtml(message.content)}

                </div>

            </div>
        `;
    }

    function renderAssistantMessage(message) {

        const citations = (message.citations ?? [])
            .sort((a, b) => a.citationIndex - b.citationIndex)
            .map(renderCitation)
            .join("");

        return `
            <div class="flex justify-start">

                <div class="max-w-4xl">

                    <div
                        class="rounded-2xl bg-message-assistant px-4 py-3 prose max-w-none">

                        ${marked.parse(message.content || "")}

                    </div>

                    ${citations.length > 0
                ? `<div class="mt-4 space-y-2">${citations}</div>`
                : ""
            }

                </div>

            </div>
        `;
    }

    function renderCitation(citation) {

        const location =
            citation.locationInDocument
                ? `
                    <div class="text-xs text-muted mt-1">
                        ${escapeHtml(citation.locationInDocument)}
                    </div>
                  `
                : "";

        return `
            <div
                class="rounded-lg border border-border p-3 bg-card">

                <div class="font-medium">

                    [${citation.citationIndex}]
                    ${escapeHtml(citation.documentTitle)}

                </div>

                ${location}

            </div>
        `;
    }

    return {
        renderNewChat,
        renderChatSession,
        renderUserMessage,
        renderAssistantMessage
    };

})();
