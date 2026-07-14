
# CHUNG

1. Admin layout chưa có các trang mới.

2. Lệch màu (trang máy mình). Nền sáng, card tối, chữ tối => Khó đọc. Mấy trang admin khác light-mode, trang này vẫn nên vậy, nhưng để card sang màu sáng hơn nha.

3. Thiếu concurrency check trong AJAX request. 1 vấn đề muôn thuở của front-end là:
    - User nhấn item A.
    - Gửi request cho item A.
    - User nhấn item B trước khi A hiển thị.
    - Gửi request cho item B.
    - Response item B về, vẽ UI.
    - Response item A về trễ (vì đường truyền), vẽ A.
    - Kết cục là UI hiển thị data của item A sai ý muốn.
    
    Cách xử lý đơn giản: Global `_currentRequestId`. Trước khi gửi `const reqId = _currentRequestId++;`. Nhận kết quả xong check `_currentRequestId === reqId`, false thì return.

# REPORTS

1. `admin-reports.js` không thấy class Chart. Javascript crash.

# EXPERIMENTS/RESULTS

2. Bảng Experiment Detail flatten progress khi vừa bật `openDetail()`, nhưng quên trong khi đang poll.
    ```
    norm.summary.completedQ = s.completedQuestionCount;
    norm.summary.totalQ = s.totalQuestionCount;
    norm.summary.indexedDocs = s.indexedDocumentCount;
    norm.summary.affectedDocs = s.affectedDocumentCount;
    ```

    Sửa: Dời khối code đó vào `normalizeSummary()`

        function normalizeSummary(s) {
            return {
                id: s.experimentId,
                // ...
                completedQ: s.completedQuestionCount,
                totalQ: s.totalQuestionCount,
                indexedDocs: s.indexedDocumentCount,
                affectedDocs: s.affectedDocumentCount,
            };
        }

3. `renderExperimentDetail()` gọi `renderAggregateScores` khi experiment đã Complete, xóa "hidden" cho score row. Nhưng sau đó khi render experiment khác không set "hidden" trở lại. Kết quả là render experiment đang Active vẫn thấy score row.

4. `OnGetDashboardAsync()` phải check subjectId, trả 404. Mình khuyên là catch `EntityNotFoundException` và `EntityConstraintException` cho mọi handler. Service sẽ xác định cho.

# EXPERIMENTS/COMPARE

1. Page handler `OnGetComparisonAsync()` không xử lý luồng 409. Catch `EntityNotFoundException` (404) và `EntityConstraintException` (409) nha.

2. Nút "Select All" của table trên UI không hoạt động. Và hiện tại cũng chưa có lý do để dùng. Compare chỉ được chọn 2. Chưa có delete hay re-run gì cả. Xóa hay disable luôn.

3. `renderIndexingDonut()` update dataset nhưng không update label.
    ```
    label: ctx => {
        const v = ctx.parsed;
        const pct = total > 0 ? ((v / total) * 100).toFixed(1) : '0.0';
        return `${ctx.label}: ${v} (${pct}%)`;
    },
    ```

    Cái `total` này capture từ lúc `new Chart()`, khi update

        chartIndexingDonut.data.datasets[0].data = values;

    `total` không được update theo.
