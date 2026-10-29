namespace The_Grocery_App.Controls
{
    //Small, reusable animations. Attach them in XAML - no page logic changes needed.

    //Fades + slides an element in the first time it shows up (pages, list rows)
    public class FadeInBehavior : Behavior<VisualElement>
    {
        public uint Duration { get; set; } = 260;
        public double Offset { get; set; } = 10;    //How far it slides up from (0 = fade only)
        public int Delay { get; set; } = 0;

        protected override void OnAttachedTo(VisualElement The_Element)
        {
            base.OnAttachedTo(The_Element);
            The_Element.Loaded += On_Loaded;
        }

        protected override void OnDetachingFrom(VisualElement The_Element)
        {
            The_Element.Loaded -= On_Loaded;
            base.OnDetachingFrom(The_Element);
        }

        async void On_Loaded(object? sender, EventArgs e)
        {
            if (sender is not VisualElement The_Element) return;

            try
            {
                The_Element.Opacity = 0;
                The_Element.TranslationY = Offset;

                if (Delay > 0) await Task.Delay(Delay);

                //Wait for the animation, but never longer than it should take (some containers stall animations)
                await Task.WhenAny(
                    Task.WhenAll(
                        The_Element.FadeToAsync(1, Duration, Easing.CubicOut),
                        The_Element.TranslateToAsync(0, 0, Duration, Easing.CubicOut)),
                    Task.Delay((int)Duration + 400));
            }
            catch { }
            finally
            {
                //Never leave anything half-faded, even if the animation was cut short
                The_Element.Opacity = 1;
                The_Element.TranslationY = 0;
            }
        }
    }

    //Buttons shrink a touch when pressed and spring back - feels "physical"
    public class PressScaleBehavior : Behavior<Button>
    {
        protected override void OnAttachedTo(Button The_Button)
        {
            base.OnAttachedTo(The_Button);
            The_Button.Pressed += On_Pressed;
            The_Button.Released += On_Released;
        }

        protected override void OnDetachingFrom(Button The_Button)
        {
            The_Button.Pressed -= On_Pressed;
            The_Button.Released -= On_Released;
            base.OnDetachingFrom(The_Button);
        }

        async void On_Pressed(object? sender, EventArgs e)
        {
            if (sender is Button The_Button)
            {
                try { await The_Button.ScaleToAsync(0.96, 80, Easing.CubicOut); } catch { }
            }
        }

        async void On_Released(object? sender, EventArgs e)
        {
            if (sender is Button The_Button)
            {
                try { await The_Button.ScaleToAsync(1, 160, Easing.SpringOut); } catch { The_Button.Scale = 1; }
            }
        }
    }

    //For things the page shows/hides in code (the recipe box + Go button):
    //animates them in when they become visible.
    //On a Border it also copies the visibility of its Content, so the card hides together with what's inside it.
    public class RevealBehavior : Behavior<VisualElement>
    {
        public uint Duration { get; set; } = 240;

        VisualElement? _element;
        VisualElement? _watched;

        protected override void OnAttachedTo(VisualElement The_Element)
        {
            base.OnAttachedTo(The_Element);
            _element = The_Element;

            //A Border follows its child (XAML may attach this before the child exists, so wait for it)
            if (The_Element is Border The_Border && The_Border.Content is null)
            {
                The_Border.PropertyChanged += On_Border_Content_Set;
                return;
            }

            Watch((The_Element as Border)?.Content as VisualElement ?? The_Element);
        }

        void On_Border_Content_Set(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Border.Content) || sender is not Border The_Border || The_Border.Content is not VisualElement The_Child) return;

            The_Border.PropertyChanged -= On_Border_Content_Set;
            Watch(The_Child);
        }

        void Watch(VisualElement The_Target)
        {
            _watched = The_Target;
            _watched.PropertyChanged += On_Watched_Changed;

            if (_element is not null && !ReferenceEquals(_watched, _element))
            {
                _element.IsVisible = _watched.IsVisible;
            }
        }

        protected override void OnDetachingFrom(VisualElement The_Element)
        {
            if (_watched is not null) _watched.PropertyChanged -= On_Watched_Changed;
            if (The_Element is Border The_Border) The_Border.PropertyChanged -= On_Border_Content_Set;
            _watched = null;
            _element = null;
            base.OnDetachingFrom(The_Element);
        }

        async void On_Watched_Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(VisualElement.IsVisible) || _element is null || _watched is null) return;

            bool Show = _watched.IsVisible;

            if (!ReferenceEquals(_watched, _element))
            {
                _element.IsVisible = Show;
            }

            if (!Show) return;

            try
            {
                _element.Opacity = 0;
                _element.TranslationY = -6;

                await Task.WhenAny(
                    Task.WhenAll(
                        _element.FadeToAsync(1, Duration, Easing.CubicOut),
                        _element.TranslateToAsync(0, 0, Duration, Easing.CubicOut)),
                    Task.Delay((int)Duration + 400));
            }
            catch { }
            finally
            {
                if (_element is not null)
                {
                    _element.Opacity = 1;
                    _element.TranslationY = 0;
                }
            }
        }
    }
}
