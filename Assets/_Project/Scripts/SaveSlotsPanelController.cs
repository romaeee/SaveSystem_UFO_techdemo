using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SaveSlotsPanelController : MonoBehaviour
{
    private const int SlotCount = 6;

    [SerializeField] private SaveLoadController saveLoadController;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Camera screenshotCamera;
    [SerializeField] private int screenshotWidth = 320;
    [SerializeField] private int screenshotHeight = 180;

    private readonly List<SaveSlotView> slots = new List<SaveSlotView>();
    private SaveSlotView selectedSlot;
    private float previousTimeScale = 1f;
    private bool isOpen;

    private string SlotsDirectory => Path.Combine(Application.persistentDataPath, "slots");

    private void Awake()
    {
        FindReferencesIfNeeded();
        BuildSlots();
        RefreshSlots();
        ClosePanel();
    }

    private void OnEnable()
    {
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveSelectedSlot);
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSelectedSlot);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteSelectedSlot);
        }
    }

    private void OnDisable()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(SaveSelectedSlot);
        }

        if (loadButton != null)
        {
            loadButton.onClick.RemoveListener(LoadSelectedSlot);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(DeleteSelectedSlot);
        }

        if (isOpen)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private void Update()
    {
        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    public void OpenPanel()
    {
        RefreshSlots();
        DeselectSlot();

        if (!isOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
        }

        SetPanelVisible(true);
    }

    public void ClosePanel()
    {
        if (isOpen)
        {
            Time.timeScale = previousTimeScale;
            isOpen = false;
        }

        SetPanelVisible(false);
    }

    public void TogglePanel()
    {
        if (isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    public void SelectSlot(SaveSlotView slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;
        selectedSlot?.SetSelected(true);
        UpdateButtons();
    }

    public void DeselectSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }

        UpdateButtons();
    }

    public void SaveSelectedSlot()
    {
        if (selectedSlot == null || saveLoadController == null)
        {
            return;
        }

        int slotIndex = selectedSlot.SlotIndex;
        Directory.CreateDirectory(SlotsDirectory);
        saveLoadController.SaveToFile(GetSlotSavePath(slotIndex));

        Texture2D screenshot = CaptureSceneScreenshot();

        if (screenshot != null)
        {
            File.WriteAllBytes(GetSlotScreenshotPath(slotIndex), screenshot.EncodeToPNG());
            Destroy(screenshot);
        }

        RefreshSlots();
        DeselectSlot();
    }

    public void LoadSelectedSlot()
    {
        if (selectedSlot == null || saveLoadController == null)
        {
            return;
        }

        string savePath = GetSlotSavePath(selectedSlot.SlotIndex);

        if (saveLoadController.LoadFromFile(savePath))
        {
            ClosePanel();
        }
    }

    public void DeleteSelectedSlot()
    {
        if (selectedSlot == null)
        {
            return;
        }

        string savePath = GetSlotSavePath(selectedSlot.SlotIndex);
        string screenshotPath = GetSlotScreenshotPath(selectedSlot.SlotIndex);

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        if (File.Exists(screenshotPath))
        {
            File.Delete(screenshotPath);
        }

        RefreshSlots();
        DeselectSlot();
    }

    public void RefreshPanelData()
    {
        RefreshSlots();
        DeselectSlot();
    }

    private void BuildSlots()
    {
        if (slotPrefab == null || slotsContainer == null)
        {
            return;
        }

        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        slots.Clear();

        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, slotsContainer);
            SaveSlotView slotView = slotObject.GetComponent<SaveSlotView>();

            if (slotView == null)
            {
                slotView = slotObject.AddComponent<SaveSlotView>();
            }

            slotView.Initialize(this, i);
            slots.Add(slotView);
        }
    }

    private void RefreshSlots()
    {
        Directory.CreateDirectory(SlotsDirectory);

        for (int i = 0; i < slots.Count; i++)
        {
            string savePath = GetSlotSavePath(i);
            string screenshotPath = GetSlotScreenshotPath(i);
            bool hasSave = File.Exists(savePath);
            Sprite screenshot = hasSave ? LoadScreenshotSprite(screenshotPath) : null;
            string savedAtText = hasSave ? File.GetLastWriteTime(savePath).ToString("g") : string.Empty;

            slots[i].SetInfo(hasSave, screenshot, savedAtText);
        }

        UpdateButtons();
    }

    private Texture2D CaptureSceneScreenshot()
    {
        Camera cameraToRender = screenshotCamera != null ? screenshotCamera : Camera.main;

        if (cameraToRender == null)
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        int width = Mathf.Max(1, screenshotWidth);
        int height = Mathf.Max(1, screenshotHeight);
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = cameraToRender.targetTexture;

        cameraToRender.targetTexture = renderTexture;
        cameraToRender.Render();
        RenderTexture.active = renderTexture;

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        cameraToRender.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(renderTexture);

        return screenshot;
    }

    private Sprite LoadScreenshotSprite(string screenshotPath)
    {
        if (!File.Exists(screenshotPath))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(screenshotPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelCanvasGroup == null)
        {
            gameObject.SetActive(isVisible);
            return;
        }

        panelCanvasGroup.alpha = isVisible ? 1f : 0f;
        panelCanvasGroup.interactable = isVisible;
        panelCanvasGroup.blocksRaycasts = isVisible;
    }

    private void UpdateButtons()
    {
        bool hasSelectedSlot = selectedSlot != null;
        bool hasSave = hasSelectedSlot && File.Exists(GetSlotSavePath(selectedSlot.SlotIndex));

        if (saveButton != null)
        {
            saveButton.interactable = hasSelectedSlot;
        }

        if (loadButton != null)
        {
            loadButton.interactable = hasSave;
        }

        if (deleteButton != null)
        {
            deleteButton.interactable = hasSave;
        }
    }

    private string GetSlotSavePath(int slotIndex)
    {
        return Path.Combine(SlotsDirectory, $"slot_{slotIndex + 1}.json");
    }

    private string GetSlotScreenshotPath(int slotIndex)
    {
        return Path.Combine(SlotsDirectory, $"slot_{slotIndex + 1}.png");
    }

    private void FindReferencesIfNeeded()
    {
        saveLoadController ??= FindAnyObjectByType<SaveLoadController>();
        panelCanvasGroup ??= GetComponent<CanvasGroup>();

        if (slotsContainer == null)
        {
            Transform foundContainer = transform.Find("SlotSavePanel/SlotsContainer");
            slotsContainer = foundContainer != null ? foundContainer : transform.Find("SlotsContainer");
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            string lowerName = button.gameObject.name.ToLowerInvariant();

            if (saveButton == null && lowerName.Contains("save"))
            {
                saveButton = button;
            }
            else if (loadButton == null && lowerName.Contains("load"))
            {
                loadButton = button;
            }
            else if (deleteButton == null && lowerName.Contains("delete"))
            {
                deleteButton = button;
            }
        }

        screenshotCamera ??= Camera.main;
    }
}
