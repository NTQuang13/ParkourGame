using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

public class Spawning : MonoBehaviour
{

    public GameObject OtherPlayerprefab; // Assign your prefab in the Inspector
    public GameObject Player;
    public List<GameObject> spawnedObjects = new List<GameObject>();

    // Method to spawn the prefab
    public int SpawnPrefab(string objectName)
    {
        // Spawn the prefab at a specific position and rotation
        GameObject newObject = Instantiate(OtherPlayerprefab, this.transform.position, Quaternion.identity);

        // Assign a name to the spawned object
        newObject.name = objectName;

        // Add the spawned object to the list
        spawnedObjects.Add(newObject);
        return spawnedObjects.Count;
    }
    public int AddPlayer()
    {
        spawnedObjects.Add(Player);
        return spawnedObjects.Count;
    }
    // Example: Accessing the spawned objects later
    public void AccessSpawnedObjects()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            Debug.Log($"Object Name: {obj.name}");
        }
    }

    public bool DeleteObjectByName(string objectName)
    {
        // Find the object in the list by name
        GameObject objectToDelete = spawnedObjects.Find(obj => obj.name == objectName);

        if (objectToDelete != null)
        {
            // Remove it from the list
            spawnedObjects.Remove(objectToDelete);

            // Destroy the GameObject
            Destroy(objectToDelete);

            Debug.Log($"Object '{objectName}' deleted.");
            return true;
        }
        else
        {
            Debug.LogWarning($"Object '{objectName}' not found.");
            return false;
        }
    }
    public int GetPlayerIndex()
    {
        // Tìm index của Player trong danh sách spawnedObjects
        int index = spawnedObjects.IndexOf(Player);

        if (index != -1)
        {
            Debug.Log($"Player found at index {index}.");
        }
        else
        {
            Debug.LogWarning("Player is not in the list.");
        }
        
        return index; // Trả về -1 nếu không tìm thấy
    }
    public void SetWinner(int index)
    {
        // Kiểm tra xem index có hợp lệ không
        if (index >= 0 && index < spawnedObjects.Count)
        {
            // Lấy GameObject từ danh sách
            GameObject winnerObject = spawnedObjects[index];
            Text text = winnerObject.GetComponent<Text>();
            //text.text = $"{winnerObject.name}+ is winner";
            // Kiểm tra xem GameObject có Renderer không
            Renderer objectRenderer = winnerObject.GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                // Đổi màu của GameObject thành đỏ
                objectRenderer.material.color = Color.red;
                Debug.Log($"Object at index {index} is now the winner and turned red.");
            }
            else
            {
                Debug.LogWarning($"Object at index {index} does not have a Renderer component.");
            }
        }
        else
        {
            Debug.LogError("Invalid index provided. Index is out of range.");
        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
