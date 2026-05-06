using System;
using System.Security.Cryptography;
using System.Text;
using Firebase.Firestore;
using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Nhận diện ngữ cảnh runtime (main/clone/editor/device) để áp chính sách local cache an toàn.
    /// </summary>
    public static class RuntimeInstanceContext
    {
        private static string cachedInstanceId;

        public static bool IsEditorCloneProcess
        {
            get
            {
                if (!Application.isEditor)
                    return false;

                string path = Application.dataPath ?? string.Empty;
                return path.IndexOf("clone", StringComparison.OrdinalIgnoreCase) >= 0
                       || path.IndexOf("parrelsync", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public static bool ShouldDisableFirestorePersistence
        {
            get
            {
                // Chỉ tắt persistence trên editor clone để tránh lock LevelDB.
                // Trên device/release giữ persistence để hỗ trợ offline cache.
                return IsEditorCloneProcess;
            }
        }

        public static string InstanceId
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedInstanceId))
                    return cachedInstanceId;

                // Dùng dataPath đầy đủ (ParrelSync clone có suffix _clone_0, _clone_1)
                // + process ID để đảm bảo unique kể cả khi chạy nhiều instance cùng lúc
                string dataPath   = Application.dataPath ?? string.Empty;
                string processId  = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
                string source     = $"{Application.companyName}|{Application.productName}|{dataPath}|pid:{processId}";
                string hash       = Sha1Hex(source);
                string key        = $"instance_id_{hash}";

                string existing = PlayerPrefs.GetString(key, null);
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    cachedInstanceId = existing;
                    return cachedInstanceId;
                }

                // Tạo ID mới — bao gồm timestamp + random để đảm bảo unique
                string created = $"inst_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{UnityEngine.Random.Range(10000, 99999)}";
                PlayerPrefs.SetString(key, created);
                PlayerPrefs.Save();
                cachedInstanceId = created;
                return cachedInstanceId;
            }
        }

        public static string Describe()
        {
            string mode = Application.isEditor ? (IsEditorCloneProcess ? "editor-clone" : "editor-main") : "device";
            return $"{mode}|{InstanceId}";
        }

        public static void ConfigureFirestoreSettings(FirebaseFirestore firestore, string ownerTag)
        {
            if (firestore == null)
                return;

            try
            {
                bool shouldDisable = ShouldDisableFirestorePersistence;
                bool desiredPersistence = !shouldDisable;

                if (firestore.Settings.PersistenceEnabled != desiredPersistence)
                {
                    firestore.Settings.PersistenceEnabled = desiredPersistence;
                }

                Debug.Log($"[{ownerTag}] Firestore persistence={(firestore.Settings.PersistenceEnabled ? "ON" : "OFF")} | runtime={Describe()}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{ownerTag}] Không thể áp dụng Firestore settings: {ex.Message} | runtime={Describe()}");
            }
        }

        private static string Sha1Hex(string input)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
                var builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
