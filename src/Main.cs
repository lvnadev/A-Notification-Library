using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Collections;

namespace apoca.Notifications
{
    public class Notifications : MonoBehaviour
    {
        private static GameObject cameraCanvas;
        private static Text notificationText;
        private static List<NotificationItem> activeNotifications = new List<NotificationItem>();
        private static Notifications instance;

        [System.Serializable]
        public class NotificationItem
        {
            public string message;
            public Color color;
            public float remainingTime;
            public bool isPermanent;

            public NotificationItem(string msg, Color col, float duration)
            {
                message = msg;
                color = col;
                remainingTime = duration;
                isPermanent = duration <= 0;
            }
        }

        private static void EnsureInstance()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("NotificationManager");
                instance = go.AddComponent<Notifications>();
                DontDestroyOnLoad(go);
                instance.StartCoroutine(instance.UpdateNotifications());
            }
        }

        public static void DestroyCameraCanvas()
        {
            if (cameraCanvas != null)
            {
                UnityEngine.Object.Destroy(cameraCanvas);
                cameraCanvas = null;
            }
        }

        public static void UpdateCameraCanvasAlternative()
        {
            if (cameraCanvas == null)
            {
                CreateCameraCanvasAlternative();
            }

            if (cameraCanvas != null && Camera.main != null)
            {
                Vector3 cameraPos = Camera.main.transform.position;
                Vector3 cameraForward = Camera.main.transform.forward;
                Vector3 cameraRight = Camera.main.transform.right;
                Vector3 cameraUp = Camera.main.transform.up;
                cameraCanvas.transform.position = cameraPos + cameraForward * 1.5f + cameraUp * -0.2f + cameraRight * -0.1f;
                cameraCanvas.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void Update()
        {
            UpdateCameraCanvasAlternative();
        }

        private static void CreateCameraCanvasAlternative()
        {
            cameraCanvas = new GameObject("CameraFollowCanvas");
            Shader textshader = Shader.Find("GUI/Text Shader");
            Material newMat = new Material(textshader);
            Canvas canvas = cameraCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
            cameraCanvas.layer = LayerMask.NameToLayer("UI");
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000, 600);
            canvasRect.localScale = Vector3.one * 0.0015f;

            GameObject textObj = new GameObject("NotificationText");
            textObj.transform.SetParent(cameraCanvas.transform, false);
            notificationText = textObj.AddComponent<UnityEngine.UI.Text>();
            notificationText.text = "";
            notificationText.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 16); // u can change the font if u want. utopium does not work well because its not dynamic
            notificationText.fontSize = 36;
            notificationText.material = newMat;
            notificationText.color = UnityEngine.Color.cyan;
            notificationText.alignment = TextAnchor.LowerLeft;
            notificationText.raycastTarget = false;
            notificationText.fontStyle = FontStyle.Bold;
            RectTransform textRect = notificationText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(20, 20); 
            textRect.offsetMax = new Vector2(-20, -20);
            UnityEngine.UI.Shadow shadow = textObj.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = UnityEngine.Color.black;
            shadow.effectDistance = new Vector2(6, -6);

            UnityEngine.Object.DontDestroyOnLoad(cameraCanvas);
        }

        public static void SendNotification(string message, Color color, float duration = 5f)
        {
            EnsureInstance();

            if (cameraCanvas == null)
            {
                CreateCameraCanvasAlternative();
            }

            NotificationItem newNotification = new NotificationItem(message, color, duration);
            activeNotifications.Add(newNotification);

            UpdateNotificationDisplay();
        }

        /// <summary>
        /// send a notification with default color
        /// </summary>
        public static void SendNotification(string message, float duration = 5f)
        {
            SendNotification(message, Color.white, duration);
        }

        /// <summary>
        /// send a permanent notification (won't auto-disappear)
        /// </summary>
        public static void SendPermanentNotification(string message, Color color)
        {
            SendNotification(message, color, 0f);
        }

        /// <summary>
        /// clear all notifications
        /// </summary>
        public static void ClearAllNotifications()
        {
            activeNotifications.Clear();
            UpdateNotificationDisplay();
        }

        /// <summary>
        /// clear the oldest notification
        /// </summary>
        public static void ClearOldestNotification()
        {
            if (activeNotifications.Count > 0)
            {
                activeNotifications.RemoveAt(0);
                UpdateNotificationDisplay();
            }
        }

        /// <summary>
        /// clear the newest notification (one at top)
        /// </summary>
        public static void ClearNewestNotification()
        {
            if (activeNotifications.Count > 0)
            {
                activeNotifications.RemoveAt(activeNotifications.Count - 1);
                UpdateNotificationDisplay();
            }
        }

        private static void UpdateNotificationDisplay()
        {
            if (notificationText == null) return;

            if (activeNotifications.Count == 0)
            {
                notificationText.text = "";
                return;
            }

            System.Text.StringBuilder displayText = new System.Text.StringBuilder();

            for (int i = 0; i < activeNotifications.Count; i++)
            {
                var notification = activeNotifications[i];
                displayText.AppendLine(notification.message);
            }

            notificationText.text = displayText.ToString().TrimEnd();

            if (activeNotifications.Count > 0)
            {
                notificationText.color = activeNotifications[activeNotifications.Count - 1].color;
            }
        }

        private IEnumerator UpdateNotifications()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                bool needsUpdate = false;

                // Update timers and remove expired notifications
                for (int i = activeNotifications.Count - 1; i >= 0; i--)
                {
                    var notification = activeNotifications[i];

                    if (!notification.isPermanent)
                    {
                        notification.remainingTime -= 0.1f;

                        if (notification.remainingTime <= 0)
                        {
                            activeNotifications.RemoveAt(i);
                            needsUpdate = true;
                        }
                    }
                }

                if (needsUpdate)
                {
                    UpdateNotificationDisplay();
                }

                UpdateCameraCanvasAlternative();
            }
        }

        /// <summary>
        /// get the number of notifications currently on the screen
        /// </summary>
        public static int GetNotificationCount()
        {
            return activeNotifications.Count;
        }

        /// <summary>
        /// check if there is any active notifications
        /// </summary>
        public static bool HasNotifications()
        {
            return activeNotifications.Count > 0;
        }
    }
}
