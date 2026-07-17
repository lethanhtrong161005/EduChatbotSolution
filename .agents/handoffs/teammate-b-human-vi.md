# Bàn giao cho Teammate B — Cấu hình AI, tạo experiment và bộ câu hỏi tiếng Việt

Ngày xác minh: 2026-07-13

## Mục tiêu chung

Chuẩn bị bộ 50 câu hỏi tiếng Việt cho DB201 và xây dựng hai màn hình Admin: cấu hình AI theo môn và tạo experiment mới.

## Tính năng và luồng giao diện

Bộ câu hỏi:

- Có đúng 50 câu hỏi và 50 câu trả lời chuẩn bằng tiếng Việt.
- Nội dung dựa trên tài liệu demo Database Systems cuối cùng.
- Mã chạy liên tục từ DB201-VI-001 đến DB201-VI-050.

Trang cấu hình AI:

- Chọn một môn học.
- Chia cấu hình thành bốn nhóm: Indexing, Retrieval, Generation và Prompts.
- Cho người dùng thấy giá trị mặc định, giá trị riêng của môn học và giá trị đang được dùng.
- Lưu thay đổi hoặc đưa một mục về giá trị mặc định.
- Có nút reindex, hộp xác nhận và trạng thái đang xử lý.

Trang tạo experiment:

- Chọn môn DB201.
- Chọn chiến lược chia đoạn, model và thông số tìm kiếm.
- Chọn bộ câu hỏi; hỗ trợ chạy nhanh 3 câu hoặc chạy đủ 50 câu.
- Kiểm tra index hiện tại có phù hợp hay không.
- Nếu cần index lại, hiển thị số tài liệu bị ảnh hưởng và yêu cầu xác nhận.
- Cảnh báo: “Hệ thống sẽ index lại [N] tài liệu của DB201 trước khi chạy. Trong lúc này, chat DB201 tạm dừng. Sau khi hoàn tất, cấu hình mới sẽ được DB201 sử dụng.”
- Sau khi tạo, mở ngay trang kết quả để xem tiến độ hoặc kết quả.

## Phạm vi file và class

Được tạo hoặc sửa:

- BusinessLayer/Services/AI/Experiments/Data/db201-vi-50.json
- PresentationLayer/Pages/Admin/AiConfiguration.cshtml
- PresentationLayer/Pages/Admin/AiConfiguration.cshtml.cs
- PresentationLayer/Pages/Admin/Experiments/Create.cshtml
- PresentationLayer/Pages/Admin/Experiments/Create.cshtml.cs
- PresentationLayer/wwwroot/js/admin/admin-ai-configuration.js
- PresentationLayer/wwwroot/js/admin/admin-experiment-create.js
- PresentationLayer/wwwroot/css/admin-ai-configuration.css
- PresentationLayer/wwwroot/css/admin-experiment-create.css
- UnitTests/AdminAiConfigurationPageTests.cs
- UnitTests/AdminExperimentCreatePageTests.cs
- UnitTests/VietnameseExperimentDatasetTests.cs

Không sửa entity, migration, DbContext, AI services, report pages, chat, Business.csproj, Program.cs hoặc _AdminLayout.cshtml. Owner sẽ nối menu, backend và import bộ câu hỏi.

## Nên đọc class nào

- SubjectManageModel: mẫu viết Admin Razor Page và xử lý request.
- AdminSubjectListVm: cách trang Admin tổ chức dữ liệu.
- AiConfigurationResolver: chỉ đọc để hiểu giá trị mặc định và giá trị riêng.
- SubjectAiConfiguration và GlobalAiConfiguration: chỉ đọc tên các mục cấu hình.
- TestQuestion: cấu trúc câu hỏi và câu trả lời chuẩn.
- CreateModel và AiConfigurationModel do owner cung cấp: dùng đúng method và dữ liệu sẵn có.
- admin-subject-manage.js: mẫu gọi API và cập nhật giao diện Admin.

## Tiêu chí hoàn thành

- Mở trang khi không phải Admin → bị từ chối.
- Dataset có đúng 50 ID khác nhau → test đạt.
- Có câu hỏi hoặc câu trả lời trống, sai ngôn ngữ hoặc ngoài tài liệu DB201 → test thất bại.
- Chọn DB201 → các giá trị cấu hình xuất hiện.
- Đặt một mục về mặc định → giao diện hiển thị đúng giá trị đang dùng.
- Nhập giá trị sai → form báo lỗi ngắn gọn.
- Bấm Save hai lần nhanh → chỉ gửi một lần.
- Bấm Reindex → có xác nhận và trạng thái chờ.
- Index phù hợp → mở trang kết quả và bắt đầu chạy 3 câu.
- Cần index lại → hiện đúng số tài liệu; hoàn tất thì tự chạy 3 câu.
- Tạo experiment 50 câu → gửi đủ 50 câu hỏi.
- Build và các test trang mới đều chạy thành công.

## Điều cần tránh

- Không tự viết backend AI hoặc dữ liệu giả trong service thật.
- Không viết câu hỏi ngoài tài liệu demo DB201 cuối cùng.
- Không sửa Business.csproj hoặc cách import/seed dataset.
- Không thêm semantic chunking.
- Không đổi tên field, model value hoặc đường dẫn API.
- Không sửa menu Admin dùng chung.
- Không dùng từ ngữ cho rằng Python RAGAS đã được chạy.
- Không hứa thời gian index xong.
- Không chạy experiment khi bước kiểm tra báo đang bị chặn.
- Không ghi đè thay đổi không liên quan.
