﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace Framework.WPF.Adorners
{
    public class AdornerBehavior
    {
        public static readonly DependencyProperty ShowAdornerProperty = DependencyProperty.RegisterAttached("ShowAdorner", typeof(bool), typeof(AdornerBehavior), new UIPropertyMetadata(false, OnShowAdornerChanged));
        public static readonly DependencyProperty ControlProperty = DependencyProperty.RegisterAttached("Control", typeof(FrameworkElement), typeof(AdornerBehavior), new UIPropertyMetadata(null));
        private static readonly DependencyProperty CtrlAdornerProperty = DependencyProperty.RegisterAttached("CtrlAdorner", typeof(ControlAdorner), typeof(AdornerBehavior), new UIPropertyMetadata(null));

        public static bool GetShowAdorner(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowAdornerProperty);
        }

        public static void SetShowAdorner(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowAdornerProperty, value);
        }

        public static FrameworkElement GetControl(DependencyObject obj)
        {
            return (FrameworkElement)obj.GetValue(ControlProperty);
        }

        public static void SetControl(DependencyObject obj, UIElement value)
        {
            obj.SetValue(ControlProperty, value);
        }

        private static void OnShowAdornerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement)
            {
                if (e.NewValue != null)
                {
                    FrameworkElement adornedElement = d as FrameworkElement;
                    bool bValue = (bool)e.NewValue;
                    FrameworkElement adorningElement = GetControl(d);
                    ControlAdorner ctrlAdorner = adornedElement.GetValue(CtrlAdornerProperty) as ControlAdorner;
                    if (ctrlAdorner != null)
                        ctrlAdorner.RemoveLayer();

                    if (bValue && adorningElement != null)
                    {
                        ctrlAdorner = new ControlAdorner(adornedElement, adorningElement);
                        adornedElement.UpdateLayout();
                        var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
                        if(adornerLayer != null)
                            ctrlAdorner.SetLayer(adornerLayer);
                        d.SetValue(CtrlAdornerProperty, ctrlAdorner);
                    }
                }
            }
        }
    }
}
