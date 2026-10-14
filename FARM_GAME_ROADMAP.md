# Nông Trại Vui Vẻ – Lộ trình phát triển

## Đã có (v0.2)
Trồng 10 loại cây, 6 vật nuôi + 3 chuồng, kho, chợ, bảng đơn hàng, lên cấp (tối đa 30), cửa hàng + xây dựng/di chuyển,
dọn gốc cây/đá, 14 nhiệm vụ hướng dẫn, quà hằng ngày, lưu offline.
**Mở rộng đất theo cấp:** bắt đầu 14x14 ô; 6 vùng mới mở ở cấp 3/5/7/9/11/13 (250 → 2.800 xu), đất khoá phủ rừng tối màu,
chạm biển gỗ ở rìa để mở; camera chỉ đi trong phần đất đã mở.

## ĐÃ LÀM (v0.3)
Chuỗi chế biến (4 máy, 11 công thức), cây ăn quả, xe tải 6 thùng, câu cá, nhiệm vụ ngày + thành tựu, ngày–đêm + mưa, mũi tên hướng dẫn, icon bay về kho, nâng cấp chuồng/máy. Còn lại: âm thanh (để cuối), online, kiếm tiền.

## Giai đoạn 1 – Cảm giác chơi (ưu tiên ngay)
| Chức năng | Nội dung | Asset |
|---|---|---|
| Âm thanh & nhạc | Nhạc nền, tiếng cuốc/tưới/thu hoạch, tiếng gà bò, tiếng xu | Cần mua/tạo (pack chưa có) |
| Hiệu ứng | Icon nông sản bay về kho, hạt bụi khi cuốc, nước khi tưới, pháo hoa lên cấp | Particle trong Unity |
| Hướng dẫn chỉ tay | Mũi tên/bàn tay chỉ vào đúng đối tượng của nhiệm vụ | Có thể dùng sprite có sẵn |
| Build Android thật | Kiểm tra hiệu năng, cảm ứng, safe area, kích thước app | — |

## Giai đoạn 2 – Chiều sâu kinh tế (khác biệt lớn nhất so với Hay Day hiện tại)
| Chức năng | Nội dung | Asset |
|---|---|---|
| Chuỗi chế biến | Cối xay (lúa mì → bột), Lò bánh (bột + trứng → bánh mì), Xưởng sữa (sữa → phô mai/bơ), Máy ép (dâu/cà chua → nước ép); hàng đợi sản xuất, mua thêm slot | Có nhà: windmill, workshop, dairy_shed; cần icon sản phẩm chế biến |
| Cây ăn quả / bụi quả | Táo, bụi quả mọng thu hoạch nhiều lần, có tuổi thọ | Có sprite apple_tree, berry_bush |
| Xe tải / tàu giao hàng | Đơn lớn nhiều món, thưởng cao, thời gian giới hạn | Cần sprite xe |
| Câu cá | Mini-game tại điểm câu cá ở ao/hồ | Có water_fishing_spot; cần icon cá |
| Nâng cấp công trình | Kho, chuồng (thêm chỗ), vòi phun (bán kính lớn hơn) | Có sẵn |

## Giai đoạn 3 – Giữ chân & live-ops
| Chức năng | Nội dung |
|---|---|
| Nhiệm vụ ngày/tuần + thành tựu | Mục tiêu ngắn hạn, thưởng xu/kim cương |
| Ngày–đêm & thời tiết | Mưa tự tưới, đêm bật đèn đường, gà ngủ |
| Khách NPC ở chợ | Khách đến mua theo giá thị trường biến động |
| Sự kiện mùa | Tết, Trung thu: cây/trang trí giới hạn thời gian |
| Thông báo đẩy | Báo cây chín, vật nuôi có sản phẩm |

## Giai đoạn 4 – Online
| Chức năng | Nội dung |
|---|---|
| Tài khoản + cloud save | Unity Authentication + Cloud Save, chơi nhiều máy |
| Thăm nông trại bạn bè | Xem, giúp tưới, để lại quà |
| Chợ người chơi | Bán/mua nông sản giữa người chơi |
| Bảng xếp hạng | Theo cấp, theo đơn hàng tuần |

## Giai đoạn 5 – Kiếm tiền (sau khi gameplay đã ổn định)
| Chức năng | Nội dung |
|---|---|
| Kim cương | Tăng tốc cây/chế biến, mua slot |
| Quảng cáo thưởng | x2 thu hoạch, tăng tốc 30 phút, quay thưởng hằng ngày |
| IAP | Gói khởi đầu, gói kim cương, gói trang trí độc quyền |
| Thẻ mùa (Farm Pass) | Phần thưởng theo mốc nhiệm vụ mỗi mùa |

## Đề xuất thứ tự làm tiếp
1. Âm thanh + hiệu ứng + chỉ tay hướng dẫn (nhanh, tăng cảm giác "game thật" rõ rệt)
2. Chuỗi chế biến (cối xay, lò bánh, xưởng sữa) – vòng lặp kinh tế chính của thể loại
3. Cây ăn quả + xe tải giao hàng
4. Nhiệm vụ ngày/tuần, thời tiết
5. Cloud save → tính năng xã hội → kiếm tiền
