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

public class ForgetPass : MonoBehaviour
{
    public InputField InputEmail;
    public TMP_Text notificationText;
    public Button buttonNext;
    public Button buttonBack;

    private FirebaseAuth auth;

    public void ForgetPassword()
    {
        string email = InputEmail.text;

        if (string.IsNullOrEmpty(email))
        {
            UpdateNotification("Please enter your email address.");
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                UpdateNotification("Password reset request was canceled.");
                Debug.Log("Password reset request was canceled.");
                return;
            }
            else if (task.IsFaulted)
            {
                UpdateNotification("Error occurred: " + task.Exception?.Message);
                Debug.LogError("Error occurred: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                UpdateNotification("Password reset email sent successfully. Please check your inbox.");
                Debug.Log("Password reset email sent successfully.");
            }
        });
    }

    private void UpdateNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message; 
        }
        else
        {
            Debug.LogWarning("Notification Text UI is not assigned in the Inspector!");
        }
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        buttonNext.onClick.AddListener(ForgetPassword);
        buttonBack.onClick.AddListener(GoBackToLogin);
    }

    public void GoBackToLogin()
    {
        SceneManager.LoadScene("Login"); 
    }
}
