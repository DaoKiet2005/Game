using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class ManagerRoom : MonoBehaviour
{
    [Header("Room Creation")]
    public InputField ipRoomId;
    public InputField ipRoomName;
    public Button buttonCreateRoom;

    [Header("Switch Forms")]
    public Button buttonSwitchToCreate;
    public Button buttonSwitchToJoin;
    public GameObject CreateRoomForm;
    public GameObject JoinRoomForm;

    [Header("Notification")]
    public TMP_Text txtNotification;
    public TMP_Text txtNotification1;

    [Header("UI Elements")]
    public Transform contentT; 
    public GameObject TemplateObj;

    private DatabaseReference databaseReference;

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase Initialized");
                FetchRoomsFromFirebase();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
            }
        });

        buttonCreateRoom.onClick.AddListener(CreateRoomHandler);
        buttonSwitchToCreate.onClick.AddListener(SwitchToCreateForm);
        buttonSwitchToJoin.onClick.AddListener(SwitchToJoinForm);
    }

    void FetchRoomsFromFirebase()
    {
        FirebaseDatabase.DefaultInstance.GetReference("rooms").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Lỗi khi lấy dữ liệu từ Firebase!");
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    List<CustomRoomInfo> roomInfos = new List<CustomRoomInfo>();

                    foreach (DataSnapshot room in snapshot.Children)
                    {
                        string roomId = room.Key;
                        string roomName = room.Child("roomName").Value.ToString();

                        roomInfos.Add(new CustomRoomInfo
                        {
                            ID = int.Parse(roomId),
                            Name = roomName
                        });
                    }

                    SpawnRoom(roomInfos); // Hiển thị danh sách phòng
                }
            });
    }

    private void SpawnRoom(List<CustomRoomInfo> roomInfos)
    {
        foreach (Transform child in contentT)
        {
            Destroy(child.gameObject);
        }

        foreach (var roomInfo in roomInfos)
        {
            var obj = Instantiate(TemplateObj, contentT);
            obj.SetActive(true);

            var textComponent = obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            var buttonComponent = obj.transform.GetChild(1).GetComponent<Button>();

            if (textComponent != null) textComponent.text = $"ID: {roomInfo.ID} - {roomInfo.Name}";
            if (buttonComponent != null) buttonComponent.onClick.AddListener(() => JoinRoom(roomInfo.ID));
        }
    }

    private void JoinRoom(int roomID)
    {
        Debug.Log("Joining room " + roomID);
        UpdateNotification1("Joined room successfully.");
        Debug.Log("Joined room successfully.");
        SceneManager.LoadScene(sceneName: "MainMenu");
    }

    private void UpdateNotification(string message)
    {
        if (txtNotification != null)
        {
            txtNotification.text = message; // Gán thông báo
        }
        else
        {
            Debug.Log("Notification Text UI is not assigned in the Inspector!");
        }
    }

    private void UpdateNotification1(string message)
    {
        if (txtNotification1 != null)
        {
            txtNotification1.text = message; // Gán thông báo
        }
        else
        {
            Debug.Log("Notification Text UI is not assigned in the Inspector!");
        }
    }

    private void CreateRoomHandler()
    {
        string roomId = ipRoomId.text;
        string roomName = ipRoomName.text;
        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(roomName))
        {
            UpdateNotification("Room ID and Room Name must not be empty!");
            Debug.Log("Room ID and Room Name must not be empty!");
            return;
        }
        // Kiểm tra xem Room ID đã tồn tại chưa
        databaseReference.Child("rooms").Child(roomId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    UpdateNotification("Room ID already exists! Please use a different Room ID.");
                    Debug.LogError("Room ID already exists! Please use a different Room ID.");
                }
                else
                {
                    Room room = new Room(roomId, roomName);
                    string json = JsonUtility.ToJson(room);

                    databaseReference.Child("rooms").Child(roomId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(createTask =>
                    {
                        if (createTask.IsCompleted)
                        {
                            UpdateNotification("Room created successfully!");
                            Debug.Log("Room created successfully.");
                            FetchRoomsFromFirebase();
                            JoinRoomForm.SetActive(true);
                            CreateRoomForm.SetActive(false);
                            // SceneManager.LoadScene(sceneName: "MainMenu");
                        }
                        else
                        {
                            UpdateNotification("Failed to create room: " + createTask.Exception);
                            Debug.Log("Failed to create room: " + createTask.Exception);
                        }
                    });
                }
            }
            else
            {
                Debug.Log("Failed to check Room ID: " + task.Exception);
            }
        });
    }

    public void SwitchToCreateForm()
    {
        CreateRoomForm.SetActive(true);
        JoinRoomForm.SetActive(false);
    }

    public void SwitchToJoinForm()
    {
        CreateRoomForm.SetActive(false);
        JoinRoomForm.SetActive(true);
    }

}
[System.Serializable]
public class Room
{
    public string roomId;
    public string roomName;

    public Room(string roomId, string roomName)
    {
        this.roomId = roomId;
        this.roomName = roomName;
    }
}
[System.Serializable]
public class CustomRoomInfo
{
    public int ID;
    public string Name;
}