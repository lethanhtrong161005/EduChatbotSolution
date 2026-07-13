# Teammate C (Trọng) — Trang Report+Stats; Trang experiment result + Trang compare; Bộ câu hỏi benchmark

## Mục tiêu

- Làm dashboard xem thống kê báo cáo.
- Làm trang kết quả experiment.
- Làm trang so sánh 2 experiment.

## Tính năng và luồng giao diện

Trang báo cáo:

- Khung thời gian (timeframe): 7 ngày, 30 ngày, all time.
- KPIs:
    + Active users (có login trong timeframe)
    + Active sessions (có message trong timeframe)
    + Completed assistant message generations
    + Measured tokens with coverage
    + Generation success rate
    + Citation coverage
    + p95 response time.

- Trends -- Biểu đồ đường (line chart) (all subject default; fitler theo subject):
    + Completed assistant generations per day.
    + Prompt and completion tokens per day, separate series.
    + p95 total response time per day.

- So sánh subject -- Biểu đồ thanh (bar chart); Có nút đổi thông số:
    + Active user count
    + Active session count
    + Completed assistant message generations
    + Measured prompt tokens
    + Measured completion tokens
    + Measured total tokens
    + Measured message count
    + Message count
    + Coverag percentage (measuredMsg / msg)

- Subject Table: All subjects, hiển thị tất cả metrics trên cùng lúc.

- Bảng Hot documents: Top N document có nhiều citation nhất trong timeframe.

- Bảng Health: Top N môn generate fail hay 0 chunk nhiều.

- Biểu đồ vòng (Donut): Tỉ lệ document processing VS indexed vs failed

Trang experiment results:

- Hiển thị tất cả experiment trong 1 bảng.
- Xem status, config, total score và kết quả từng câu.
- Chọn 2 experiment để so sánh → Sang trang so sánh

Trang experiment result comparison:
- Hiển thị 4 điểm RAGAS từng môn + Chênh lệch

## Phạm vi file và class

Được tạo hoặc sửa:

- BusinessLayer/Services/AI/Experiments/Data/db201-vi-50.json
- PresentationLayer/Pages/Admin/Reports.cshtml
- PresentationLayer/Pages/Admin/Reports.cshtml.cs
- PresentationLayer/Pages/Admin/Experiments/Results.cshtml
- PresentationLayer/Pages/Admin/Experiments/Results.cshtml.cs
- PresentationLayer/Pages/Admin/Experiments/Compare.cshtml
- PresentationLayer/Pages/Admin/Experiments/Compare.cshtml.cs
- PresentationLayer/wwwroot/js/admin/admin-reports.js
- PresentationLayer/wwwroot/js/admin/admin-experiment-results.js
- PresentationLayer/wwwroot/js/admin/admin-experiment-compare.js
- PresentationLayer/wwwroot/css/admin-reports.css
- PresentationLayer/wwwroot/css/admin-experiment-results.css
- UnitTests/AdminReportsPageTests.cs
- UnitTests/AdminExperimentResultsPageTests.cs
- UnitTests/VietnameseExperimentDatasetTests.cs

## Nên đọc qua

- SubjectManageModel và UserManageModel
- Razor Page code-behind ReportsModel, ResultsModel và CompareModel
- AdminReportDashboardDto: thông số cần vẽ.
- ExperimentSummaryDto, ExperimentResultDto và ExperimentComparisonDto: dữ liệu kết quả experiment.
- TestQuestion: cấu trúc câu hỏi và câu trả lời chuẩn.
- admin-subject-manage.js: mẫu front-end.

## Tiêu chí hoàn thành

- Trang reports đầy đủ các bảng.
- Trang experiment results.
- Trang experiment result compare.

## Không

- Làm câu hỏi cho môn khác ngoài DB201 (Database Systems)
- Sửa service. Từ page handler trở ra UI thoải mái.
