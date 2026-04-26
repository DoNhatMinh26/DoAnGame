using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Service lưu tiến trình local cho guest mode
    /// Dữ liệu lưu trong PlayerPrefs, không đồng bộ cross-device
    /// </summary>
    public class LocalProgressService : MonoBehaviour
    {
        private static LocalProgressService instance;
        public static LocalProgressService Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[LocalProgressService]");
                    instance = go.AddComponent<LocalProgressService>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // PlayerPrefs keys
        private const string LOCAL_SCORE_KEY = "LocalGuestScore";
        private const string LOCAL_AVATAR_KEY = "LocalGuestAvatar";
        
        // Tiến trình từng chế độ (grade 1-5, level 1-100)
        private const string LOCAL_CHONDA_PROGRESS_KEY = "LocalGuest_ChonDA_Grade{0}_Level";
        private const string LOCAL_KEOTHADA_PROGRESS_KEY = "LocalGuest_KeoThaDA_Grade{0}_Level";
        private const string LOCAL_PHITHUYEN_PROGRESS_KEY = "LocalGuest_PhiThuyen_Grade{0}_Level";

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Score Management
        
        /// <summary>
        /// Lưu điểm số
        /// </summary>
        public void SaveScore(int score)
        {
            int currentScore = GetScore();
            int newScore = currentScore + score;
            PlayerPrefs.SetInt(LOCAL_SCORE_KEY, newScore);
            PlayerPrefs.Save();
            Debug.Log($"[LocalProgress] Saved score: {newScore} (added {score})");
        }

        /// <summary>
        /// Lấy điểm số hiện tại
        /// </summary>
        public int GetScore()
        {
            return PlayerPrefs.GetInt(LOCAL_SCORE_KEY, 0);
        }
        
        #endregion

        #region Avatar Management
        
        /// <summary>
        /// Lưu avatar đã chọn
        /// </summary>
        public void SaveAvatar(string avatarId)
        {
            PlayerPrefs.SetString(LOCAL_AVATAR_KEY, avatarId);
            PlayerPrefs.Save();
            Debug.Log($"[LocalProgress] Saved avatar: {avatarId}");
        }

        /// <summary>
        /// Lấy avatar đã chọn
        /// </summary>
        public string GetAvatar()
        {
            return PlayerPrefs.GetString(LOCAL_AVATAR_KEY, "default");
        }
        
        #endregion

        #region Progress Management (ChonDA, KeoThaDA, PhiThuyen)
        
        /// <summary>
        /// Lưu tiến trình cho chế độ Chọn Đáp Án
        /// </summary>
        public void SaveChonDAProgress(int grade, int level)
        {
            string key = string.Format(LOCAL_CHONDA_PROGRESS_KEY, grade);
            PlayerPrefs.SetInt(key, level);
            PlayerPrefs.Save();
            Debug.Log($"[LocalProgress] ChonDA - Grade {grade}: Level {level}");
        }

        /// <summary>
        /// Lấy tiến trình Chọn Đáp Án
        /// </summary>
        public int GetChonDAProgress(int grade)
        {
            string key = string.Format(LOCAL_CHONDA_PROGRESS_KEY, grade);
            return PlayerPrefs.GetInt(key, 1); // Default level 1
        }

        /// <summary>
        /// Lưu tiến trình cho chế độ Kéo Thả Đáp Án
        /// </summary>
        public void SaveKeoThaDAProgress(int grade, int level)
        {
            string key = string.Format(LOCAL_KEOTHADA_PROGRESS_KEY, grade);
            PlayerPrefs.SetInt(key, level);
            PlayerPrefs.Save();
            Debug.Log($"[LocalProgress] KeoThaDA - Grade {grade}: Level {level}");
        }

        /// <summary>
        /// Lấy tiến trình Kéo Thả Đáp Án
        /// </summary>
        public int GetKeoThaDAProgress(int grade)
        {
            string key = string.Format(LOCAL_KEOTHADA_PROGRESS_KEY, grade);
            return PlayerPrefs.GetInt(key, 1);
        }

        /// <summary>
        /// Lưu tiến trình cho chế độ Phi Thuyền
        /// </summary>
        public void SavePhiThuyenProgress(int grade, int level)
        {
            string key = string.Format(LOCAL_PHITHUYEN_PROGRESS_KEY, grade);
            PlayerPrefs.SetInt(key, level);
            PlayerPrefs.Save();
            Debug.Log($"[LocalProgress] PhiThuyen - Grade {grade}: Level {level}");
        }

        /// <summary>
        /// Lấy tiến trình Phi Thuyền
        /// </summary>
        public int GetPhiThuyenProgress(int grade)
        {
            string key = string.Format(LOCAL_PHITHUYEN_PROGRESS_KEY, grade);
            return PlayerPrefs.GetInt(key, 1);
        }
        
        #endregion

        #region Data Management
        
        /// <summary>
        /// Xóa tất cả dữ liệu local (khi đăng nhập)
        /// </summary>
        public void ClearAllData()
        {
            PlayerPrefs.DeleteKey(LOCAL_SCORE_KEY);
            PlayerPrefs.DeleteKey(LOCAL_AVATAR_KEY);
            
            // Xóa tiến trình tất cả grades (1-5)
            for (int grade = 1; grade <= 5; grade++)
            {
                PlayerPrefs.DeleteKey(string.Format(LOCAL_CHONDA_PROGRESS_KEY, grade));
                PlayerPrefs.DeleteKey(string.Format(LOCAL_KEOTHADA_PROGRESS_KEY, grade));
                PlayerPrefs.DeleteKey(string.Format(LOCAL_PHITHUYEN_PROGRESS_KEY, grade));
            }
            
            PlayerPrefs.Save();
            Debug.Log("[LocalProgress] Cleared all local data");
        }

        /// <summary>
        /// Lấy tất cả dữ liệu để migrate sang Firebase
        /// </summary>
        public GuestProgressData GetAllData()
        {
            var data = new GuestProgressData
            {
                score = GetScore(),
                avatar = GetAvatar(),
                chonDAProgress = new int[5],
                keoThaDAProgress = new int[5],
                phiThuyenProgress = new int[5]
            };

            for (int grade = 1; grade <= 5; grade++)
            {
                data.chonDAProgress[grade - 1] = GetChonDAProgress(grade);
                data.keoThaDAProgress[grade - 1] = GetKeoThaDAProgress(grade);
                data.phiThuyenProgress[grade - 1] = GetPhiThuyenProgress(grade);
            }

            return data;
        }

        /// <summary>
        /// Hiển thị thống kê
        /// </summary>
        public void LogStats()
        {
            Debug.Log("=== Local Guest Stats ===");
            Debug.Log($"Score: {GetScore()}");
            Debug.Log($"Avatar: {GetAvatar()}");
            
            for (int grade = 1; grade <= 5; grade++)
            {
                Debug.Log($"Grade {grade}:");
                Debug.Log($"  ChonDA: Level {GetChonDAProgress(grade)}");
                Debug.Log($"  KeoThaDA: Level {GetKeoThaDAProgress(grade)}");
                Debug.Log($"  PhiThuyen: Level {GetPhiThuyenProgress(grade)}");
            }
            
            Debug.Log("========================");
        }
        
        #endregion
    }

    /// <summary>
    /// Data structure cho migration
    /// </summary>
    [System.Serializable]
    public class GuestProgressData
    {
        public int score;
        public string avatar;
        public int[] chonDAProgress;      // 5 grades
        public int[] keoThaDAProgress;    // 5 grades
        public int[] phiThuyenProgress;   // 5 grades
    }
}
