using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeManager : MonoBehaviour
{
    void Update()
    {
        // Check if the ESC key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Load the main menu scene (replace "MainMenu" with your actual scene name)
            SceneManager.LoadScene("Main_Menu");
        }
    }
}
