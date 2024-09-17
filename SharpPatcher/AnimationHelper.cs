using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static SharpPatcher.Consts;

namespace SharpPatcher
{
    public static class AnimationHelper
    {
        public static void InitializeLoginAnimation() => WorkingWindow?.LoginScroll.RenderTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-575, TimeSpan.FromSeconds(1)));

        public static ThicknessAnimation F_Thickness_Animate(Thickness From, Thickness To, double Duration = 0.1)
        {
            ThicknessAnimation marginAnimation = new ThicknessAnimation();
            marginAnimation.GetAnimationBaseValue(FrameworkElement.MarginProperty);
            marginAnimation.From = From; // Valor inicial da margem
            marginAnimation.To = To; // Valor final da margem
            marginAnimation.Duration = TimeSpan.FromSeconds(Duration);
            return marginAnimation;
        }

        public static Thickness F_Pressed(Thickness margin) => new Thickness(margin.Left + 1, margin.Top + 1, margin.Right, margin.Bottom);

        public static void AnimateButton(object button, Thickness margin, bool enter = false)
        {
            if (button.GetType() == typeof(Grid))
            {
                var grid = (Grid)button;
                if (enter)
                    grid.BeginAnimation(FrameworkElement.MarginProperty, F_Thickness_Animate(margin, F_Pressed(margin)));
                else
                    grid.BeginAnimation(FrameworkElement.MarginProperty, F_Thickness_Animate(F_Pressed(margin), margin));
            }
            else if (button.GetType() == typeof(Label))
            {
                var label = (Label)button;
                if (enter)
                    label.BeginAnimation(FrameworkElement.MarginProperty, F_Thickness_Animate(margin, F_Pressed(margin)));
                else
                    label.BeginAnimation(FrameworkElement.MarginProperty, F_Thickness_Animate(F_Pressed(margin), margin));
            }
        }
    }
}