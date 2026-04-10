ỨNG DỤNG THUYẾT MINH PHỐ ẨM THỰC VĨNH KHÁNH

Tổng quan
Tên dự án : Ứng dụng thuyết minh phố ẩm thực
Trạng thái : Đang trong quá trình thực hiện
Thành viên tham gia: Nguyễn Tấn Phát (3123411219)
  	  Nguyễn Huỳnh Hoàng Vũ (3123411346)

[Ứng dụng thuyết minh phố ẩm thực Vĩnh Khánh] - Product Requirement Document (PRD)
1. Tổng quan dự án (Executive Summary)
1.1. Bối cảnh (Background)
Trong kỷ nguyên số, khách du lịch đang đối mặt với "nghịch lý của sự lựa chọn" do tình trạng quá tải thông tin. Mặc dù có vô vàn gợi ý từ các nền tảng mạng xã hội (Facebook, TikTok) và công cụ tìm kiếm, nhưng dữ liệu này thường rời rạc, thiếu tính hệ thống và độ tin cậy không đồng nhất.
Bên cạnh đó, các ứng dụng bản đồ hiện nay (như Google Maps) chủ yếu tối ưu cho điều hướng giao thông thuần túy, chưa thực sự chú trọng đến trải nghiệm khám phá chuyên sâu và gặp hạn chế lớn về khả năng định vị thực tế tại các khu vực có kết nối mạng kém hoặc đối với du khách quốc tế chưa có SIM nội địa.

1.2. Mục tiêu dự án (Project Goals)
Dự án được thực hiện nhằm xây dựng một ứng dụng hướng dẫn du lịch thông minh dựa trên bản đồ, hướng tới 3 mục tiêu chính:
Hệ thống hóa lộ trình: Tổng hợp và cung cấp các lộ trình khám phá chuyên sâu, đáng tin cậy thay vì những gợi ý rời rạc.
Tối ưu hóa hiển thị: Cung cấp giao diện bản đồ trực quan, tập trung vào các điểm chạm du lịch và hỗ trợ định vị thực tế chính xác.
Xóa bỏ rào cản kết nối: Đảm bảo trải nghiệm liền mạch thông qua khả năng hoạt động ngoại tuyến (Offline Mode), giúp người dùng tra cứu thông tin mọi lúc mọi nơi.

1.3. Giá trị cốt lõi (Value Propositions)
Khám phá liền mạch: Loại bỏ sự ngắt quãng trong việc lên kế hoạch và di chuyển.
Tin cậy & Chuyên sâu: Thông tin được chọn lọc, giúp người dùng tiết kiệm thời gian sàng lọc dữ liệu từ mạng xã hội.
Tính sẵn sàng cao: Hoạt động ổn định ngay cả trong điều kiện vùng sâu vùng xa hoặc không có kết nối internet.

1.4. Đối tượng mục tiêu (Target Audience)
Khách du lịch tự túc (Solo Travelers): Những người cần lộ trình chi tiết và sự chủ động.
Du khách quốc tế: Những người gặp khó khăn về ngôn ngữ và kết nối mạng khi vừa đặt chân đến địa phương.
Người yêu thích khám phá (Explorers): Những người muốn tìm kiếm các địa điểm chuyên sâu thay vì chỉ các điểm đến phổ thông trên bản đồ giao thông.

2. Yêu cầu chức năng (Functional Requirements)
2.1. Bản đồ ẩm thực tương tác
Hiển thị danh sách các quán ăn theo danh mục (Ốc, đồ nướng, lẩu, tráng miệng).
Định vị GPS người dùng trên phố Vĩnh Khánh.
Chế độ Bản đồ Ngoại tuyến: Tải trước dữ liệu phố Vĩnh Khánh để tra cứu không cần 4G.

2.2. Thuyết minh đa phương tiện
Audio Guide: Tự động phát âm thanh giới thiệu về lịch sử phố Vĩnh Khánh hoặc đặc điểm một quán ăn khi người dùng đến gần.
Hình ảnh & Thực đơn: Hiển thị menu chi tiết và giá cả tham khảo.

2.3. Lộ trình gợi ý (Itineraries)
Gợi ý tour "Càn quét phố ốc" hoặc "Ăn vặt vỉa hè" tùy theo ngân sách và thời gian của người dùng.

3. Kiến trúc hệ thống & Công nghệ (Technical Stack)
Framework: .NET MAUI (Cross-platform cho Android & iOS), ASP .NET cho web.
Database (Local): SQLite-net để lưu trữ thông tin quán ăn, lịch sử và gói dữ liệu offline.
Database: MySQL.
Map Engine: Leaflet.
Media: Đọc script trong thuộc tính POI.
API: RESTful API viết bằng ASP.NET Core để cập nhật dữ liệu hàng quán mới.





