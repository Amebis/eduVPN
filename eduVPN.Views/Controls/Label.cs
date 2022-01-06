/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace eduVPN.Views.Controls
{
    public static class Label
    {
        #region Fields

        public static readonly DependencyProperty CharacterCasingProperty = DependencyProperty.RegisterAttached(
            "CharacterCasing",
            typeof(CharacterCasing),
            typeof(Label),
            new FrameworkPropertyMetadata(
                CharacterCasing.Normal,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.NotDataBindable,
                OnCharacterCasingChanged));

        private static readonly DependencyProperty ContentProxyProperty = DependencyProperty.RegisterAttached(
            "ContentProxy",
            typeof(string),
            typeof(Label),
            new PropertyMetadata(default(string), OnContentProxyChanged));

        private static readonly PropertyPath ContentPropertyPath = new PropertyPath("Content");

        #endregion

        #region Methods

        public static void SetCharacterCasing(DependencyObject element, CharacterCasing value)
        {
            element.SetValue(CharacterCasingProperty, value);
        }

        public static CharacterCasing GetCharacterCasing(DependencyObject element)
        {
            return (CharacterCasing)element.GetValue(CharacterCasingProperty);
        }

        private static void OnCharacterCasingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.Label textBlock && BindingOperations.GetBinding(textBlock, ContentProxyProperty) == null)
                BindingOperations.SetBinding(
                    textBlock,
                    ContentProxyProperty,
                    new Binding
                    {
                        Path = ContentPropertyPath,
                        RelativeSource = RelativeSource.Self,
                        Mode = BindingMode.OneWay,
                    });
        }

        private static void OnContentProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetCurrentValue(System.Windows.Controls.Label.ContentProperty, Format((string)e.NewValue, GetCharacterCasing(d)));

            string Format(string text, CharacterCasing casing)
            {
                if (string.IsNullOrEmpty(text))
                    return text;

                switch (casing)
                {
                    case CharacterCasing.Normal:
                        return text;
                    case CharacterCasing.Lower:
                        return text.ToLower();
                    case CharacterCasing.Upper:
                        return text.ToUpper();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(casing), casing, null);
                }
            }
        }

        #endregion
    }
}
