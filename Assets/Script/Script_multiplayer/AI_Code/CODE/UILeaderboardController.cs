using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 12: Leaderboard Panel
    /// </summary>
    public class UILeaderboardController : BasePanelController
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button topWeekButton;
        [SerializeField] private Button topMonthButton;
        [SerializeField] private Button topAllButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private RectTransform content;
        [SerializeField] private LeaderboardEntryWidget entryPrefab;
        [SerializeField] private UIFlowManager flowManager;

        private readonly List<LeaderboardEntryWidget> entries = new List<LeaderboardEntryWidget>();
        private string currentFilter = "all";

        protected override void Awake()
        {
            base.Awake();
            backButton?.onClick.AddListener(() => flowManager.Back());
            topWeekButton?.onClick.AddListener(() => _ = LoadLeaderboardAsync("week"));
            topMonthButton?.onClick.AddListener(() => _ = LoadLeaderboardAsync("month"));
            topAllButton?.onClick.AddListener(() => _ = LoadLeaderboardAsync("all"));
            refreshButton?.onClick.AddListener(() => _ = LoadLeaderboardAsync(currentFilter));
        }

        protected override void OnShow()
        {
            base.OnShow();
            _ = LoadLeaderboardAsync(currentFilter);
        }

        private void ClearEntries()
        {
            foreach (var entry in entries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            entries.Clear();
        }

        private async Task LoadLeaderboardAsync(string filter)
        {
            currentFilter = filter;
            ClearEntries();

            // TODO: Thay bằng call Firebase. Tạm thời mock data.
            await Task.Delay(100);
            var mockData = BuildMockData();

            int rank = 1;
            foreach (var data in mockData)
            {
                var widget = Instantiate(entryPrefab, content);
                widget.SetData(rank, data.username, data.totalScore);
                entries.Add(widget);
                rank++;
            }
        }

        private List<PlayerData> BuildMockData()
        {
            var list = new List<PlayerData>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new PlayerData
                {
                    username = $"Player{i + 1}",
                    totalScore = Random.Range(5000, 15000)
                });
            }
            list.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
            return list;
        }
    }
}
