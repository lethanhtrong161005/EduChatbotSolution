# Bàn giao cho Teammate A — Chat và các câu trả lời thay thế

Ngày xác minh: 2026-07-13

## Mục tiêu chung

Hoàn thiện các thao tác quan trọng của trang chat: xóa cuộc trò chuyện, thử lại câu trả lời bị lỗi, tạo câu trả lời khác và chọn câu trả lời sẽ được dùng cho các tin nhắn tiếp theo.

Phần nền tảng chat của owner đã hoàn tất: tạo cặp tin nhắn an toàn và lịch sử chat đã dùng `MessageIndex`.

## Tính năng và luồng giao diện

- Người dùng có thể xóa cuộc trò chuyện của mình.
- Câu trả lời bị lỗi có nút “Thử lại”.
- Câu trả lời hoàn tất có nút “Tạo câu trả lời khác”.
- Người dùng có thể xem câu trả lời trước hoặc sau.
- Khi đang xem một câu trả lời khác, người dùng có thể chọn “Dùng câu trả lời này”.
- Hai tab đang mở cùng một cuộc trò chuyện phải cập nhật đồng bộ.
- Trong lúc hệ thống đang tạo câu trả lời, các nút không được gửi lặp nhiều lần.

## Phạm vi file và class

Được sửa:

- PresentationLayer/Pages/Chat/Index.cshtml
- PresentationLayer/Pages/Chat/Index.cshtml.cs
- PresentationLayer/Realtime/AiChatHub.cs
- PresentationLayer/wwwroot/js/chat/chat.js
- PresentationLayer/wwwroot/js/chat/chat-signalr.js
- PresentationLayer/wwwroot/js/chat/chat-templates.js
- UnitTests/ChatPageModelTests.cs

Không sửa entity, migration, dịch vụ lưu chat, cấu hình AI, tìm kiếm tài liệu, báo cáo hoặc experiment. Nếu class hoặc method cần dùng chưa có, dừng và báo owner.

## Nên đọc class nào

- IndexModel: cách trang chat nhận request và kiểm tra người dùng.
- ChatGenerationCoordinator: đọc để hiểu luồng tạo câu trả lời; không cần sửa cách sắp xếp lịch sử.
- AiChatHub và IAiChatClient: cách hai tab nhận cập nhật.
- ChatPersistenceService: chỉ đọc để hiểu dữ liệu; không sửa.
- chat.js, chat-signalr.js và chat-templates.js: cách trang lưu trạng thái và vẽ tin nhắn.

## Tiêu chí hoàn thành

- Xóa cuộc trò chuyện của mình → cuộc trò chuyện biến mất.
- Xóa cuộc trò chuyện của người khác → bị từ chối.
- Thử lại câu trả lời lỗi → dùng lại đúng câu trả lời cũ.
- Tạo câu trả lời khác → câu cũ vẫn còn.
- Bấm trước/sau → chỉ đổi nội dung đang xem.
- Chọn một câu trả lời → tin nhắn tiếp theo dùng câu đó.
- Mở hai tab → cả hai tab cập nhật giống nhau.
- Build và các test chat mới đều chạy thành công.

## Điều cần tránh

- Không thay đổi cách tìm tài liệu hoặc tạo prompt.
- Không tạo câu trả lời mới khi chỉ đang thử lại lỗi.
- Không dùng thời gian gửi để sắp xếp thứ tự hội thoại.
- Không tải toàn bộ nội dung của mọi câu trả lời nếu chưa cần.
- Không sửa tên field hoặc đường dẫn API đã được owner cung cấp.
- Không ghi đè thay đổi không liên quan.
