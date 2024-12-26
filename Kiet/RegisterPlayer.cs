using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Database;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

public class LoginPlayer : MonoBehaviour
{
    [Header(header:"Register")]
    public InputField ipRegisterEmail;
    public InputField ipRegisterPassword;
    public InputField ipComfirmPassword;
    public Button buttonRegister;

    [Header(header:"Sign In")]
    public InputField ipLoginEmail;
    public InputField ipLoginPassword;

    [Header("Notification")]
    public TMP_Text txtNotificationLogin;
    public TMP_Text txtNotificationRegister;

    public Button buttonLogin;
    public Button Forgotpass;
    private FirebaseAuth auth;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        buttonRegister.onClick.AddListener(RegisterFirebase);
        buttonLogin.onClick.AddListener(SignInAccount);
        buttonMoveRegister.onClick.AddListener(SwitchForm);
        buttonMoveToSignIn.onClick.AddListener(SwitchForm);
        Forgotpass.onClick.AddListener(GoForgot);
    }

    private void UpdateNotification(string message)
    {
        if (txtNotificationLogin != null)
        {
            txtNotificationLogin.text = message; 
        }
        else
        {
            Debug.Log("Notification Text UI is not assigned in the Inspector!");
        }
    }
    private void UpdateNotificationRegister(string message)
    {
        if (txtNotificationRegister != null)
        {
            txtNotificationRegister.text = message;
        }
        else
        {
            Debug.Log("Notification Text UI is not assigned in the Inspector!");
        }
    }

    public void RegisterFirebase() 
    {
        string email = ipRegisterEmail.text;
        string password = ipRegisterPassword.text;
        string confirmPassword = ipComfirmPassword.text;

        if (password != confirmPassword)
        {
            UpdateNotificationRegister("The password and the confirmation password do not match!");
            Debug.LogError("Mật khẩu và mật khẩu xác nhận không giống nhau!");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                UpdateNotificationRegister("Registration has been canceled.");
                Debug.Log(message: "Dang ki bi huy");
                return;
            }
            else if (task.IsFaulted)
            {
                UpdateNotificationRegister("Registration failed");
                Debug.Log(message: "Dang ki that bai");
            }
            else if (task.IsCompleted)
            {
                UpdateNotificationRegister("Registration Successful");
                Debug.Log(message: "Dang ki thanh cong");
                FirebaseUser user = task.Result.User;
                string hashedPassword = EncryptionHelper.HashPassword(password);
                string displayName = user.Email.Split('@')[0]; // Nó được sử dụng phần trước dấu '@' làm tên mặc định
                user.UpdateUserProfileAsync(new UserProfile { DisplayName = displayName }).ContinueWithOnMainThread(profileTask =>
                {
                    if (profileTask.IsCanceled)
                    {
                        Debug.Log("Profile update canceled.");
                    }
                    else if (profileTask.IsFaulted)
                    {
                        Debug.LogError("Profile update failed: " + profileTask.Exception);
                    }
                    else
                    {
                        Debug.Log("Profile updated successfully!");
                    }
                });

                DatabaseReference databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                UserProfileData userProfileData = new UserProfileData
                {
                    Name = displayName,
                    Email = user.Email,
                };

                string path = "users/" + user.UserId; 
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
                        SceneManager.LoadScene("CreateName");
                    }
                });
            }
        });
    }

    public void SignInAccount()
    {
        string email = ipLoginEmail.text;
        string password = ipLoginPassword.text;
        SignInAccount(email, password); 
    }

    public void SignInAccount(string email, string password)
    {
        email = ipLoginEmail.text;
        password = ipLoginPassword.text;
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                UpdateNotification("Login has been canceled");
                Debug.Log("Đăng nhập bị hủy");
                return;
            }
            if (task.IsFaulted)
            {
                UpdateNotification("Login failed");
                Debug.Log("Đăng nhập thất bại");
                return;
            }
            if (task.IsCompleted)
            {
                FirebaseUser user = task.Result.User;
                string userId = user.UserId;
                UpdateNotification($"Login successful! Welcome {user.DisplayName ?? user.Email}");
                Debug.Log("Đăng nhập thành công");

                DatabaseReference databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

                // Chỉ cập nhật các trường cần thiết tránh hiện tượng ghi đè hoặc xóa thông tin
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "Name", user.DisplayName ?? "No Display Name" },
                { "Email", EncryptionHelper.EncryptEmail(user.Email) }
            };

                string path = "users/" + userId;
                databaseReference.Child(path).UpdateChildrenAsync(updates).ContinueWithOnMainThread(dbTask =>
                {
                    if (dbTask.IsCanceled) Debug.Log("Database update canceled.");
                    else if (dbTask.IsFaulted) Debug.LogError("Database update failed: " + dbTask.Exception);
                    else Debug.Log("Database update successful!");
                });

                // Lưu thông tin đăng nhập vào login_logs
                string loginLogsPath = "login_logs/" + userId;
                LoginLog loginLog = new LoginLog
                {
                    Name = user.DisplayName ?? "No Display Name",
                    Email1 = EncryptionHelper.EncryptEmail(user.Email),
                    LoginTime = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm"),
                };
                string logJson = JsonUtility.ToJson(loginLog);
                databaseReference.Child(loginLogsPath).Push().SetRawJsonValueAsync(logJson).ContinueWithOnMainThread(logTask =>
                {
                    if (logTask.IsCanceled) Debug.Log("Login log update canceled.");
                    else if (logTask.IsFaulted) Debug.LogError("Login log update failed: " + logTask.Exception);
                    else Debug.Log("Login log update successful!");
                });

                SceneManager.LoadScene("Profile");
            }
        });
    }

    [Header(header:"Switch form")]  
    public Button buttonMoveToSignIn;
    public Button buttonMoveRegister;
    public GameObject LoginForm;
    public GameObject RegisterForm;

    public void SwitchForm()
    {
        LoginForm.SetActive(!LoginForm.activeSelf);
        RegisterForm.SetActive(!RegisterForm.activeSelf);
    }

    public void GoForgot()
    {
        SceneManager.LoadScene("ForgotPassword");
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

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashBytes); 
            }
        }
    } 
}

[System.Serializable]
public class UserProfileData
{
    public string Name;
    public string Email;
    public string Password;
}

[System.Serializable]
public class LoginLog
{
    public string Name;       
    public string Email1;      
    public string LoginTime;
}
