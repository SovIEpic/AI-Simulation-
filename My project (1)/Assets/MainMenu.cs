using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1); //open main scene
    }
    public void QuitGame()
    {
        Application.Quit(); //quit the game
    }
}
