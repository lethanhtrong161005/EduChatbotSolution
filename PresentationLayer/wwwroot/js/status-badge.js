const StatusNames = Object.freeze({
    "-1": "Failed",
    "0": "Uploaded",
    "1": "Parsing",
    "2": "Parsed",
    "3": "Chunking",
    "4": "Chunked",
    "5": "Embedding",
    "6": "Indexed",
});

const StatusSettings = Object.freeze({
    "Failed": {
        text: "Failed",
        className: "status-failed",
        iconClass: "fa-triangle-exclamation"
    },
    "Uploaded": {
        text: "Uploaded",
        className: "status-uploaded",
        iconClass: "fa-clock"
    },
    "Parsing": {
        text: "Parsing ({{PROGRESS}}%)",
        className: "status-parsing",
        iconClass: "fa-file-lines"
    },
    "Parsed": {
        text: "Parsed",
        className: "status-parsed",
        iconClass: "fa-check"
    },
    "Chunking": {
        text: "Chunking ({{PROGRESS}}%)",
        className: "status-chunking",
        iconClass: "fa-scissors"
    },
    "Chunked": {
        text: "Chunked",
        className: "status-chunked",
        iconClass: "fa-layer-group"
    },
    "Embedding": {
        text: "Embedding ({{PROGRESS}}%)",
        className: "status-embedding",
        iconClass: "fa-brain"
    },
    "Indexed": {
        text: "Indexed",
        className: "status-indexed",
        iconClass: "fa-circle-check"
    },
});
