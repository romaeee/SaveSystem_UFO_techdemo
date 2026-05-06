using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSessionController : MonoBehaviour
{
    [SerializeField] private SaveLoadController saveLoadController;
    [SerializeField] private SaveSlotsPanelController saveSlotsPanelController;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button restoreButton;
    [SerializeField] private bool autoFindButtons = true;

    private void Awake()
    {
        FindReferencesIfNeeded();
    }

    private void OnEnable()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (restoreButton != null)
        {
            restoreButton.onClick.AddListener(RestoreSaves);
        }
    }

    private void OnDisable()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (restoreButton != null)
        {
            restoreButton.onClick.RemoveListener(RestoreSaves);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    public void RestoreSaves()
    {
        Time.timeScale = 1f;
        DeleteAllSaves();

        if (saveSlotsPanelController != null)
        {
            saveSlotsPanelController.RefreshPanelData();
            saveSlotsPanelController.ClosePanel();
        }
    }

    private void DeleteAllSaves()
    {
        if (saveLoadController != null)
        {
            DeleteFileIfExists(saveLoadController.SavePath);
            DeleteFileIfExists(saveLoadController.AutosavePath);
        }

        DeleteDirectoryIfExists(Path.Combine(Application.persistentDataPath, "slots"));
    }

    private void DeleteFileIfExists(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void DeleteDirectoryIfExists(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private void FindReferencesIfNeeded()
    {
        saveLoadController ??= FindAnyObjectByType<SaveLoadController>();
        saveSlotsPanelController ??= FindAnyObjectByType<SaveSlotsPanelController>(FindObjectsInactive.Include);

        if (!autoFindButtons)
        {
            return;
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);

        foreach (Button button in buttons)
        {
            string lowerName = button.gameObject.name.ToLowerInvariant();

            if (restartButton == null && lowerName.Contains("restart"))
            {
                restartButton = button;
            }
            else if (restoreButton == null && lowerName.Contains("restore"))
            {
                restoreButton = button;
            }
        }
    }
}
