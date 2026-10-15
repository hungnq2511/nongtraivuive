using System.Collections.Generic;
using UnityEngine;

namespace Farm
{
    public class VisitorDef
    {
        public string id, name, title;
        public float scale = 0.75f;
        /// <summary>Kind of goods this visitor prefers: "crop", "animal", "processed", "any".</summary>
        public string likes = "any";
        public string[] hello, thanks;
        public string Frame(string pose) => "npc_" + id + "_" + pose;
    }

    public class PetDef
    {
        public string id, name, perk;
        public int cost, unlock;
        public float scale;
        public string Frame(string pose) => id + "_" + pose;
    }

    /// <summary>B2 – a farm with life: visitors, pets, crows.</summary>
    public static partial class Defs
    {
        // ---------------- visitors
        public const int VisitorUnlock = 3;
        public const float VisitorMinGap = 150f, VisitorMaxGap = 360f; // seconds between arrivals
        public const float VisitorGemChance = 0.15f;
        public const int FriendPerLevel = 3;       // deliveries per friendship heart
        public const int FriendMaxLevel = 10;
        public const float FriendBonusPerLevel = 0.05f;
        /// <summary>Road cell where visitors appear (left edge of the starter farm's road).</summary>
        public static readonly Vector2Int VisitorEntry = new Vector2Int(2, 6);
        /// <summary>Waiting spots on the road in front of the buildings.</summary>
        public static readonly Vector2Int[] VisitorSpots = { new Vector2Int(7, 6), new Vector2Int(10, 6), new Vector2Int(13, 6) };

        public static int MaxVisitors(int level) => level < VisitorUnlock ? 0 : level < 6 ? 1 : level < 10 ? 2 : 3;

        public static readonly List<VisitorDef> Visitors = new List<VisitorDef>
        {
            new VisitorDef { id = "grandma", name = "Bà Tư", title = "hàng xóm", scale = 0.74f, likes = "crop",
                hello = new[] { "Cháu ơi, bà đang nấu canh mà thiếu rau, cháu bán cho bà ít nhé!", "Nông trại cháu dạo này đẹp quá! Bà mua ít đồ về làm bánh.", "Trời đẹp thế này bà đi chợ sớm, cháu có hàng không?" },
                thanks = new[] { "Cảm ơn cháu ngoan! Bà gửi tiền nhé.", "Tốt quá, mai bà lại ghé!" } },
            new VisitorDef { id = "boy", name = "Bé Tí", title = "cậu bé trong xóm", scale = 0.7f, likes = "any",
                hello = new[] { "Chú ơi! Mẹ cháu dặn mua mấy thứ này!", "Cháu để dành tiền heo đất để mua đó!", "Nhà chú có con gà nào đẻ trứng vàng không?" },
                thanks = new[] { "Hoan hô! Cháu cảm ơn chú!", "Mẹ sẽ khen cháu cho xem!" } },
            new VisitorDef { id = "chef", name = "Cô Mai", title = "đầu bếp quán ăn", scale = 0.75f, likes = "processed",
                hello = new[] { "Quán cô tối nay đông khách, cần gấp nguyên liệu tươi!", "Món đặc biệt hôm nay cần đồ ngon nhất của cháu đó.", "Nguyên liệu nhà cháu nấu món gì cũng ngon!" },
                thanks = new[] { "Tuyệt vời! Thực khách sẽ thích lắm đây.", "Cô sẽ quay lại thường xuyên!" } },
            new VisitorDef { id = "merchant", name = "Chú Ba", title = "thương lái", scale = 0.78f, likes = "animal",
                hello = new[] { "Chú gom hàng mang lên thành phố, trả giá cao đây!", "Hàng nhà cháu bán chạy lắm, chú lấy thêm nhé.", "Làm ăn lâu dài nha cháu, chú trả sòng phẳng!" },
                thanks = new[] { "Giao dịch vui vẻ! Lần sau chú lại tới.", "Hàng tốt, tiền tươi!" } },
        };

        public static VisitorDef Visitor(string id) => Visitors.Find(v => v.id == id);

        // ---------------- pets
        public const float CatDoubleChance = 0.10f;
        public const float DogChaseRadius = 14f;   // world units around the dog

        public static readonly List<PetDef> Pets = new List<PetDef>
        {
            new PetDef { id = "dog", name = "Chó Vàng", perk = "Đi dạo quanh nông trại, tự đuổi quạ khỏi ruộng", cost = 500, unlock = 3, scale = 0.62f },
            new PetDef { id = "cat", name = "Mèo Mướp", perk = "+10% sản phẩm đôi ở chuồng", cost = 900, unlock = 5, scale = 0.6f },
        };

        public static PetDef Pet(string id) => Pets.Find(p => p.id == id);

        // ---------------- crows & scarecrows
        public const int CrowUnlock = 3;
        public const int CrowMax = 2;
        public const float CrowMinGap = 70f, CrowMaxGap = 170f;
        public const float CrowMaxStay = 90f;        // seconds a crow stays (growth is paused meanwhile)
        public const int CrowCoins = 3;
        public const int ScarecrowRadius = 2;         // protects a 5x5 square around each scarecrow
    }
}
