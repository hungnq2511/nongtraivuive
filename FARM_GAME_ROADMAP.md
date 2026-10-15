# Nông Trại Vui Vẻ – Lộ trình phát triển

## Đã có (v0.5)
- **Trồng trọt:** 11 loại cây (thêm mía). Gieo hạt bằng bảng chọn hạt nhỏ hiện cạnh ô đất; kéo hạt qua nhiều ô để gieo hàng loạt.
- **Vật nuôi:** 6 loài, mỗi loài một chuồng có sân rào; nông dân đứng ở cổng để cho ăn và thu hoạch.
- **Cây ăn quả & tổ ong:** bụi quả mọng, táo, anh đào, cam, chanh; tổ ong cho mật và giúp cây gần đó ra thêm quả.
- **Chế biến:** 8 máy, 24 công thức.
- **Kinh tế:**
  - Kho hàng và Chợ nông sản (quầy bày bán, khách NPC mua).
  - Bảng đơn hàng, xe tải.
  - Kim cương và nút "Xong ngay".
- **Giữ chân người chơi:**
  - Nhiệm vụ hướng dẫn, nhiệm vụ ngày, thành tựu, quà đăng nhập.
  - Bảng "Trong lúc bạn vắng nhà".
- **Thế giới:**
  - Mở rộng đất 7 vùng.
  - Câu cá, ngày–đêm, mưa.
- **Build:** script build iOS lên TestFlight.

## ĐÃ LÀM (v0.6 – B1)
Hiệu ứng thao tác, đếm xu/kim cương, vụ mùa vàng 5% + thành tựu, màn hình lên cấp có pháo giấy, rung (iOS), thông báo cục bộ (iOS, plugin native, không cần package).

## ĐÃ LÀM (v0.7 – B2.1–B2.3, nông trại có sự sống)
- **Khách đến thăm:** 4 khách (Bà Tư, Bé Tí, Cô Mai đầu bếp, Chú Ba thương lái), mở từ cấp 3.
  - Khách đi bộ vào theo con đường, đứng chờ với bong bóng món cần (1–3 món, theo sở thích từng người).
  - Giao hàng: nhận xu x1.8 giá trị, XP, 15% cơ hội +1 kim cương. Từ chối thì khách về.
  - Thân thiết: 3 lần giao lên 1 cấp, mỗi cấp +5% thưởng (tối đa 10 cấp).
  - Số khách cùng lúc: 1 (cấp 3–5), 2 (cấp 6–9), 3 (cấp 10+). Ban đêm không có khách.
- **Thú cưng** (Cửa hàng › Vật nuôi): Chó Vàng (cấp 3, 500 xu) tự chạy đuổi quạ; Mèo Mướp (cấp 5, 900 xu) +10% sản phẩm đôi ở chuồng. Đi theo nông dân, ngủ ban đêm, chạm để vuốt ve.
- **Quạ & bù nhìn:** từ cấp 3, quạ bay đậu vào ô đang lớn (tối đa 2 con), cây ngừng lớn khi quạ đậu (tối đa 90 giây). Chạm quạ để đuổi (+3 xu, +1 XP). Bù nhìn bảo vệ ô vuông 5x5 quanh nó. Không có quạ khi trời tối hoặc mưa.
- **Nhiệm vụ mới:** giao hàng cho khách, đuổi 2 con quạ, nuôi Chó Vàng.
- **Hoạt ảnh đi bộ:** mặt trước có frame chân trái riêng, hai chân bước luân phiên.
- **Còn thiếu:** frame chân trái cho hướng sau lưng của nông dân.

## ĐÃ LÀM (v0.8 – B3.1 + chỉnh sửa)
- **Nhà chính 5 cấp:** cần cấp người chơi (4/7/10/14), xu và vật liệu (Ván gỗ, Đinh, Ngói đỏ). Mỗi cấp: kho +20 chỗ, +3% xu & XP từ đơn hàng, khách, xe tải.
- **Vật liệu:** 40% rơi khi dọn gốc cây/đá, 25% khi giao hàng cho khách. Vật liệu còn thiếu mua bằng kim cương (4 kim cương/món). Đang dùng icon tạm, chờ art.
- **Điểm đẹp:** mỗi món trang trí cộng giá/10 điểm. Có 7 hạng, mỗi hạng +2% thưởng và khách ghé thăm thường hơn 5%.
- **Trang trí:** chạm vào món trang trí để Di chuyển / Cất kho / Bán (50% giá). Đồ đã cất nằm ở Cửa hàng › Trang trí, đặt lại miễn phí.
- **Thú cưng:** tự đi dạo khắp nông trại (có tìm đường), tối về ngủ cạnh Nhà chính. Chó thấy quạ trong bán kính lớn thì chạy tới đuổi.
- **Chờ art:** Nhà chính cấp 2–5 (`building_farmhouse_lv2..5`) và 3 icon vật liệu (`icon_plank`, `icon_nail`, `icon_roof_tile`).

## Giai đoạn B – Làm game hấp dẫn hơn
Mục tiêu: chạm vào thấy "đã tay", nông trại có người qua lại, có lý do quay lại mỗi ngày và có mục tiêu dài hạn.

