using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PaymProdNet9.Controls;

public partial class DateTimePicker : UserControl
{
    public static readonly DependencyProperty SelectedDateTimeProperty =
        DependencyProperty.Register(nameof(SelectedDateTime), typeof(DateTime?), typeof(DateTimePicker),
            new PropertyMetadata(null, OnSelectedDateTimeChanged));

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(DateTimePicker),
            new PropertyMetadata(null, OnSelectedDateChanged));

    public static readonly RoutedEvent SelectedDateTimeChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectedDateTimeChanged), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(DateTimePicker));

    private bool _isUpdating = false;

    public DateTimePicker()
    {
        InitializeComponent();
        
        // Устанавливаем русскую локализацию для DatePicker
        var culture = new CultureInfo("ru-RU");
        DatePickerControl.Language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
        
        // Инициализируем ComboBox для часов (0-23)
        HoursComboBox.ItemsSource = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
        
        // Инициализируем ComboBox для минут (0-59 с шагом 1)
        MinutesComboBox.ItemsSource = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();
        
        // Устанавливаем текущее время по умолчанию
        var now = DateTime.Now;
        HoursComboBox.SelectedItem = now.Hour.ToString("00");
        MinutesComboBox.SelectedItem = now.Minute.ToString("00");
        
        DatePickerControl.SelectedDateChanged += DatePicker_SelectedDateChanged;
    }

    public DateTime? SelectedDateTime
    {
        get => (DateTime?)GetValue(SelectedDateTimeProperty);
        set => SetValue(SelectedDateTimeProperty, value);
    }

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public event RoutedEventHandler SelectedDateTimeChanged
    {
        add => AddHandler(SelectedDateTimeChangedEvent, value);
        remove => RemoveHandler(SelectedDateTimeChangedEvent, value);
    }

    private static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DateTimePicker picker && !picker._isUpdating)
        {
            picker._isUpdating = true;
            try
            {
                var dateTime = (DateTime?)e.NewValue;
                if (dateTime.HasValue)
                {
                    picker.DatePickerControl.SelectedDate = dateTime.Value.Date;
                    picker.HoursComboBox.SelectedItem = dateTime.Value.Hour.ToString("00");
                    picker.MinutesComboBox.SelectedItem = dateTime.Value.Minute.ToString("00");
                }
                else
                {
                    picker.DatePickerControl.SelectedDate = null;
                    picker.HoursComboBox.SelectedItem = null;
                    picker.MinutesComboBox.SelectedItem = null;
                }
                
                // Вызываем событие
                picker.RaiseEvent(new RoutedEventArgs(SelectedDateTimeChangedEvent));
            }
            finally
            {
                picker._isUpdating = false;
            }
        }
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DateTimePicker picker && !picker._isUpdating)
        {
            picker._isUpdating = true;
            try
            {
                var date = (DateTime?)e.NewValue;
                if (date.HasValue)
                {
                    picker.DatePickerControl.SelectedDate = date.Value.Date;
                    var time = picker.SelectedDateTime?.TimeOfDay ?? TimeSpan.Zero;
                    picker.SelectedDateTime = date.Value.Date + time;
                }
                else
                {
                    picker.DatePickerControl.SelectedDate = null;
                    picker.SelectedDateTime = null;
                }
            }
            finally
            {
                picker._isUpdating = false;
            }
        }
    }

    private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            UpdateSelectedDateTime();
        }
    }

    private void TimeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUpdating)
        {
            UpdateSelectedDateTime();
        }
    }

    private void UpdateSelectedDateTime()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            var date = DatePickerControl.SelectedDate;
            if (!date.HasValue)
            {
                var oldValue = SelectedDateTime;
                SelectedDateTime = null;
                if (oldValue != SelectedDateTime)
                {
                    RaiseEvent(new RoutedEventArgs(SelectedDateTimeChangedEvent));
                }
                return;
            }

            // Получаем выбранные часы и минуты из ComboBox
            var hoursStr = HoursComboBox.SelectedItem?.ToString();
            var minutesStr = MinutesComboBox.SelectedItem?.ToString();

            int hours = 0;
            int minutes = 0;

            if (!string.IsNullOrEmpty(hoursStr) && int.TryParse(hoursStr, out var h))
            {
                hours = h;
            }

            if (!string.IsNullOrEmpty(minutesStr) && int.TryParse(minutesStr, out var m))
            {
                minutes = m;
            }

            var time = new TimeSpan(hours, minutes, 0);
            var oldDateTime = SelectedDateTime;
            SelectedDateTime = date.Value.Date + time;
            SelectedDate = date.Value.Date;
            
            if (oldDateTime != SelectedDateTime)
            {
                RaiseEvent(new RoutedEventArgs(SelectedDateTimeChangedEvent));
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
