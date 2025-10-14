using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnPlay;
    public Button btnSettings;
    public Button btnCredits;
    public Button btnQuit;

    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("First Select")]
    public GameObject firstSelected; // set ke Btn_Play

    void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        btnPlay.onClick.AddListener(OnPlay);
        btnSettings.onClick.AddListener(() => TogglePanel(settingsPanel, true));
        btnCredits.onClick.AddListener(() => TogglePanel(creditsPanel, true));
        btnQuit.onClick.AddListener(OnQuit);
    }

    void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void Update()
    {
        // ESC untuk back dari panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel && settingsPanel.activeSelf) { TogglePanel(settingsPanel, false); return; }
            if (creditsPanel && creditsPanel.activeSelf)   { TogglePanel(creditsPanel, false); return; }
        }
        // Enter untuk click selected
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var sel = EventSystem.current.currentSelectedGameObject;
            if (sel != null)
            {
                var btn = sel.GetComponent<Button>();
                if (btn) btn.onClick.Invoke();
            }
        }
    }

    void TogglePanel(GameObject panel, bool show)
    {
        if (!panel) return;
        panel.SetActive(show);
        if (show) EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<Button>()?.gameObject);
        else EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void OnPlay()
    {
        // TODO: ganti "Game" dengan nama scene gameplay kamu
        SceneManager.LoadScene("Game");
    }

    void OnQuit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
