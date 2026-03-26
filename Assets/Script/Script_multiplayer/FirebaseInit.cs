using UnityEngine;
using Firebase;
using Firebase.Extensions;
using System;

public class FirebaseInit : MonoBehaviour
{
    void Start()
    {
        // 1. Tạo cấu hình bằng tay để điền URL bị thiếu
        AppOptions options = new AppOptions();
        // THAY CÁI LINK DƯỚI ĐÂY BẰNG LINK TRÊN WEB FIREBASE CỦA BẠN
        options.DatabaseUrl = new Uri("https://multiplayer-studie-default-rtdb.asia-southeast1.firebasedatabase.app/");
        options.ApiKey = "AIzaSyAr_kEm2aJOR4QAU095qmzkLQEBj3FRpVk";
        options.AppId = "1:952390969854:android:8280366b02a22d6e0cb8af";
        options.ProjectId = "multiplayer-studie";

        // 2. Kiểm tra dependencies
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // 3. Khởi tạo bằng options thay vì DefaultInstance
                FirebaseApp app = FirebaseApp.Create(options);
                Debug.Log(">>> KẾT NỐI FIREBASE THÀNH CÔNG RỒI NÈ!");
            }
            else
            {
                Debug.LogError("Lỗi: " + dependencyStatus);
            }
        });
    }
}