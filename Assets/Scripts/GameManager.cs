using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void MenuClicked()
    {
        SceneManager.LoadScene(0);
    }
}
