using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PaymProdNet9.Services;

/// <summary>
/// Помощник для валидации ввода в текстовых полях
/// </summary>
public static class InputValidationHelper
{
    /// <summary>
    /// Максимальное значение для числовых полей
    /// </summary>
    public const long MaxNumericValue = 999_999_999;

    /// <summary>
    /// Максимальная длина текстовых полей
    /// </summary>
    public const int MaxTextLength = 50;

    /// <summary>
    /// Обработчик для числовых полей: только положительные числа от 0 до 999 999 999
    /// </summary>
    public static void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null)
        {
            e.Handled = true;
            return;
        }

        // Разрешаем только цифры и десятичный разделитель
        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var allowedChars = "0123456789" + decimalSeparator;

        // Проверяем, что вводимый символ разрешен
        if (allowedChars.IndexOf(e.Text) < 0)
        {
            e.Handled = true;
            return;
        }

        // Проверяем, что десятичный разделитель не дублируется
        if (e.Text == decimalSeparator && textBox.Text.Contains(decimalSeparator))
        {
            e.Handled = true;
            return;
        }

        // Получаем текст, который будет после ввода
        var currentText = textBox.Text ?? string.Empty;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        var newText = currentText.Substring(0, selectionStart) +
                      e.Text +
                      currentText.Substring(selectionStart + selectionLength);

        // Проверяем, что значение не превышает максимум
        if (!string.IsNullOrWhiteSpace(newText) && newText != decimalSeparator)
        {
            // Удаляем разделитель для проверки максимального значения
            var textForCheck = newText.Replace(decimalSeparator, string.Empty);

            // Проверяем, что это число
            if (long.TryParse(textForCheck, out var numericValue))
            {
                // Проверяем, что значение не превышает максимум
                if (numericValue > MaxNumericValue)
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (double.TryParse(newText, NumberStyles.Any, CultureInfo.CurrentCulture, out var doubleValue))
            {
                // Для дробных чисел проверяем целую часть
                var integerPart = (long)Math.Truncate(doubleValue);
                if (integerPart > MaxNumericValue || doubleValue < 0)
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Обработчик для целочисленных полей: только положительные целые числа от 0 до 999 999 999
    /// </summary>
    public static void IntegerOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null)
        {
            e.Handled = true;
            return;
        }

        // Разрешаем только цифры
        if (!char.IsDigit(e.Text, 0))
        {
            e.Handled = true;
            return;
        }

        // Получаем текст, который будет после ввода
        var currentText = textBox.Text ?? string.Empty;
        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        var newText = currentText.Substring(0, selectionStart) +
                      e.Text +
                      currentText.Substring(selectionStart + selectionLength);

        // Проверяем, что значение не превышает максимум
        if (!string.IsNullOrWhiteSpace(newText))
            if (long.TryParse(newText, out var numericValue))
                if (numericValue > MaxNumericValue || numericValue < 0)
                {
                    e.Handled = true;
                    return;
                }
    }

    /// <summary>
    /// Валидация числового поля при потере фокуса: ограничение от 0 до 999 999 999
    /// </summary>
    public static void ValidateNumericField_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        var text = textBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;

        // Пробуем распарсить как число
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
        {
            // Проверяем диапазон
            if (value < 0)
                textBox.Text = "0";
            else if (value > MaxNumericValue) textBox.Text = MaxNumericValue.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            // Если не удалось распарсить, очищаем поле или устанавливаем 0
            textBox.Text = "0";
        }
    }

    /// <summary>
    /// Валидация целочисленного поля при потере фокуса: ограничение от 0 до 999 999 999
    /// </summary>
    public static void ValidateIntegerField_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        var text = textBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;

        // Пробуем распарсить как целое число
        if (long.TryParse(text, out var value))
        {
            // Проверяем диапазон
            if (value < 0)
                textBox.Text = "0";
            else if (value > MaxNumericValue) textBox.Text = MaxNumericValue.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            // Если не удалось распарсить, устанавливаем 0
            textBox.Text = "0";
        }
    }

    /// <summary>
    /// Валидация текстового поля: ограничение длины до 50 символов
    /// </summary>
    public static void ValidateTextField_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        var text = textBox.Text ?? string.Empty;
        if (text.Length > MaxTextLength)
        {
            textBox.Text = text.Substring(0, MaxTextLength);
            textBox.CaretIndex = MaxTextLength;
        }
    }
}