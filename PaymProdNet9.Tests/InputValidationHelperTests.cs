using PaymProdNet9.Services;
using System;
using System.Threading;
using System.Windows.Controls;
using Xunit;

namespace PaymProdNet9.Tests.Services;

public class InputValidationHelperTests
{
    private void RunInSta(Action action)
    {
        Exception exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
            throw exception;
    }

    [Fact]
    public void ValidateNumericField_LostFocus_ShouldCapMaximize()
    {
        RunInSta(() =>
        {
            var textBox = new TextBox { Text = "1000000000" }; // > Max
            InputValidationHelper.ValidateNumericField_LostFocus(textBox, null);
            Assert.Equal("999999999", textBox.Text);
        });
    }

    [Fact]
    public void ValidateNumericField_LostFocus_ShouldClampNegative()
    {
        RunInSta(() =>
        {
            var textBox = new TextBox { Text = "-5" };
            InputValidationHelper.ValidateNumericField_LostFocus(textBox, null);
            Assert.Equal("0", textBox.Text);
        });
    }

    [Fact]
    public void ValidateNumericField_LostFocus_ShouldResetInvalid()
    {
        RunInSta(() =>
        {
            var textBox = new TextBox { Text = "abc" };
            InputValidationHelper.ValidateNumericField_LostFocus(textBox, null);
            Assert.Equal("0", textBox.Text);
        });
    }

    [Fact]
    public void ValidateTextField_LostFocus_ShouldTruncate()
    {
        RunInSta(() =>
        {
            var longText = new string('a', 60);
            var textBox = new TextBox { Text = longText };
            InputValidationHelper.ValidateTextField_LostFocus(textBox, null);

            Assert.Equal(50, textBox.Text.Length);
            Assert.Equal(new string('a', 50), textBox.Text);
        });
    }

    [Fact]
    public void ValidateIntegerField_LostFocus_ShouldWork()
    {
        RunInSta(() =>
        {
            var textBox = new TextBox { Text = "123.45" }; // Invalid integer
            InputValidationHelper.ValidateIntegerField_LostFocus(textBox, null);
            Assert.Equal("0", textBox.Text);

            textBox.Text = "123";
            InputValidationHelper.ValidateIntegerField_LostFocus(textBox, null);
            Assert.Equal("123", textBox.Text);
        });
    }
}
