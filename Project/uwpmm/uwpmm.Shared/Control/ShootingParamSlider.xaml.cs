﻿using Kazyx.RemoteApi;
using Kazyx.Uwpmm.Utility;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Kazyx.Uwpmm.Control
{
    public sealed partial class ShootingParamSlider : UserControl
    {
        public ShootingParamSlider()
        {
            this.InitializeComponent();
        }

        public Capability<string> Parameter
        {
            set
            {
                SetValue(PrameterProperty, value);
                UpdateDisplay<string>(value);
            }
            get { return (Capability<string>)GetValue(PrameterProperty); }
        }

        public static readonly DependencyProperty PrameterProperty = DependencyProperty.Register(
            "Parameter",
            typeof(Capability<string>),
            typeof(ShootingParamSlider),
            new PropertyMetadata(null, new PropertyChangedCallback(ShootingParamSlider.ParameterUpdated)));

        private static void ParameterUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                DebugUtil.Log("Parameter updated: " + (e.NewValue as Capability<string>).Current);
                (d as ShootingParamSlider).UpdateDisplay<string>(e.NewValue as Capability<string>);
            }
        }

        public ImageSource IconSource { set { SettingImage.Source = value; } }

        void UpdateDisplay<T>(Capability<T> parameter)
        {
            if (parameter == null || parameter.Candidates == null || parameter.Candidates.Count == 0) { return; }

            Slider.Minimum = 0;
            Slider.Maximum = parameter.Candidates.Count - 1;
            for (int i = 0; i < parameter.Candidates.Count; i++)
            {
                if (parameter.Current.Equals(parameter.Candidates[i]))
                {
                    Slider.Value = i;
                }
            }
            MinLabel.Text = parameter.Candidates[0].ToString();
            MaxLabel.Text = parameter.Candidates[parameter.Candidates.Count - 1].ToString();
        }

        public event EventHandler<ShootingParameterChangedEventArgs> SliderOperated;
        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var selected = (int)Math.Round(e.NewValue);
            if (Parameter == null || selected < 0 || selected >= Parameter.Candidates.Count) { return; }
            if (SliderOperated != null) { SliderOperated(this, new ShootingParameterChangedEventArgs() { Selected = Parameter.Candidates[selected] }); }
        }
    }

    public class ShootingParameterChangedEventArgs
    {
        public string Selected { get; set; }
    }
}
