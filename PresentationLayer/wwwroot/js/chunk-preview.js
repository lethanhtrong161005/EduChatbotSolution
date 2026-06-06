var chunkList;

document.addEventListener(
    "DOMContentLoaded",
    () => {

        let currentChunkPage = 1;

        const chunkList =
            document.getElementById("chunkList");

        const chunkPagination =
            document.getElementById("chunkPagination");

        const documentId =
            document.getElementById("chunk-showcase")
                .dataset.docId;

        loadChunks(documentId);

        async function loadChunks(documentId, pageIndex = 1) {

            const response = await fetch(
                `/documents/chunks?documentId=${documentId}&pageIndex=${pageIndex}`);

            const page =
                await response.json();

            renderChunks(page.chunks);

            renderPagination(
                page.pageIndex,
                page.totalPages);

            currentChunkPage =
                page.pageIndex;
        }

        function renderChunks(chunks) {

            chunkList.innerHTML = "";

            for (const chunk of chunks) {

                const vectorText =
                    buildVectorPreview(chunk.vectorPreview);

                chunkList.insertAdjacentHTML(
                    "beforeend",
                    `
            <div class="chunk-row">

                <div class="chunk-text-panel">

                    <div class="chunk-meta">

                        <span class="chunk-index">
                            Chunk #${chunk.chunkIndex}
                        </span>

                        ${chunk.pageNumber
                        ? `
                            <span class="page-number">
                                Page ${chunk.pageNumber}
                            </span>
                            `
                        : ""}

                        ${chunk.sectionTitle
                        ? `
                            <span class="section-title">
                                ${escapeHtml(chunk.sectionTitle)}
                            </span>
                            `
                        : ""}

                        ${chunk.tokenCount
                        ? `
                            <span class="token-count">
                                ${chunk.tokenCount} tokens
                            </span>
                            `
                        : ""}

                    </div>

                    <pre class="chunk-text">
${escapeHtml(chunk.chunkText)}
                    </pre>

                </div>

                <div class="vector-panel">

                    <div class="vector-header">
                        Embedding Preview
                    </div>

                    <pre class="vector-preview">
${vectorText}
                    </pre>

                    <div class="vector-meta">

                        ${chunk.embeddingModel}
                        ·
                        ${chunk.tokenCount ?? "???"}
                        tokens

                    </div>

                </div>

            </div>
            `);
            }
        }

        function buildVectorPreview(vector) {

            const rows = [];

            for (let i = 0; i < vector.length; i += 4) {

                rows.push(
                    vector
                        .slice(i, i + 4)
                        .map(e => e.toFixed(3).padStart(7))
                        .join(", "));
            }

            if (rows.length > 0) {

                rows[rows.length - 1] += ", ...";
            }

            return (
                "(\n" +
                rows.join("\n") +
                "\n)"
            );
        }

        function renderPagination(
            pageIndex,
            totalPages) {

            chunkPagination.innerHTML = "";

            const side = 2;

            let left = pageIndex - side;
            let right = pageIndex + side;

            if (left < 1) {
                left = 1;
                right = Math.min(totalPages, 5);
            }

            if (right > totalPages) {
                right = totalPages;
                left = Math.max(1, totalPages - 4);
            }

            addPageButton(
                "First",
                1,
                pageIndex === 1);

            addPageButton(
                "Prev",
                pageIndex - 1,
                pageIndex === 1);

            if (left > 1) {

                addPageButton(
                    "1",
                    1,
                    false);

                if (left > 2) {

                    chunkPagination.insertAdjacentHTML(
                        "beforeend",
                        `<span class="px-2">...</span>`);
                }
            }

            for (let i = left; i <= right; i++) {

                addPageButton(
                    i,
                    i,
                    false,
                    i === pageIndex);
            }

            if (right < totalPages) {

                if (right < totalPages - 1) {

                    chunkPagination.insertAdjacentHTML(
                        "beforeend",
                        `<span class="px-2">...</span>`);
                }

                addPageButton(
                    totalPages,
                    totalPages,
                    false);
            }

            addPageButton(
                "Next",
                pageIndex + 1,
                pageIndex === totalPages);

            addPageButton(
                "Last",
                totalPages,
                pageIndex === totalPages);
        }

        function addPageButton(
            text,
            page,
            disabled,
            active = false) {

            const btn =
                document.createElement("button");

            btn.className =
                "chunk-page-btn";

            if (active)
                btn.classList.add("active");

            if (disabled)
                btn.classList.add("disabled");

            btn.textContent =
                text;

            btn.addEventListener(
                "click",
                () => loadChunks(
                    documentId,
                    page));

            chunkPagination.appendChild(btn);
        }

        function escapeHtml(text) {

            const div =
                document.createElement("div");

            div.textContent = text;

            return div.innerHTML;
        }

    });
