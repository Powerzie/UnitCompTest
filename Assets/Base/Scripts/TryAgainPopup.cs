using UnityEngine;
using UnityEngine.SceneManagement;

public class TryAgainPopup : MonoBehaviour
{
    void Awake()
    {
        if (GetComponent<Popup>() == null)
            gameObject.AddComponent<Popup>();
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
