namespace The_Grocery_App;

//Cortana: swipeable walkthrough. Shown once on first launch, and any time from the "?" button.
public partial class OnboardingPage : ContentPage
{
    public const string Seen_Key = "Onboarding_Seen_v1";

    readonly bool _First_Run;   //true = opened after the splash (finish goes to the app), false = opened from "?" (finish closes it)
    readonly View[] _Slides;
    readonly View[] _Stages;
    readonly List<Border> _Dots = new();
    int _Index;
    bool _Busy;

    static readonly Color Dot_On = Color.FromArgb("#2E8B57");
    static readonly Color Dot_Off = Color.FromArgb("#D9D6C5");

    public static bool Has_Been_Seen => Preferences.Default.Get(Seen_Key, false);

    public OnboardingPage(bool First_Run)
    {
        InitializeComponent();
        _First_Run = First_Run;

        _Slides = new View[] { Slide_0, Slide_1, Slide_2, Slide_3, Slide_4 };
        _Stages = new View[] { Stage_0, Stage_1, Stage_2, Stage_3, Stage_4 };

        for (int i = 0; i < _Slides.Length; i++)
        {
            var The_Dot = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                StrokeThickness = 0,
                HeightRequest = 8,
                WidthRequest = i == 0 ? 24 : 8,
                BackgroundColor = i == 0 ? Dot_On : Dot_Off,
                VerticalOptions = LayoutOptions.Center
            };
            _Dots.Add(The_Dot);
            Dots_Host.Children.Add(The_Dot);
        }

        Slide_0.Opacity = 0;
        Update_Controls();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_Index == 0 && Slide_0.Opacity == 0)
        {
            await Animate_In(0, 1);
        }
    }

    //Android back button: step back through the slides; on the first one, just finish
    protected override bool OnBackButtonPressed()
    {
        if (_Index > 0) { _ = Go_To(_Index - 1); }
        else { _ = Finish(); }
        return true;
    }

    void On_Next_Clicked(object? sender, EventArgs e)
    {
        if (_Index >= _Slides.Length - 1) { _ = Finish(); }
        else { _ = Go_To(_Index + 1); }
    }

    void On_Skip_Tapped(object? sender, TappedEventArgs e) => _ = Finish();

    void On_Swiped_Left(object? sender, SwipedEventArgs e)
    {
        if (_Index < _Slides.Length - 1) _ = Go_To(_Index + 1);
    }

    void On_Swiped_Right(object? sender, SwipedEventArgs e)
    {
        if (_Index > 0) _ = Go_To(_Index - 1);
    }

    async Task Go_To(int New_Index)
    {
        if (_Busy || New_Index == _Index || New_Index < 0 || New_Index >= _Slides.Length) return;
        _Busy = true;

        int Direction = New_Index > _Index ? 1 : -1;
        var Old_Slide = _Slides[_Index];

        try
        {
            await Safely(Task.WhenAll(
                Old_Slide.FadeToAsync(0, 150, Easing.CubicIn),
                Old_Slide.TranslateToAsync(-36 * Direction, 0, 150, Easing.CubicIn)), 150);
        }
        finally
        {
            Old_Slide.IsVisible = false;
            Old_Slide.Opacity = 1;
            Old_Slide.TranslationX = 0;
        }

        _Index = New_Index;
        Update_Controls();
        await Animate_In(_Index, Direction);
        _Busy = false;
    }

    async Task Animate_In(int Index, int Direction)
    {
        var The_Slide = _Slides[Index];
        var The_Stage = _Stages[Index];

        The_Slide.Opacity = 0;
        The_Slide.TranslationX = 40 * Direction;
        The_Slide.IsVisible = true;
        The_Stage.Scale = 0.94;

        try
        {
            await Safely(Task.WhenAll(
                The_Slide.FadeToAsync(1, 260, Easing.CubicOut),
                The_Slide.TranslateToAsync(0, 0, 300, Easing.CubicOut),
                The_Stage.ScaleToAsync(1, 420, Easing.SpringOut)), 420);
        }
        finally
        {
            //Never leave a slide half-drawn, even if an animation is cut short
            The_Slide.Opacity = 1;
            The_Slide.TranslationX = 0;
            The_Stage.Scale = 1;
        }
    }

    void Update_Controls()
    {
        bool Last = _Index == _Slides.Length - 1;
        Next_Button.Text = Last ? "Let's go shopping" : (_Index == 0 ? "Show me around" : "Next");
        Skip_Label.IsVisible = !Last;

        for (int i = 0; i < _Dots.Count; i++)
        {
            var The_Dot = _Dots[i];
            bool On = i == _Index;
            double From = The_Dot.WidthRequest, To = On ? 24 : 8;
            The_Dot.BackgroundColor = On ? Dot_On : Dot_Off;
            if (Math.Abs(From - To) > 0.5)
            {
                The_Dot.Animate($"Dot_{i}", v => The_Dot.WidthRequest = v, From, To, length: 220, easing: Easing.CubicOut);
            }
        }
    }

    async Task Finish()
    {
        if (_Busy && _First_Run is false) return;
        _Busy = true;

        Preferences.Default.Set(Seen_Key, true);

        if (_First_Run)
        {
            try { await Safely(this.Content.FadeToAsync(0, 220, Easing.CubicIn), 220); } catch { }

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = new AppShell();
            }
        }
        else
        {
            await Navigation.PopModalAsync();
        }
    }

    //Wait for an animation, but never longer than it should take
    static Task Safely(Task The_Animation, int Duration_Ms)
        => Task.WhenAny(The_Animation, Task.Delay(Duration_Ms + 400));
}
