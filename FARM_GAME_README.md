# Nông Trại Vui Vẻ – Unity 6 (mobile, màn hình ngang)

## Chạy thử
1. Mở project bằng Unity 6000.6.5f1. Lần đầu mở, script `Assets/Farm/Editor/FarmProjectSetup.cs` tự chạy:
   cắt toàn bộ sprite theo `Assets/FarmGameArt/Data/sprite_layout.json`, tạo `Resources/FarmSpriteDB.asset`,
   cấu hình màn hình ngang và tạo scene `Assets/Farm/Scenes/Farm.unity`.
   Chạy lại bất cứ lúc nào: menu **Farm > Setup Project**.
2. Mở scene: menu **Farm > Open Farm Scene**, Game view chọn tỷ lệ ngang (16:9 / 19.5:9), bấm Play.
3. Build mobile: File > Build Profiles > Android hoặc iOS (bundle id `com.nutrica.nongtraivuive`).

## Điều khiển
- Joystick góc trái (hoặc WASD trong Editor) để đi.
- Chạm ô đất / cây / vật nuôi: nông dân tự đi tới (A*) và làm việc.
- Gieo hạt: chạm ô đất đã cuốc → bảng hạt giống nhỏ hiện cạnh ô (không che ô). Kéo hạt từ bảng qua các ô để gieo hàng loạt, hoặc chạm 1 hạt để gieo ô đó và giữ hạt đang chọn (thanh "Đang gieo" ở trên, bấm ✕ để dừng).
- Kéo ngón tay qua nhiều ô: cuốc / gieo / tưới / thu hoạch / cho ăn hàng loạt (kiểu Hay Day).
- Kéo trên nền trống: di chuyển camera. Hai ngón (hoặc con lăn chuột): zoom.
- Menu **Farm > Delete Save Data** để chơi lại từ đầu; Cài đặt trong game có tốc độ x10 để test.

## Gameplay
| Hệ thống | Nội dung |
|---|---|
| Trồng trọt | 11 cây (thêm mía), cuốc → gieo (tốn xu) → tưới → lớn theo thời gian thực → thu hoạch x2 |
| Vật nuôi | Gà, vịt, bò, dê, lợn, cừu; ăn nông sản → trứng, sữa, truffle, len…; mỗi loại có chuồng riêng (nhà + sân rào: gà/vịt 3x3, bò/lợn/cừu/dê 4x4), 6 con/chuồng, con vật chỉ đi lại trong sân, nông dân đứng ở cổng để cho ăn/thu hoạch |
| Kho hàng | Sức chứa 50 (+25/lần nâng cấp), lọc theo loại, xem công dụng / đơn đang cần, bỏ bớt hàng – **không bán** |
| Chợ nông sản | Quầy 3 chỗ (mua thêm tới 8): chọn hàng + số lượng + giá (60–200% giá chuẩn), khách NPC mua sau một lúc (rẻ → nhanh, đắt → chậm), chạm chợ để nhận xu; 2 mặt hàng "giá hot" +50% mỗi ngày trong game |
| Đơn hàng | Bảng đơn hàng 5 đơn |
| Tiến trình | XP & cấp độ (tối đa 30) mở khoá cây/con/công trình, thưởng xu khi lên cấp |
| Xây dựng | Mua ô đất, vòi phun tự tưới 3x3, chuồng, ~26 đồ trang trí; chế độ Xây dựng để di chuyển |
| Mở rộng đất | Bắt đầu 14x14 ô; 6 vùng mới mở ở cấp 3/5/7/9/11/13 bằng xu (chạm biển gỗ ở rìa rừng); vùng khoá phủ rừng, camera giới hạn trong đất đã mở. Cấu hình ở `Defs.Zones` |
| Dọn đất | Gốc cây / đá / bụi rậm trên mỗi vùng, chạm 2 lần để dọn (tốn xu, nhận XP) |
| Chế biến | 8 máy, 24 công thức: Cối xay, Lò bánh (+ bánh anh đào, bánh mật ong), Xưởng bơ sữa, Máy ép (+ nước cam, nước chanh), Xưởng đường (mía → đường, cấp 10), Máy bỏng ngô (cấp 12), Khung dệt (vải, áo len, cấp 14), Máy làm kem (cấp 16); hàng đợi 2–5 chỗ, chạy cả khi offline |
| Cây ăn quả & tổ ong | Bụi quả mọng, táo, anh đào, cam, chanh: thu 6 lần, hết quả thì trồng lại 50% giá. Tổ ong cho mật mãi mãi; cây ăn quả trong bán kính 3 ô quanh tổ ong được +1 quả mỗi lần thu |
| Kim cương | Bắt đầu 10; nhận khi lên cấp, nhận thành tựu, rương nhiệm vụ ngày, xuất xe tải, quà đăng nhập, cá vàng. Dùng "Xong ngay" (~1 kim cương / 6 phút) cho cây, vật nuôi, cây ăn quả, máy, quầy chợ, xe tải, đơn hàng |
| Cảm giác chơi | Hiệu ứng bụi đất khi cuốc, giọt nước khi tưới, lá bay khi thu hoạch, lấp lánh khi thu sản phẩm; xu/kim cương trên HUD đếm tăng dần; 5% vụ mùa vàng (x2, lấp lánh, thành tựu "Bàn tay vàng"); màn hình lên cấp có pháo giấy, phần thưởng và hình các thứ mới mở khoá |
| Rung & thông báo (iOS) | Rung nhẹ bằng Taptic Engine (Plugins/iOS/FarmNative.mm); thông báo cục bộ khi app vào nền: cả ruộng chín, vật nuôi có sản phẩm, máy xong, chợ bán hết, xe tải về, quà ngày 8:00 sáng mai, nhắc sau 26 giờ. Bật/tắt trong Cài đặt; xin quyền từ cấp 2 |
| Trong lúc bạn vắng nhà | Mở game sau ≥5 phút (hoặc quay lại từ nền): bảng tóm tắt cây chín, vật nuôi, máy xong, chợ bán được, xe tải, đơn mới; nhận xu chợ ngay |
| Xe tải | 6 thùng hàng, đóng từng thùng nhận xu, đủ thùng thì xuất hàng nhận thưởng lớn, nghỉ 20 phút |
| Câu cá | Điểm câu ở ao (vùng 1) và hồ (vùng 4), mini-game canh kim, 3 loại cá, 5 lượt/điểm hồi 4 phút |
| Nhiệm vụ ngày & thành tựu | 3 nhiệm vụ/ngày + rương thưởng; 8 thành tựu × 3 bậc |
| Ngày–đêm & thời tiết | 1 ngày = 20 phút, đêm tối màu + đèn đường sáng, mưa buổi chiều tự tưới cây |
| Hướng dẫn | Mũi tên chỉ vào đúng đối tượng của nhiệm vụ, icon bay về kho/xu |
| Nâng cấp | Chuồng +2 chỗ (3 lần), máy chế biến thêm chỗ hàng đợi |
| Giữ chân | 14 nhiệm vụ hướng dẫn, quà đăng nhập hằng ngày (chuỗi 7 ngày), lưu tự động + cây lớn khi offline |