### B1 – Cảm giác chơi (ĐÃ LÀM)
| # | Chức năng | Nội dung | Asset |
|---|---|---|---|
| B1.1 | Hiệu ứng thao tác | Bụi đất khi cuốc, giọt nước khi tưới, lá bay và sản phẩm nảy lên khi thu hoạch, xu bay về HUD kèm số đếm tăng dần | Hạt tự vẽ bằng code |
| B1.2 | Vụ mùa vàng | Mỗi lần thu hoạch có ~5% ra "nông sản vàng" (x2 sản lượng, lấp lánh, tính vào album) | Hiệu ứng lấp lánh bằng code |
| B1.3 | Lên cấp hoành tráng | Màn hình lên cấp có pháo hoa, rương quà mở ra, danh sách thứ mới mở khoá có hình | Không |
| B1.4 | Rung nhẹ (haptic) | Rung nhẹ khi thu hoạch, khi nhận xu, khi lên cấp (bật/tắt trong Cài đặt) | Không |
| B1.5 | Thông báo đẩy | Báo cây chín, máy xong, chợ bán hết, xe tải về, quà ngày sẵn sàng | Gói Mobile Notifications |

### B2 – Nông trại có sự sống (B2.1–B2.3 ĐÃ LÀM)
| # | Chức năng | Nội dung | Asset |
|---|---|---|---|
| B2.1 | Khách đến thăm | 1–3 NPC lần lượt đi bộ vào nông trại, đứng chờ với bong bóng yêu cầu (1–3 món). Giao thì nhận nhiều xu + XP, đôi khi kim cương; từ chối thì khách đi. Mỗi khách có tên, câu thoại, mức thân thiết | NPC_Visitors_4x4 ✔ |
| B2.2 | Thú cưng | Chó/mèo đi theo nông dân, nằm ngủ ban đêm. Chó đuổi quạ; mèo cho thêm 10% cơ hội sản phẩm đôi ở chuồng | Pets_Dog_Cat_4x4 ✔ |
| B2.3 | Quạ & bù nhìn | Thỉnh thoảng quạ đậu vào ruộng làm cây ngừng lớn; chạm để đuổi (nhận xu nhỏ). Bù nhìn bảo vệ các ô quanh nó | Crow_3x2 ✔ |
| B2.4 | Phiên chợ cuối tuần | Thứ 7, CN: chợ có thêm khách, giá hot +100%, bảng xếp hạng "nhà nông bán giỏi" (offline, với NPC) | Banner sự kiện |

### B3 – Mục tiêu dài hạn
| # | Chức năng | Nội dung | Asset |
|---|---|---|---|
| B3.1 | Nhà chính lên cấp & điểm đẹp | Mỗi món trang trí có điểm đẹp; tổng điểm cho thưởng % xu/XP. Nâng nhà chính (cần vật liệu) mở thêm ô kho, chỗ chợ, nhiệm vụ | Nhà chính cấp 2, cấp 3 |
| B3.2 | Album sưu tập | Bộ sưu tập: nông sản vàng, cá, sản phẩm chế biến lần đầu… Đủ một bộ thì nhận rương lớn + danh hiệu | Khung thẻ album |
| B3.3 | Vòng quay may mắn | 1 lượt miễn phí mỗi ngày, thêm lượt bằng kim cương; phần thưởng xu, hạt, kim cương, đồ trang trí hiếm | Vòng quay (vẽ bằng code được) |
| B3.4 | Tàu thủy giao hàng | Mở ở cấp 12 tại bến (dùng building_fishing_pier): đơn lớn 9 ô hàng trong 8 giờ, mỗi ô giao xong cộng điểm, đủ hết thưởng lớn + đồ hiếm | Con tàu |
| B3.5 | Kho báu khi dọn đất | Dọn gốc cây / đá có thể rơi rương nhỏ (xu, kim cương, vật liệu nâng cấp) | Rương nhỏ |

## Giai đoạn C – Live-ops
| # | Chức năng | Nội dung |
|---|---|---|
| C1 | Mùa vụ | Xuân/Hạ/Thu/Đông theo tuần thật, cây đặc trưng, màu cỏ khác |
| C2 | Sự kiện theo dịp | Tết, Trung Thu: nhiệm vụ riêng, đồ trang trí giới hạn, bảng điểm |
| C3 | Thẻ mùa (Farm Pass) | Thang phần thưởng miễn phí/trả phí theo điểm nhiệm vụ |
| C4 | Remote Config | Chỉnh giá, sự kiện từ xa |

## Giai đoạn D – Online
Tài khoản + cloud save → thăm nông trại bạn bè (tưới giúp, để quà) → chợ giữa người chơi → bảng xếp hạng.

## Giai đoạn E – Kiếm tiền
IAP (gói khởi đầu, gói kim cương) và quảng cáo thưởng (x2 thu hoạch, tăng tốc, thêm lượt quay).

## Giai đoạn F – Âm thanh (làm cuối theo yêu cầu)
Nhạc nền ngày/đêm, tiếng thao tác, tiếng vật nuôi, tiếng xu.

## Xuyên suốt
- Hướng dẫn chơi lần đầu.
- Cân bằng kinh tế.
- Build Android.
- Analytics: đo tỉ lệ ở lại ngày 1 và ngày 7.

## Thứ tự đề xuất (còn lại)
B3.2/B3.3 album, vòng quay → B3.4 tàu thủy → B2.4 phiên chợ → C → D → E → F
