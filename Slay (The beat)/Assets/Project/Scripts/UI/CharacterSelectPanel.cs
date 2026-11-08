using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The character select screen in song select, shown after picking a song and before picking a difficulty.
/// </summary>
/// <remarks>
/// Works like the song cover carousel: dancer cards are copies of the song card, sit in the same slots,
/// pop in from the centre outward, dim on the sides, and the highlighted dancer's big picture and name
/// slide in where the song's character and title usually are.
/// Lives in the SongSelection scene, so its layout is edited there. Song select fades it with its canvas group.
/// </remarks>
public class CharacterSelectPanel : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("Fades the whole panel in and out")]
    public CanvasGroup canvasGroup;
    [Tooltip("Big picture of the highlighted dancer")]
    public Image portraitDisplay;
    [Tooltip("Name of the highlighted dancer")]
    public TextMeshProUGUI nameText;

    [Header("Cards")]
    [Tooltip("Copied for every dancer. Use the song card prefab so dancers look like song covers")]
    public GameObject cardPrefab;
    [Tooltip("Where the cards are created")]
    public RectTransform cardParent;
    [Tooltip("Card positions from left to right. The middle one is the selected card and the two ends are hidden, like the song carousel")]
    public RectTransform[] slots;

    [Header("Motion")]
    [Tooltip("How fast cards slide to their slots")]
    public float moveSpeed = 10f;
    [Tooltip("Brightness of the cards on the sides")]
    [Range(0f, 1f)] public float sideBrightness = 0.35f;
    [Tooltip("Brightness of the other cards once a dancer is confirmed")]
    [Range(0f, 1f)] public float confirmedOthersBrightness = 0.15f;
    [Tooltip("Size kick on the selected card when the selection moves")]
    public float movePunch = 0.08f;
    [Tooltip("Size kick on the selected card when it's confirmed")]
    public float confirmPunch = 0.15f;
    [Tooltip("How much the selected card grows and shrinks. 0 keeps it still")]
    public float breathAmount = 0.015f;
    public float breathSpeed = 2.4f;

    [Header("Pop In")]
    [Tooltip("How long each card takes to pop in")]
    public float introDuration = 0.5f;
    [Tooltip("Delay between each step out from the centre")]
    public float introStagger = 0.07f;
    [Tooltip("How far the big picture slides in when the selection changes")]
    public float portraitSlideDistance = 90f;
    public float portraitPopDuration = 0.54f;
    [Tooltip("How far the name slides in when the selection changes")]
    public float nameSlideDistance = 60f;
    public float namePopDuration = 0.45f;

    private const float PunchDecay = 10f;

    private class Card
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector3 originalScale;
        public Vector3 baseScale;
        public float punch;
        public int slot = -1;
        public float brightness = -1f;
        public Graphic[] graphics;
        public Color[] colors;
    }

    private readonly List<Card> cards = new List<Card>();
    private readonly List<DancerData> dancers = new List<DancerData>();
    private int selected;
    private bool confirmed;
    private float introStart = -1f;

    private Vector2 portraitRestPosition;
    private Vector3 portraitRestScale = Vector3.one;
    private Vector2 nameRestPosition;
    private Vector3 nameRestScale = Vector3.one;
    private Coroutine portraitRoutine;
    private Coroutine nameRoutine;

    private int CenterSlot => slots.Length / 2;

    /// <summary>The highlighted dancer, or null when there are none.</summary>
    public DancerData Selected => dancers.Count > 0 ? dancers[selected] : null;

    /// <summary>How many dancers there are to pick from.</summary>
    public int Count => dancers.Count;

    void Awake()
    {
        if (portraitDisplay != null)
        {
            portraitRestPosition = portraitDisplay.rectTransform.anchoredPosition;
            portraitRestScale = portraitDisplay.rectTransform.localScale;
        }

        if (nameText != null)
        {
            nameRestPosition = nameText.rectTransform.anchoredPosition;
            nameRestScale = nameText.rectTransform.localScale;
        }
    }

    /// <summary>Makes a card for every dancer, highlights one, and pops the cards in.</summary>
    public void Show(List<DancerData> newDancers, int selectedIndex)
    {
        dancers.Clear();
        dancers.AddRange(newDancers);
        while (cards.Count < dancers.Count) cards.Add(MakeCard());

        for (int i = 0; i < cards.Count; i++)
        {
            bool used = i < dancers.Count;
            cards[i].rect.gameObject.SetActive(used);
            if (used) SetCardPicture(cards[i], dancers[i]);
        }

        selected = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, dancers.Count - 1));
        confirmed = false;
        introStart = Time.time;
        SnapCards();
        ShowSelected(0, true);
    }

    /// <summary>Moves the highlight by one card, wrapping around at the ends.</summary>
    public void Move(int dir)
    {
        if (dancers.Count <= 1) return;

        selected = (selected + dir + dancers.Count) % dancers.Count;
        cards[selected].punch = movePunch;
        ShowSelected(dir, false);
        ApplyDepthOrder();
    }

    /// <summary>Kicks the selected card to show it's been chosen, and dims the rest.</summary>
    public void Confirm()
    {
        if (dancers.Count == 0) return;
        cards[selected].punch = confirmPunch;
        confirmed = true;
    }

    /// <summary>Clears the confirmed look, for coming back to this screen.</summary>
    public void ClearConfirm()
    {
        confirmed = false;
    }

    void Update()
    {
        if (dancers.Count == 0 || slots == null || slots.Length == 0) return;

        float move = Damp(moveSpeed);
        float punchSettle = Damp(PunchDecay);
        float fade = Time.deltaTime * 5f;
        float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount;

        for (int i = 0; i < dancers.Count; i++)
        {
            Card card = cards[i];
            int slot = GetTargetSlot(i);
            float intro = GetIntro(slot);

            if (!IsSlotValid(slot))
            {
                card.slot = -1;
                card.group.alpha = Mathf.MoveTowards(card.group.alpha, 0f, fade);
                continue;
            }

            Vector3 targetScale = card.originalScale * slots[slot].localScale.x;

            // A card wrapping around the edge would fly across the whole row; jump it while it's invisible instead.
            bool wrapped = card.slot < 0 || Mathf.Abs(card.slot - slot) > 1;
            if (wrapped && card.group.alpha < 0.05f)
            {
                card.rect.position = slots[slot].position;
                card.baseScale = targetScale;
            }
            else
            {
                card.rect.position = Vector3.Lerp(card.rect.position, slots[slot].position, move);
                card.baseScale = Vector3.Lerp(card.baseScale, targetScale, move);
            }
            card.slot = slot;

            card.punch = Mathf.Lerp(card.punch, 0f, punchSettle);
            float extra = 1f + card.punch + (slot == CenterSlot ? breath : 0f);
            card.rect.localScale = card.baseScale * extra * Mathf.LerpUnclamped(0.7f, 1f, EaseOutBack(intro));

            float targetAlpha = IsEdgeSlot(slot) ? 0f : 1f;
            card.group.alpha = intro < 1f ? targetAlpha * EaseOutCubic(intro) : Mathf.MoveTowards(card.group.alpha, targetAlpha, fade);

            float targetBrightness = slot == CenterSlot ? 1f : confirmed ? confirmedOthersBrightness : sideBrightness;
            SetBrightness(card, Mathf.MoveTowards(card.brightness, targetBrightness, fade));
        }
    }

    private Card MakeCard()
    {
        GameObject go = Instantiate(cardPrefab, cardParent != null ? cardParent : (RectTransform)transform);
        go.name = "Dancer Card " + cards.Count;

        Card card = new Card
        {
            rect = go.GetComponent<RectTransform>(),
            group = go.TryGetComponent(out CanvasGroup group) ? group : go.AddComponent<CanvasGroup>(),
            graphics = go.GetComponentsInChildren<Graphic>(true),
        };
        card.originalScale = card.rect.localScale;
        card.baseScale = card.originalScale;
        card.colors = new Color[card.graphics.Length];
        for (int g = 0; g < card.graphics.Length; g++) card.colors[g] = card.graphics[g].color;

        foreach (Graphic graphic in card.graphics) graphic.raycastTarget = false;
        return card;
    }

    /// <summary>Puts the dancer's card picture on the card, or its portrait if it has no card picture.</summary>
    private static void SetCardPicture(Card card, DancerData dancer)
    {
        SongBlock block = card.rect.GetComponent<SongBlock>();
        Image image = block != null && block.jacketDisplay != null ? block.jacketDisplay : card.rect.GetComponentInChildren<Image>(true);
        if (image == null) return;

        Sprite picture = dancer.cardImage != null ? dancer.cardImage : dancer.portrait;
        if (picture == null) return; // Keep the card's own picture.

        image.sprite = picture;
        // Portraits aren't square like covers, so keep their shape.
        image.preserveAspect = dancer.cardImage == null;
    }

    /// <summary>Places every card in its slot straight away, hidden, ready to pop in.</summary>
    private void SnapCards()
    {
        for (int i = 0; i < dancers.Count; i++)
        {
            Card card = cards[i];
            int slot = GetTargetSlot(i);
            card.punch = 0f;
            card.group.alpha = 0f;
            card.slot = IsSlotValid(slot) ? slot : -1;
            if (card.slot < 0) continue;

            card.rect.position = slots[slot].position;
            card.baseScale = card.originalScale * slots[slot].localScale.x;
            card.rect.localScale = card.baseScale * 0.7f;
            SetBrightness(card, slot == CenterSlot ? 1f : sideBrightness);
        }
        ApplyDepthOrder();
    }

    /// <summary>Shows the highlighted dancer's picture and name, sliding in from the side it came from.</summary>
    private void ShowSelected(int dir, bool opening)
    {
        DancerData dancer = Selected;
        if (dancer == null) return;

        if (portraitDisplay != null)
        {
            portraitDisplay.sprite = dancer.portrait;
            portraitDisplay.enabled = dancer.portrait != null;
            Vector2 from = opening ? new Vector2(0f, -portraitSlideDistance * 0.5f) : new Vector2(dir * portraitSlideDistance, 0f);
            Restart(ref portraitRoutine, PopIn(portraitDisplay.rectTransform, portraitDisplay, portraitRestPosition, portraitRestScale, from, 0.96f, portraitPopDuration));
        }

        if (nameText != null)
        {
            nameText.text = dancer.displayName;
            Restart(ref nameRoutine, PopIn(nameText.rectTransform, nameText, nameRestPosition, nameRestScale, new Vector2(dir * nameSlideDistance, 0f), 1.08f, namePopDuration));
        }
    }

    private void Restart(ref Coroutine routine, IEnumerator next)
    {
        if (routine != null) StopCoroutine(routine);
        routine = isActiveAndEnabled ? StartCoroutine(next) : null;
    }

    /// <summary>Slides, scales, and fades a graphic into its rest state, like the song screen does.</summary>
    private static IEnumerator PopIn(RectTransform rt, Graphic graphic, Vector2 restPos, Vector3 restScale, Vector2 fromOffset, float fromScale, float duration)
    {
        Color color = graphic.color;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(t);
            rt.anchoredPosition = restPos + fromOffset * (1f - eased);
            rt.localScale = restScale * Mathf.Lerp(fromScale, 1f, eased);
            color.a = EaseOutCubic(Mathf.Clamp01(t * 1.6f));
            graphic.color = color;
            yield return null;
        }

        rt.anchoredPosition = restPos;
        rt.localScale = restScale;
        color.a = 1f;
        graphic.color = color;
    }

    /// <summary>Darkens a card by scaling the colour of its graphics, keeping their alpha.</summary>
    private static void SetBrightness(Card card, float brightness)
    {
        if (Mathf.Approximately(card.brightness, brightness)) return;
        card.brightness = brightness;

        for (int g = 0; g < card.graphics.Length; g++)
        {
            if (card.graphics[g] == null) continue;
            Color c = card.colors[g];
            card.graphics[g].color = new Color(c.r * brightness, c.g * brightness, c.b * brightness, c.a);
        }
    }

    /// <summary>Draws the selected card on top, with its neighbours layered under it.</summary>
    private void ApplyDepthOrder()
    {
        List<int> order = new List<int>();
        for (int i = 0; i < dancers.Count; i++) order.Add(i);
        order.Sort((a, b) => SlotDistance(b).CompareTo(SlotDistance(a)));
        foreach (int index in order) cards[index].rect.SetAsLastSibling();
    }

    private int SlotDistance(int index)
    {
        int slot = GetTargetSlot(index);
        return IsSlotValid(slot) ? Mathf.Abs(slot - CenterSlot) : int.MaxValue;
    }

    /// <summary>How far a slot's card is through popping in, from 0 to 1. Cards nearer the centre pop first.</summary>
    private float GetIntro(int slot)
    {
        if (introStart < 0f) return 1f;
        float delay = IsSlotValid(slot) ? Mathf.Abs(slot - CenterSlot) * introStagger : 0f;
        return Mathf.Clamp01((Time.time - introStart - delay) / introDuration);
    }

    /// <summary>The slot a card belongs in, which may be off the row for cards far from the selection.</summary>
    private int GetTargetSlot(int index)
    {
        int count = dancers.Count;
        int diff = index - selected;
        if (diff > count / 2) diff -= count;
        if (diff <= -count / 2) diff += count;
        return diff + CenterSlot;
    }

    private bool IsSlotValid(int slot) => slot >= 0 && slot < slots.Length;
    private bool IsEdgeSlot(int slot) => slot == 0 || slot == slots.Length - 1;

    private static float Damp(float speed) => 1f - Mathf.Exp(-speed * Time.deltaTime);
    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
