using System;

namespace KingmakerBuffPlanner.UI
{
    // The right-click spell description's text, composed from the read
    // model only (no invented data): the title as the graph names the spell,
    // one meta line - the native duration text and whether the exact
    // selected variant is shown - and the exact native localized description,
    // which is never altered, trimmed or truncated.
    internal sealed class SpellScrollContent
    {
        internal const string ExactNote = "exact selected source";
        internal const string BaseNote = "base spell — select a caster/source for exact values";
        internal const string MissingDescription = "The game provides no description for this spell.";
        internal const string MetaSeparator = "  ·  ";

        private SpellScrollContent(string title, string meta, string body, bool nativeDescription)
        {
            Title = title;
            Meta = meta;
            Body = body;
            HasNativeDescription = nativeDescription;
        }

        internal string Title { get; private set; }
        internal string Meta { get; private set; }
        internal string Body { get; private set; }
        internal bool HasNativeDescription { get; private set; }

        internal static SpellScrollContent Compose(string title, string description, string durationText,
            bool exactVariant)
        {
            string duration = (durationText ?? string.Empty).Trim();
            string meta = (duration.Length == 0 ? string.Empty : duration + MetaSeparator) +
                (exactVariant ? ExactNote : BaseNote);
            bool native = !string.IsNullOrWhiteSpace(description);
            return new SpellScrollContent(title ?? string.Empty, meta,
                native ? description : MissingDescription, native);
        }
    }

    internal enum SpellScrollInput
    {
        Escape,
        ClickInsideScroll,
        ClickOutsideScroll,
        WheelOverDescription,
        WheelElsewhere
    }

    internal enum SpellScrollOutcome
    {
        // The description is closed: the planner handles the input as usual.
        NotHandled,
        // Swallowed by the open description; nothing beneath sees it.
        Consumed,
        Closed,
        ScrollsDescription
    }

    // The open description's input policy. It is the innermost surface:
    // Escape closes it first; a click outside the scroll closes it and is
    // consumed (the dimmed backdrop is the only thing under the pointer, so
    // the click never reaches the planner, the graph or the game); a click
    // on the scroll itself does nothing but its own Close button; the wheel
    // over the description scrolls only the description, and anywhere else
    // it is swallowed, so the planner's scrolls never move while it is open.
    // Reading a description never authors, targets, casts or saves.
    internal sealed class SpellScrollModalState
    {
        internal const string OutsideClickPolicy = "outside-click-closes;consumed;never-clicks-through";

        internal bool IsOpen { get; private set; }
        internal SpellScrollContent Content { get; private set; }

        // Opening while open replaces the content in place (a programmatic
        // re-show); the description stays the innermost surface.
        internal void Open(SpellScrollContent content)
        {
            if (content == null) throw new ArgumentNullException("content");
            Content = content;
            IsOpen = true;
        }

        internal void Close()
        {
            IsOpen = false;
        }

        internal SpellScrollOutcome Handle(SpellScrollInput input)
        {
            if (!IsOpen) return SpellScrollOutcome.NotHandled;
            switch (input)
            {
                case SpellScrollInput.Escape:
                case SpellScrollInput.ClickOutsideScroll:
                    Close();
                    return SpellScrollOutcome.Closed;
                case SpellScrollInput.WheelOverDescription:
                    return SpellScrollOutcome.ScrollsDescription;
                case SpellScrollInput.ClickInsideScroll:
                case SpellScrollInput.WheelElsewhere:
                    return SpellScrollOutcome.Consumed;
                default:
                    throw new ArgumentOutOfRangeException("input");
            }
        }
    }

    // A rectangle inside the scroll, top-left origin, y down, canvas units.
    internal struct SpellScrollRect
    {
        internal SpellScrollRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        internal float X;
        internal float Y;
        internal float Width;
        internal float Height;
        internal float Right { get { return X + Width; } }
        internal float Bottom { get { return Y + Height; } }

        internal bool Overlaps(SpellScrollRect other)
        {
            return X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
        }
    }

    // The spell scroll's hierarchy: a centred title clear of the Close
    // button, the meta line, a scroll rule, then the scrolling body. The
    // insets keep every text off the sheet's darker edge zone (see
    // PlannerParchmentPalette) once the paper reaches its outsets.
    internal sealed class SpellScrollLayout
    {
        // The sheet's own proportions (858x551 is 1.56:1; 760x520 is 1.46:1)
        // keep the stretched centre close to the artwork's grain.
        internal const float PanelWidth = 760f;
        internal const float PanelHeight = 520f;
        internal const float SidePadding = 44f;
        internal const float TopPadding = 30f;
        internal const float BottomPadding = 30f;
        internal const float CloseWidth = 96f;
        internal const float CloseHeight = 30f;
        internal const float CloseInset = 16f;
        internal const float TitleHeight = 34f;
        internal const float MetaGap = 2f;
        internal const float MetaHeight = 22f;
        internal const float RuleGap = 6f;
        // The rule's reserved slot: the native blockscroll rule's height at
        // the 100-PPU canvas (5.5 units, drawn as 6); a hairline is centred
        // in the same slot, so the body never moves with the theme.
        internal const float RuleSlotHeight = 6f;
        internal const float BodyGap = 10f;
        // The Teleport divider's 12% side margins.
        internal const float RuleSideMargin = 0.12f;
        internal const int TitleFontSize = 22;
        internal const int TitleMinimumFontSize = 15;
        internal const int MetaFontSize = 14;
        internal const int BodyFontSize = 16;
        internal const float BodyScrollbarWidth = 10f;

        private SpellScrollLayout()
        {
        }

        internal float Width { get; private set; }
        internal float Height { get; private set; }
        internal SpellScrollRect Title { get; private set; }
        internal SpellScrollRect Meta { get; private set; }
        internal SpellScrollRect Rule { get; private set; }
        internal SpellScrollRect Body { get; private set; }
        internal SpellScrollRect Close { get; private set; }

        internal static SpellScrollLayout Compute(float width, float height)
        {
            if (width <= 0f || height <= 0f) throw new ArgumentOutOfRangeException("width");
            var layout = new SpellScrollLayout { Width = width, Height = height };
            layout.Close = new SpellScrollRect(width - CloseInset - CloseWidth, CloseInset, CloseWidth, CloseHeight);
            // Symmetric insets keep the title centred on the sheet.
            float titleInset = Math.Max(SidePadding, CloseInset + CloseWidth + 8f);
            layout.Title = new SpellScrollRect(titleInset, TopPadding, width - 2f * titleInset, TitleHeight);
            layout.Meta = new SpellScrollRect(SidePadding, layout.Title.Bottom + MetaGap,
                width - 2f * SidePadding, MetaHeight);
            float ruleWidth = width * (1f - 2f * RuleSideMargin);
            layout.Rule = new SpellScrollRect((width - ruleWidth) / 2f, layout.Meta.Bottom + RuleGap,
                ruleWidth, RuleSlotHeight);
            float bodyTop = layout.Rule.Bottom + BodyGap;
            layout.Body = new SpellScrollRect(SidePadding, bodyTop, width - 2f * SidePadding,
                Math.Max(0f, height - BottomPadding - bodyTop));
            return layout;
        }
    }
}
