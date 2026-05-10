using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Tạo prefix key local riêng theo project hiện tại.
    /// Dùng để main/clone không đụng chung PlayerPrefs.
    /// </summary>
    public static class LocalStorageKeyResolver
    {
        private static string cachedPrefix;

        public static string Key(string baseKey)
        {
            return $"{GetPrefix()}:{baseKey}";
        }

        // ─────────────────────────────────────────────────────────────
        // GAME DATA KEYS — tất cả dùng prefix để main/clone tách biệt
        // ─────────────────────────────────────────────────────────────

        // Logged-in user
        public static string UserScore        => Key("UserScore");
        public static string UserLevel        => Key("UserLevel");
        public static string TotalCoins       => Key("TotalCoins");
        public static string ClassHighest     => Key("Class_HighestLevel");
        public static string KeoThaHighest    => Key("HighestLevelReached");
        public static string SpaceHighest     => Key("Space_HighestLevel");

        // Guest mode
        public static string IsGuestMode      => Key("IsGuestMode");
        public static string GuestName        => Key("GuestPlayerName");
        public static string SelectedGrade    => Key("SelectedGrade");
        public static string LocalGuestScore  => Key("LocalGuestScore");
        public static string LocalGuestLevel  => Key("LocalGuestLevel");

        // Avatar
        public static string SelectedAvatarID => Key("SelectedAvatarID");

        // Shop — Skin / Phao / Ship (cosmetic, không ảnh hưởng gameplay)
        // Dùng Key() để tách biệt main/clone nếu cần test cosmetic riêng
        public static string SelectedSkinID   => Key("SelectedSkinID");
        public static string SelectedPhaoID   => Key("SelectedPhaoID");
        public static string SelectedShipID   => Key("SelectedShipID");
        public static string SelectedClassSkinID => Key("SelectedClassSkinID");

        private static string GetPrefix()
        {
            if (!string.IsNullOrEmpty(cachedPrefix))
                return cachedPrefix;

#if UNITY_EDITOR
            // Editor (bao gồm ParrelSync clone): dùng dataPath hash để tách biệt main/clone
            string source = $"{Application.companyName}|{Application.productName}|{Application.dataPath}";
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(source));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                cachedPrefix = $"local_{builder}";
            }
#else
            // Android/iOS build thực tế: prefix cố định theo app identity
            // Không dùng dataPath vì nó thay đổi giữa các build → mất data
            string fixedSource = $"{Application.companyName}|{Application.productName}";
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(fixedSource));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                cachedPrefix = $"local_{builder}";
            }
#endif

            // Migration một lần: copy dữ liệu từ key cũ sang key mới
            MigrateOldKeys(cachedPrefix);

            return cachedPrefix;
        }

        // ─────────────────────────────────────────────────────────────
        // BACKWARD-COMPATIBLE HELPERS
        // Đọc key mới trước, fallback về key cũ nếu chưa migrate.
        // Dùng cho legacy code (UiClass, UiTp, UiSp, ProfileUI).
        // ─────────────────────────────────────────────────────────────

        public static int GetInt(string newKey, string legacyKey, int defaultValue = 0)
        {
            if (PlayerPrefs.HasKey(newKey))
                return PlayerPrefs.GetInt(newKey, defaultValue);
            return PlayerPrefs.GetInt(legacyKey, defaultValue);
        }

        public static void SetInt(string newKey, string legacyKey, int value)
        {
            PlayerPrefs.SetInt(newKey, value);
            // Không ghi vào legacyKey nữa — chỉ dùng key mới
        }

        public static void DeleteBoth(string newKey, string legacyKey)
        {
            PlayerPrefs.DeleteKey(newKey);
            PlayerPrefs.DeleteKey(legacyKey);
        }

        // ─────────────────────────────────────────────────────────────
        // DYNAMIC KEYS — Skin/Phao/Ship unlock status
        // ─────────────────────────────────────────────────────────────

        public static string SkinUnlockedKey(int index)   => Key($"SkinUnlocked_{index}");
        public static string PhaoUnlockedKey(int index)   => Key($"PhaoUnlocked_{index}");
        public static string ShipUnlockedKey(int index)   => Key($"ShipUnlocked_{index}");
        public static string ClassSkinUnlockedKey(int index) => Key($"ClassSkinUnlocked{index}");

        private static void MigrateOldKeys(string prefix)
        {
            const string MIGRATION_FLAG = "LocalKeyMigrationDone_v1";
            string flagKey = $"{prefix}:{MIGRATION_FLAG}";

            // Đã migrate rồi thì bỏ qua
            if (PlayerPrefs.GetInt(flagKey, 0) == 1) return;

            // Danh sách key cần migrate: (oldKey, newKey)
            var migrations = new (string old, string newSuffix)[]
            {
                ("UserScore",          "UserScore"),
                ("UserLevel",          "UserLevel"),
                ("TotalCoins",         "TotalCoins"),
                ("Class_HighestLevel", "Class_HighestLevel"),
                ("HighestLevelReached","HighestLevelReached"),
                ("Space_HighestLevel", "Space_HighestLevel"),
                ("IsGuestMode",        "IsGuestMode"),
                ("GuestPlayerName",    "GuestPlayerName"),
                ("SelectedGrade",      "SelectedGrade"),
                ("LocalGuestScore",    "LocalGuestScore"),
                ("LocalGuestLevel",    "LocalGuestLevel"),
                ("SelectedAvatarID",   "SelectedAvatarID"),
            };

            bool migrated = false;
            foreach (var (old, newSuffix) in migrations)
            {
                string newKey = $"{prefix}:{newSuffix}";
                // Chỉ copy nếu key mới chưa có và key cũ có giá trị
                if (!PlayerPrefs.HasKey(newKey) && PlayerPrefs.HasKey(old))
                {
                    // Copy int
                    if (old == "GuestPlayerName")
                    {
                        string val = PlayerPrefs.GetString(old, null);
                        if (val != null) PlayerPrefs.SetString(newKey, val);
                    }
                    else
                    {
                        int val = PlayerPrefs.GetInt(old, int.MinValue);
                        if (val != int.MinValue) PlayerPrefs.SetInt(newKey, val);
                    }
                    migrated = true;
                }
            }

            // Đánh dấu đã migrate
            PlayerPrefs.SetInt(flagKey, 1);
            if (migrated) PlayerPrefs.Save();

            UnityEngine.Debug.Log($"[LocalStorageKeyResolver] Migration done (migrated={migrated})");
        }
    }
}