using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelPopup : MonoBehaviour
{
    [SerializeField] GameObject nextButton;
    [SerializeField] GameObject youWonLabel;

    void Awake()
    {
        if (GetComponent<Popup>() == null)
            gameObject.AddComponent<Popup>();
    }

    void Start()
    {
        GameFx.PlayConfetti(transform as RectTransform);
    }

    public void Show(bool finalWin)
    {
        if (!finalWin)
            return;

        if (nextButton != null)
            nextButton.SetActive(false);
        if (youWonLabel != null)
            youWonLabel.SetActive(true);
    }

    public void Restart()
    {
        LevelProgress.Advance();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
