/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace eduVPN.Views.Controls
{
    public static class TextBlock
    {
        #region Fields

        public static readonly DependencyProperty CharacterCasingProperty = DependencyProperty.RegisterAttached(
            "CharacterCasing",
            typeof(CharacterCasing),
            typeof(TextBlock),
            new FrameworkPropertyMetadata(
                CharacterCasing.Normal,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.NotDataBindable,
                OnCharacterCasingChanged));

        private static readonly DependencyProperty TextProxyProperty = DependencyProperty.RegisterAttached(
            "TextProxy",
            typeof(string),
            typeof(TextBlock),
            new PropertyMetadata(default(string), OnTextProxyChanged));

        private static readonly PropertyPath TextPropertyPath = new PropertyPath("Text");

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
            if (d is System.Windows.Controls.TextBlock textBlock && BindingOperations.GetBinding(textBlock, TextProxyProperty) == null)
                BindingOperations.SetBinding(
                    textBlock,
                    TextProxyProperty,
                    new Binding
                    {
                        Path = TextPropertyPath,
                        RelativeSource = RelativeSource.Self,
                        Mode = BindingMode.OneWay,
                    });
        }

        private static void OnTextProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty, Format((string)e.NewValue, GetCharacterCasing(d)));

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
