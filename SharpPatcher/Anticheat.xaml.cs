using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SharpPatcher
{
    /// <summary>
    /// Lógica interna para Window1.xaml
    /// </summary>
    public partial class AntiCheater : Window
    {
 
        public AntiCheater()
        {
            InitializeComponent();
        }

        private void Progress_Initialized(object sender, EventArgs e)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(2));

            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.From = 0; // Start from the current value
            doubleAnimation.To = 100; // End at the maximum value (100%)
            doubleAnimation.Duration = duration;
            doubleAnimation.Completed += (s, _) =>
            {
                Close();
            };
            // Apply the animation to the ProgressBar's Value property
            progress.BeginAnimation(ProgressBar.ValueProperty, doubleAnimation);
        }
    }
}