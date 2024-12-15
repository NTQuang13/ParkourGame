using UnityEngine;
using UnityEngine.UI; // Nếu sử dụng TextMeshPro, dùng TMPro
using TMPro;
//using UnityEditor.VersionControl;
using System.Collections.Generic;

public class ChatBox : MonoBehaviour
{
    public TMP_InputField inputField; // Hộp nhập liệu
    public Transform chatContent; // Vị trí chứa tin nhắn (Content trong Scroll View)
    public GameObject messagePrefab; // Prefab tin nhắn (Text hoặc TMP_Text)
    public NetworkCommunicator communicator;
    private List<GameObject> messages = new List<GameObject>(); // Danh sách tin nhắn
    private int maxMessages = 4; // Số lượng tin nhắn tối đa
    private bool isInputActive = false; // Kiểm tra trạng thái nhập liệu

    void Update()
    {
        // Nhấn Enter để bật hoặc tắt chế độ nhập
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isInputActive)
            {   
                ShowMessage();
            }
            else
            {
                ActivateInputField();
            }
        }
    }
    // Bật chế độ nhập liệu
    private void ActivateInputField()
    {
        inputField.gameObject.SetActive(true);
        inputField.ActivateInputField(); // Đưa con trỏ vào InputField
        isInputActive = true;
    }
    public GameObject SpawnMessage(string messageText)
    {
        // Instantiate prefab
        GameObject newMessage = Instantiate(messagePrefab, chatContent);

        // Set the message text
        TMP_Text messageComponent = newMessage.GetComponent<TMP_Text>();
        if (messageComponent != null)
        {
            messageComponent.text = messageText;
        }
        else
        {
            Debug.LogWarning("Message prefab does not have a TMP_Text component.");
        }
        return newMessage;
    }
    // Gửi tin nhắn
    private void ShowMessage()
    {
        string messageText = inputField.text.Trim();
        if (!string.IsNullOrEmpty(messageText))
        {
            // Tạo một tin nhắn mới
            /*GameObject newMessage = Instantiate(messagePrefab, chatContent);
            TMP_Text messageTMP = newMessage.GetComponent<TMP_Text>();*/
            messages.Add(SpawnMessage(messageText));
            communicator.ClientSpeak(messageText);
            /*if (messageTMP != null)
            {
                messageTMP.text = messageText;
            }
            */  
            // Xóa nội dung InputField
            inputField.text = "";
        }
        if (messages.Count > maxMessages)
        {
            RemoveOldestMessage();
        }
        // Tắt chế độ nhập liệu
        inputField.DeactivateInputField();
        inputField.gameObject.SetActive(false);
        isInputActive = false;
    }
    public void ShowMessage(string messageText)
    {
        
        if (!string.IsNullOrEmpty(messageText))
        {
            // Tạo một tin nhắn mới
            /*GameObject newMessage = Instantiate(messagePrefab, chatContent);
            TMP_Text messageTMP = newMessage.GetComponent<TMP_Text>();*/
            messages.Add(SpawnMessage(messageText));
            /*if (messageTMP != null)
            {
                messageTMP.text = messageText;
            }
            */
            // Xóa nội dung InputField
            inputField.text = "";
        }
        if (messages.Count > maxMessages)
        {
            RemoveOldestMessage();
        }
        // Tắt chế độ nhập liệu
        inputField.DeactivateInputField();
        inputField.gameObject.SetActive(false);
        isInputActive = false;
    }
    private void RemoveOldestMessage()
    {
        if (messages.Count > 0)
        {
            // Lấy tin nhắn cũ nhất
            GameObject oldestMessage = messages[0];
            // Xóa khỏi danh sách và phá hủy GameObject
            messages.RemoveAt(0);
            Destroy(oldestMessage);
        }
    }
}