## Cấu trúc code (`Assets/Farm/Scripts`)
- `Core/` – Bootstrap (điểm vào duy nhất), GameManager (xu/XP/kho/clock/save), GameDefs (bảng cân bằng), IsoGrid, SpriteDB.
- `World/` – WorldMap (bản đồ 30x30, chiếm ô), PlotView, AnimalView, PlaceView/DebrisView, FarmerController (+A*), CameraController, InteractionController (input), PlacementController.
- `UI/` – UI dựng bằng code (uGUI): HUD, thanh hạt giống, popup Kho/Chợ/Cửa hàng/Đơn hàng/Cài đặt, Joystick, SafeArea.
- `Systems/` – OrderSystem, QuestSystem, MarketSystem (quầy chợ), FeatureSystems, TaskSystem, DayNight.

Cân bằng game (giá, thời gian, mở khoá) chỉnh tập trung trong `Core/GameDefs.cs` và `Core/GameDefsFeatures.cs`.
Cài đặt trong game có nút "Thử nghiệm: +1000 xu, +1 cấp" và tốc độ x10 để test nhanh.
Asset chưa có hình được thay tạm bằng sprite khác (`Defs.SpriteFallbacks`); khi thêm hình thật, sprite cùng tên sẽ tự được dùng.

## Ghi chú asset
Atlas gốc trong pack không khớp lưới 313px (sprite tràn ô, nhiễu alpha). Bản trong project đã được tách lại,
làm sạch nhiễu và đóng gói lại với rect + pivot chính xác; pack gốc không bị sửa.
Hướng nhìn trong atlas không khớp tên, nên game dùng frame `_se` (mặt trước, đi xuống) và frame quay lưng (`_sw` cho nông dân/bò, `_nw` cho các thú khác) khi đi lên; lật ngang để có hướng còn lại.
Ô đất dùng tile đất cày phẳng (sẫm màu khi đã tưới); cây trồng được tách khỏi luống đất nổi của atlas gốc để mọc trực tiếp trên ô.

## Việc nên làm tiếp
- Âm thanh/nhạc nền (pack chưa có audio).
- Animation hành động nhiều frame, chuỗi chế biến (cối xay, xưởng…), IAP/quảng cáo khi gameplay đã chốt.
