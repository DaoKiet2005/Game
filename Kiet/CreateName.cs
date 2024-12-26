using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System;

public class CreateName : MonoBehaviour
{
    public InputField ipNameInput;
    public Button buttonSubmitName;
    public InputField ipAgeInput;
    public InputField ipPhoneInput;

    private DatabaseReference databaseReference;
    private FirebaseAuth auth;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            ipNameInput.text = user.DisplayName ?? user.Email.Split('@')[0]; // Hiển thị tên hiện tại hoặc tên mặc định từ email
            LoadUserData(user.UserId);
        }

        buttonSubmitName.onClick.AddListener(UpdateDisplayName);
    }

    public static class EncryptionHelper
    {
        private static readonly string Key = "YourSecretKey123";
        private static readonly string IV = "YourIV1234567890";

        public static string EncryptEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = Encoding.UTF8.GetBytes(IV);
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] emailBytes = Encoding.UTF8.GetBytes(email);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(emailBytes, 0, emailBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }
    }

    public void LoadUserData(string userId)
    {
        string path = "users/" + userId;

        databaseReference.Child(path).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to load user data: " + task.Exception);
                return;
            }

            if (task.Result.Exists)
            {
                string json = task.Result.GetRawJsonValue();
                UserProfileData userProfileData = JsonUtility.FromJson<UserProfileData>(json);

                ipNameInput.text = userProfileData.Name;
                ipAgeInput.text = EncryptionHelper.EncryptEmail(userProfileData.Age);
                ipPhoneInput.text = EncryptionHelper.EncryptEmail(userProfileData.Phone);
            }
            else
            {
                Debug.LogWarning("No user data found.");
            }
        });
    }

    public void UpdateDisplayName()
    {
        string name = ipNameInput.text;
        string age = ipAgeInput.text;
        string phone = ipPhoneInput.text;

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("Name cannot be empty!");
            return;
        }

        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            user.UpdateUserProfileAsync(new UserProfile { DisplayName = name }).ContinueWithOnMainThread(task => {
                if (task.IsCanceled)
                {
                    Debug.Log("Profile update canceled.");
                }
                else if (task.IsFaulted)
                {
                    Debug.LogError("Profile update failed: " + task.Exception);
                }
                else
                {
                    Debug.Log("Profile updated successfully!");

                    string userId = user.UserId;

                    UserProfileData userProfileData = new UserProfileData
                    {
                        Name = name,
                        Email = EncryptionHelper.EncryptEmail(user.Email),
                        Date = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm"),
                        Age = EncryptionHelper.EncryptEmail(age),
                        Phone = EncryptionHelper.EncryptEmail(phone)
                    };

                    string path = "users/" + userId; // Đường dẫn lưu trữ dữ liệu trong database
                    string json = JsonUtility.ToJson(userProfileData);
                    databaseReference.Child(path).SetRawJsonValueAsync(json).ContinueWithOnMainThread(dbTask => {
                        if (dbTask.IsCanceled)
                        {
                            Debug.Log("Database update canceled.");
                        }
                        else if (dbTask.IsFaulted)
                        {
                            Debug.LogError("Database update failed: " + dbTask.Exception);
                        }
                        else
                        {
                            Debug.Log("Database update successful!");
                            SceneManager.LoadScene("Profile");
                        }
                    });
                }
            });
        }
        else
        {
            Debug.LogError("No user is logged in.");
        }
    }

    [System.Serializable]
    public class UserProfileData
    {
        public string Name;
        public string Email;
        public string Date;
        public string Phone;
        public string Age;
    }
}
