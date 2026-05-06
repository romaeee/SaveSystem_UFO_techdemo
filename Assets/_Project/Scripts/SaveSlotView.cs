using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image screenshotImage;
    [SerializeField] private Image selectionImage;

    private Button button;
    private int slotIndex;
    private SaveSlotsPanelController owner;
    private Sprite screenshotSprite;

    public int SlotIndex => slotIndex;

    public void Initialize(SaveSlotsPanelController panelOwner, int index)
    {
        owner = panelOwner;
        slotIndex = index;

        FindReferencesIfNeeded();

        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Select);

        SetSelected(false);
    }

    public void SetInfo(bool hasSave, Sprite screenshot, string savedAtText)
    {
        FindReferencesIfNeeded();

        if (titleText != null)
        {
            titleText.text = $"Slot {slotIndex + 1}";
        }

        if (timeText != null)
        {
            timeText.text = hasSave ? savedAtText : "Empty";
        }

        if (screenshotImage != null)
        {
            if (screenshotSprite != null && screenshotSprite != screenshot)
            {
                Destroy(screenshotSprite.texture);
                Destroy(screenshotSprite);
            }

            screenshotSprite = screenshot;
            screenshotImage.sprite = screenshotSprite;
            screenshotImage.color = screenshotSprite != null ? Color.white : new Color(0f, 0f, 0f, 0.35f);
            screenshotImage.preserveAspect = true;
        }
    }

    public void SetSelected(bool isSelected)
    {
        FindReferencesIfNeeded();

        if (selectionImage != null)
        {
            selectionImage.color = isSelected
                ? new Color(0.35f, 0.75f, 1f, 1f)
                : Color.white;
        }
    }

    private void Select()
    {
        owner?.SelectSlot(this);
    }

    private void FindReferencesIfNeeded()
    {
        button ??= GetComponent<Button>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            string lowerName = text.gameObject.name.ToLowerInvariant();

            if (titleText == null && lowerName.Contains("title"))
            {
                titleText = text;
            }
            else if (timeText == null && lowerName.Contains("time"))
            {
                timeText = text;
            }
        }

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null)
            {
                continue;
            }

            if (screenshotImage == null && image.gameObject.name.ToLowerInvariant().Contains("image"))
            {
                screenshotImage = image;
            }
        }

        selectionImage ??= screenshotImage;
    }
}
