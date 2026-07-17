# Bàn giao cho Teammate C — Báo cáo và kết quả experiment

Ngày xác minh: 2026-07-13

## Mục tiêu chung

Xây dựng các màn hình Admin để xem báo cáo, theo dõi kết quả experiment và so sánh hai lần chạy.

## Tính năng và luồng giao diện

Trang báo cáo:

- Chọn 7 ngày, 30 ngày hoặc toàn bộ thời gian.
- Ba biểu đồ theo ngày mặc định dùng tất cả môn; có thể chọn một môn.
- Hiển thị người dùng hoạt động, phiên hoạt động, câu trả lời AI, token, tỷ lệ thành công, trích dẫn và tốc độ phản hồi.
- Dùng một biểu đồ thanh và bảng ngắn để so sánh các môn.
- Dùng biểu đồ tròn cho trạng thái index và danh sách tài liệu được trích dẫn nhiều.

Trang experiment:

- Hiển thị tất cả lần chạy trong một bảng.
- Xem trạng thái, cấu hình, điểm tổng và kết quả từng câu.
- Chọn đúng hai lần chạy đã hoàn tất và tương thích để so sánh.
- Hiển thị bốn điểm RAGAS-style bằng biểu đồ dễ đọc.

## Phạm vi file và class

Được tạo hoặc sửa:

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

Không sửa entity, migration, AI services, evaluator, runner, Business.csproj, Program.cs, _AdminLayout.cshtml, chat hoặc các trang của B.

## Nên đọc class nào

- SubjectManageModel và UserManageModel: mẫu viết trang Admin.
- ReportsModel, ResultsModel và CompareModel do owner cung cấp: dùng đúng method và dữ liệu sẵn có.
- AdminReportDashboardDto: các số và danh sách cần vẽ.
- ExperimentSummaryDto, ExperimentResultDto và ExperimentComparisonDto: dữ liệu kết quả cần hiển thị.
- admin-subject-manage.js: mẫu gọi API và cập nhật giao diện.

## Tiêu chí hoàn thành

- Chọn 7/30/all time → biểu đồ đổi đúng dữ liệu.
- Không chọn môn → biểu đồ theo ngày dùng tất cả môn.
- Chọn DB201 → biểu đồ theo ngày chỉ dùng DB201; bảng so sánh môn không đổi.
- Ngày không có hoạt động → vẫn xuất hiện với giá trị 0.
- Token chưa đo được → hiện “Chưa đo được”, không hiện 0.
- Một câu trả lời trích dẫn nhiều đoạn cùng tài liệu → tài liệu chỉ tăng một lượt.
- Mở một experiment hoàn tất → thấy điểm và kết quả từng câu.
- Chọn ít hơn hoặc nhiều hơn hai lần chạy → chưa thể so sánh.
- So sánh hai experiment hợp lệ → thấy bốn điểm và chênh lệch.
- So sánh dữ liệu không hợp lệ → hiện thông báo dễ hiểu.
- Build và các test mới đều chạy thành công.

## Điều cần tránh

- Không biến token chưa đo được thành số 0.
- Không dùng biểu đồ tròn để so sánh người dùng giữa các môn.
- Không để bộ lọc môn của biểu đồ theo ngày làm mất bảng so sánh tất cả môn.
- Không ghi rằng Python hoặc official RAGAS package đã được chạy.
- Không thêm export, drill-down, tự làm mới liên tục hoặc chọn ngày tùy ý.
- Không sửa backend metrics hoặc evaluator.
- Không sửa menu Admin dùng chung.
- Không ghi đè thay đổi không liên quan.
