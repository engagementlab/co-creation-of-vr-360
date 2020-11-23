using UnityEngine;
using UnityEngine.SceneManagement;

public class JustLoadNextScene : MonoBehaviour
{
    private void Awake() {
        SceneManager.LoadScene(1);
    }
}
