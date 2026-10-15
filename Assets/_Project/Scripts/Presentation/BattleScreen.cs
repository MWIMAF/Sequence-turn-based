using System.Collections.Generic;
using Game.Combat;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Builds the test battle layout in code and holds references to its widgets.
    /// No logic: BattleController decides what each widget shows.
    /// </summary>
    /// <remarks>
    /// Layout is authored for a 1920x1080 reference canvas; CanvasScaler scales it to the window.
    /// </remarks>
    public sealed class BattleScreen
    {
        private const float MainCenterX = -240f;
        private const float SlotSpacing = 260f;
        private static readonly Vector2 SlotSize = new Vector2(220f, 150f);

        public RectTransform Root { get; private set; }
        public RectTransform FxLayer { get; private set; }

        public Text PhaseText { get; private set; }
        public Text SeedText { get; private set; }
        public Text Announcement { get; private set; }
        public Text AnnouncementSub { get; private set; }
        public Text BudgetText { get; private set; }
        public Text HintText { get; private set; }
        public Text EnemyIntelText { get; private set; }
        public Text LogText { get; private set; }

        public Button IntelButton { get; private set; }
        public Text IntelLabel { get; private set; }
        public Button SpeedButton { get; private set; }
        public Text SpeedLabel { get; private set; }
        public Button RestartButton { get; private set; }
        public Button EnemyButton { get; private set; }
        public Text EnemyLabel { get; private set; }
        public Button LockInButton { get; private set; }
        public Button ClearButton { get; private set; }

        public CombatantPanel PlayerPanel { get; private set; }
        public CombatantPanel EnemyPanel { get; private set; }
        public SlotCard[] PlayerSlots { get; private set; }
        public SlotCard[] EnemySlots { get; private set; }
        public List<MoveButton> MoveButtons { get; } = new List<MoveButton>();

        public GameObject Overlay { get; private set; }
        public Text OverlayTitle { get; private set; }
        public Text OverlaySub { get; private set; }
        public Button OverlayButton { get; private set; }

        public static BattleScreen Build(Transform host, int slotCount, IReadOnlyList<LoadoutAction> playerActions)
        {
            var screen = new BattleScreen();
            screen.Root = CreateCanvas(host);

            var background = UiFactory.Panel("Background", screen.Root, UiPalette.Background, rounded: false);
            UiFactory.Stretch(background.rectTransform);

            screen.BuildHeader();
            screen.EnemyPanel = CombatantPanel.Create(screen.Root, "EnemyPanel", new Vector2(MainCenterX, 395f),
                new Vector2(900f, 100f), UiPalette.Hp, showStamina: false);
            screen.EnemySlots = BuildSlotRow(screen.Root, "EnemySlot", slotCount, 195f);
            screen.BuildCentre();
            screen.PlayerSlots = BuildSlotRow(screen.Root, "PlayerSlot", slotCount, -170f);
            screen.PlayerPanel = CombatantPanel.Create(screen.Root, "PlayerPanel", new Vector2(MainCenterX, -325f),
                new Vector2(900f, 100f), UiPalette.Accent, showStamina: true);
            screen.BuildSideControls();
            screen.BuildHand(playerActions);
            screen.BuildLog();
            screen.BuildOverlay();

            screen.FxLayer = UiFactory.Stretch(UiFactory.Rect("FX", screen.Root));
            return screen;
        }

        /// <summary>Shown instead of the battle when the controller is missing its data assets.</summary>
        public static void BuildError(Transform host, string message)
        {
            var root = CreateCanvas(host);
            var background = UiFactory.Panel("Background", root, UiPalette.Background, rounded: false);
            UiFactory.Stretch(background.rectTransform);
            var text = UiFactory.Label("Error", root, message, 30, UiPalette.Bad);
            UiFactory.Place(text.rectTransform, Vector2.zero, new Vector2(1400f, 600f));
        }

        public static float SlotX(int slot, int slotCount) => MainCenterX + (slot - (slotCount - 1) / 2f) * SlotSpacing;

        private static RectTransform CreateCanvas(Transform host)
        {
            var go = new GameObject("BattleCanvas", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(host, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return UiFactory.Stretch(UiFactory.Rect("Screen", go.transform));
        }

        private static SlotCard[] BuildSlotRow(Transform parent, string prefix, int slotCount, float y)
        {
            var cards = new SlotCard[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                cards[i] = SlotCard.Create(parent, $"{prefix}{i + 1}", new Vector2(SlotX(i, slotCount), y), SlotSize,
                    $"SLOT {i + 1}");
            }

            return cards;
        }

        private void BuildHeader()
        {
            var bar = UiFactory.Panel("Header", Root, UiPalette.Panel, rounded: false);
            UiFactory.Place(bar.rectTransform, new Vector2(0f, 505f), new Vector2(1920f, 70f));

            var title = UiFactory.Label("Title", Root, "SEQUENCE DUEL", 30, UiPalette.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(title.rectTransform, new Vector2(-750f, 505f), new Vector2(380f, 50f));

            PhaseText = UiFactory.Label("Phase", Root, string.Empty, 24, UiPalette.Highlight, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(PhaseText.rectTransform, new Vector2(-370f, 505f), new Vector2(420f, 50f));

            EnemyButton = UiFactory.Button("EnemyButton", Root, string.Empty, UiPalette.PanelLight, 18, out var enemy);
            EnemyLabel = enemy;
            UiFactory.Place((RectTransform)EnemyButton.transform, new Vector2(-15f, 505f), new Vector2(270f, 46f));

            SeedText = UiFactory.Label("Seed", Root, string.Empty, 16, UiPalette.TextDim, TextAnchor.MiddleRight);
            UiFactory.Place(SeedText.rectTransform, new Vector2(265f, 505f), new Vector2(200f, 40f));

            IntelButton = UiFactory.Button("IntelButton", Root, string.Empty, UiPalette.PanelLight, 18, out var intel);
            IntelLabel = intel;
            UiFactory.Place((RectTransform)IntelButton.transform, new Vector2(530f, 505f), new Vector2(290f, 46f));

            SpeedButton = UiFactory.Button("SpeedButton", Root, string.Empty, UiPalette.PanelLight, 18, out var speed);
            SpeedLabel = speed;
            UiFactory.Place((RectTransform)SpeedButton.transform, new Vector2(745f, 505f), new Vector2(120f, 46f));

            RestartButton = UiFactory.Button("RestartButton", Root, "Restart (R)", UiPalette.PanelLight, 18, out _);
            UiFactory.Place((RectTransform)RestartButton.transform, new Vector2(875f, 505f), new Vector2(120f, 46f));
        }

        private void BuildCentre()
        {
            Announcement = UiFactory.Label("Announcement", Root, string.Empty, 40, UiPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(Announcement.rectTransform, new Vector2(MainCenterX, 22f), new Vector2(1000f, 60f));
            UiFactory.AddShadow(Announcement);

            AnnouncementSub = UiFactory.Label("AnnouncementSub", Root, string.Empty, 22, UiPalette.TextDim);
            UiFactory.Place(AnnouncementSub.rectTransform, new Vector2(MainCenterX, -28f), new Vector2(1000f, 36f));
        }

        private void BuildSideControls()
        {
            EnemyIntelText = UiFactory.Label("EnemyIntel", Root, string.Empty, 18, UiPalette.TextDim, TextAnchor.MiddleLeft);
            UiFactory.Place(EnemyIntelText.rectTransform, new Vector2(305f, 195f), new Vector2(270f, 150f));

            BudgetText = UiFactory.Label("Budget", Root, string.Empty, 20, UiPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(BudgetText.rectTransform, new Vector2(300f, -72f), new Vector2(300f, 40f));

            LockInButton = UiFactory.Button("LockInButton", Root, "LOCK IN  (Enter)", UiPalette.Accent, 26, out _);
            UiFactory.Place((RectTransform)LockInButton.transform, new Vector2(300f, -150f), new Vector2(250f, 96f));

            ClearButton = UiFactory.Button("ClearButton", Root, "Clear slots", UiPalette.PanelLight, 18, out _);
            UiFactory.Place((RectTransform)ClearButton.transform, new Vector2(300f, -230f), new Vector2(250f, 44f));
        }

        private void BuildHand(IReadOnlyList<LoadoutAction> actions)
        {
            var size = new Vector2(172f, 104f);
            float spacing = 182f;
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                float x = MainCenterX + (i - (actions.Count - 1) / 2f) * spacing;
                string hotkey = i < 9 ? $"[{i + 1}]" : string.Empty;
                string caption = $"{BodyParts.ShortName(action.BodyPart)} · {action.Equipment.DisplayName}";
                MoveButtons.Add(MoveButton.Create(Root, new Vector2(x, -465f), size, action.Move, caption, hotkey));
            }

            HintText = UiFactory.Label("Hint", Root, string.Empty, 16, UiPalette.TextDim, TextAnchor.MiddleLeft);
            UiFactory.Place(HintText.rectTransform, new Vector2(375f, -465f), new Vector2(250f, 120f));
        }

        private void BuildLog()
        {
            var panel = UiFactory.Panel("LogPanel", Root, UiPalette.Panel);
            UiFactory.Place(panel.rectTransform, new Vector2(720f, -30f), new Vector2(400f, 930f));

            var title = UiFactory.Label("LogTitle", panel.transform, "COMBAT LOG", 18, UiPalette.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 440f), new Vector2(360f, 30f));

            // Newest lines sit at the bottom; older ones overflow upward and are clipped by the mask.
            var viewport = UiFactory.Rect("LogViewport", panel.transform);
            UiFactory.Place(viewport, new Vector2(0f, -15f), new Vector2(370f, 870f));
            viewport.gameObject.AddComponent<RectMask2D>();

            LogText = UiFactory.Label("Log", viewport, string.Empty, 17, UiPalette.Text, TextAnchor.LowerLeft);
            LogText.lineSpacing = 1.1f;
            UiFactory.Stretch(LogText.rectTransform);
        }

        private void BuildOverlay()
        {
            var dim = UiFactory.Panel("ResultOverlay", Root, new Color(0f, 0f, 0f, 0.72f), rounded: false);
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform);
            Overlay = dim.gameObject;

            var box = UiFactory.Panel("Box", dim.transform, UiPalette.Panel);
            UiFactory.Place(box.rectTransform, Vector2.zero, new Vector2(640f, 340f));

            OverlayTitle = UiFactory.Label("Title", box.transform, string.Empty, 64, UiPalette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(OverlayTitle.rectTransform, new Vector2(0f, 90f), new Vector2(600f, 90f));
            UiFactory.AddShadow(OverlayTitle);

            OverlaySub = UiFactory.Label("Sub", box.transform, string.Empty, 22, UiPalette.TextDim);
            UiFactory.Place(OverlaySub.rectTransform, new Vector2(0f, 10f), new Vector2(600f, 70f));

            OverlayButton = UiFactory.Button("FightAgain", box.transform, "Fight again  (R)", UiPalette.Accent, 26, out _);
            UiFactory.Place((RectTransform)OverlayButton.transform, new Vector2(0f, -100f), new Vector2(300f, 70f));

            Overlay.SetActive(false);
        }
    }
}
