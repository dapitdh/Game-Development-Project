using UnityEngine;
using UnityEngine.SceneManagement;

public class Sc_btnMainMenu : MonoBehaviour
{
    [Header("Nama scene tujuan (harus ada di Build Settings)")]
    [SerializeField] private string sceneName = "MainMenu";

    [Tooltip("Gunakan loading async (lebih halus, cocok jika ada loading screen)")]
    [SerializeField] private bool loadAsync = true;

    // Panggil fungsi ini dari OnClick() pada UI Button
    public void GoToMainMenu()
    {
        // Pastikan unpause bila sebelumnya game di-pause
        Time.timeScale = 1f;

        if (loadAsync)
        {
            StartCoroutine(LoadSceneAsync());
        }
        else
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    private System.Collections.IEnumerator LoadSceneAsync()
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        op.allowSceneActivation = true;

        // (Opsional) Anda bisa membaca op.progress untuk update progress bar
        while (!op.isDone)
            yield return null;
    }
}
