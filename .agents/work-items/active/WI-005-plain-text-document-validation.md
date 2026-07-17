# WI-005: Plain-Text Document Validation

Last verified: 2026-07-13  
Status: Active

## Goal

Accept legitimate plain-text document uploads without weakening binary-file validation or trusting a `.txt` extension by itself.

## Verified Current State

- The document-library client includes `txt` in its accepted extension list.
- Server-side validation relies on detected extensions and MIME types from content inspection.
- Ordinary plain text has no dependable file signature, so valid `.txt` content may produce no matching detected extension or MIME type and be rejected.
- An extension-only bypass would allow arbitrary binary content renamed to `.txt`.

Implementation anchors:

- `BusinessLayer/Services/Documents/File/DocumentFileValidator.cs`
- document file validation options and registrations
- `PresentationLayer/wwwroot/js/documents/document-library.js`
- existing document reception and validator tests

## Locked Decisions

- This defect is separate from WI-001; the upload queue is complete.
- Do not accept `.txt` solely because the filename extension is allowed.
- Keep the existing signature/MIME path for formats with reliable detection.
- Any text-specific inspection must read only a bounded sample and restore the original stream position.
- Do not buffer an entire uploaded document merely to validate plain text.
- Broad legacy-encoding support is not required unless separately approved.

## Proposed Validation Direction

For `.txt` only:

1. Read a bounded sample, such as the first 64 KiB.
2. Accept an optional UTF-8 BOM.
3. Validate the remaining sample as UTF-8.
4. Reject NUL bytes and binary/control-heavy content while allowing normal whitespace such as tab, carriage return, and line feed.
5. Restore a seekable stream to its original position before returning.
6. Define and test the empty-file policy explicitly.
7. Keep allowed-extension and configured-policy checks in force.

## Remaining Work

1. Inspect the current validator, options, registrations, and real validator test coverage.
2. Lock the exact UTF-8, empty-file, control-character, and sample-size policy.
3. Add focused failing tests for valid UTF-8 text, UTF-8 BOM, empty input, invalid UTF-8, binary data renamed to `.txt`, NUL bytes, and stream-position restoration.
4. Implement the minimal `.txt` inspection path.
5. Run the focused tests and the full solution build/test commands.
6. Reconcile client copy or accepted-extension behavior if server support remains intentionally limited.

## Hazards And Boundaries

- Do not weaken PDF, DOCX, PPTX, HTML, or other signature/MIME validation.
- Do not silently accept arbitrary encodings.
- Do not use OCR for text validation.
- Do not consume or close caller-owned streams.
- Do not broaden this item into parser, chunking, or upload-queue redesign.

## Acceptance Criteria

- Valid UTF-8 `.txt` files pass validation.
- Optional UTF-8 BOM input passes.
- Binary content renamed to `.txt` fails.
- Invalid UTF-8 and NUL-containing content fail under the locked policy.
- Validation reads a bounded sample and restores stream position.
- Existing non-text validation behavior remains unchanged.
- Focused validator tests and the applicable full build/test commands pass.

## Resume Prompt

Inspect the current `DocumentFileValidator` and its real tests. First propose the exact bounded UTF-8 policy and test matrix; do not implement until the policy is approved.
