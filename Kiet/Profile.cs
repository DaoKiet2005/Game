using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Database;
using System.Text;
using System;
using System.Security.Cryptography;

public class Profile : MonoBehaviour
{
    public TMP_Text txtName;
    public TMP_Text txtEmail;
    public TMP_Text txtAge;
    public TMP_Text txtPhone;
    public TMP_Text txtDate;
    public Button buttonLogout;
    public Button buttonNext;

    private DatabaseReference databaseReference;
    private FirebaseAuth auth;

    void Start()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null)
        {
            Debug.Log($"Current User ID: {currentUser.UserId}");
            DisplayUserInfo(currentUser.UserId);
        }
        else
        {
            Debug.LogError("No user is logged in.");
        }
        buttonLogout.onClick.AddListener(Logout);
        buttonNext.onClick.AddListener(Next);
    }

    public static class EncryptionHelper
    {
        private static readonly string Key = "YourSecretKey123";
        private static readonly string IV = "YourIV1234567890";

        public static string DecryptEmail(string encryptedEmail)
        {
            if (string.IsNullOrEmpty(encryptedEmail)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = Encoding.UTF8.GetBytes(IV);
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedEmail);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

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

    async void DisplayUserInfo(string userId)
    {
        DatabaseReference databaseReference = FirebaseDatabase.DefaultInstance.GetReference("users").Child(userId);

        try
        {
            DataSnapshot snapshot = await databaseReference.GetValueAsync();
            if (snapshot.Exists)
            {
                string name = snapshot.Child("Name").Value?.ToString() ?? "N/A";
                string encryptedEmail = snapshot.Child("Email").Value?.ToString() ?? "N/A";
                string encryptedAge = snapshot.Child("Age").Value?.ToString() ?? "N/A";
                string encryptedPhone = snapshot.Child("Phone").Value?.ToString() ?? "N/A";
                string date = snapshot.Child("Date").Value?.ToString() ?? "N/A";

                // Giải mã thông tin Age và Phone
                string email = EncryptionHelper.DecryptEmail(encryptedEmail);
                string age = EncryptionHelper.DecryptEmail(encryptedAge);
                string phone = EncryptionHelper.DecryptEmail(encryptedPhone);

                // Hiển thị thông tin lên giao diện
                if (txtName != null) txtName.text = name;
                if (txtEmail != null) txtEmail.text = email;
                if (txtAge != null) txtAge.text = age;
                if (txtPhone != null) txtPhone.text = phone;
                if (txtDate != null) txtDate.text = date;

                Debug.Log($"Name: {name}, Email: {email}, Age: {encryptedAge}, Phone: {encryptedPhone}, Date: {date}");
            }
            else
            {
                Debug.LogWarning("No data found for user.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error fetching user data: " + ex.Message);
        }
    }


    void Logout()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null)
        {
            string logoutTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm");

            string userId = currentUser.UserId;

            string loginLogsPath = "login_logs/" + userId;

            LoginLog loginLog = new LoginLog
            {
                Name = currentUser.DisplayName ?? "No Display Name",
                Email1 = EncryptionHelper.EncryptEmail(currentUser.Email),
                LoginTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm"),
                LogoutTime = logoutTime
            };

            DatabaseReference userReference = FirebaseDatabase.DefaultInstance.GetReference(loginLogsPath);

            userReference.SetRawJsonValueAsync(JsonUtility.ToJson(loginLog)).ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Logout log recorded: " + JsonUtility.ToJson(loginLog));
                }
                else
                {
                    Debug.LogError("Failed to record logout log: " + task.Exception);
                }
            });
        }

        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("User has been logged out.");

        SceneManager.LoadScene("Login");
    }

    void Next()
    {
        SceneManager.LoadScene("Manager");
    }

    [System.Serializable]
    public class LoginLog
    {
        public string Name;
        public string Email1;
        public string LoginTime;
        public string LogoutTime;
    }
}
