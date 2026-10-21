using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using KönyvTár.Models;

namespace KönyvTár.Views.Main;

public partial class EditLendingWindow : Window
{
    private readonly LendingRecord _lending;

    public EditLendingWindow() : this(null, Array.Empty<LibraryUser>())
    {
    }

    public EditLendingWindow(LendingRecord? lending, IEnumerable<LibraryUser> users)
    {
        InitializeComponent();
        _lending = lending ?? new LendingRecord(
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            DateTimeOffset.Now, DateTimeOffset.Now, null, string.Empty, false);

        UserAutoComplete.ItemsSource = users.Select(user => user.Name).Distinct().ToList();
        if (lending is null)
            return;

        UserAutoComplete.Text = lending.UserName;
        BorrowedDatePicker.SelectedDate = lending.BorrowedAt.Date;
        DueDatePicker.SelectedDate = lending.DueDate.Date;
        NotifyCheckBox.IsChecked = lending.NotifyByEmail;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserAutoComplete.Text)
            || BorrowedDatePicker.SelectedDate is not { } borrowedAt
            || DueDatePicker.SelectedDate is not { } dueDate
            || dueDate.Date < borrowedAt.Date)
        {
            ValidationTextBlock.Text = "Adjon meg felhasználót, érvényes kezdő dátumot és a kezdő dátummal azonos vagy későbbi lejáratot.";
            ValidationTextBlock.IsVisible = true;
            return;
        }

        Close(_lending with
        {
            UserName = UserAutoComplete.Text.Trim(),
            BorrowedAt = new DateTimeOffset(borrowedAt.Date, _lending.BorrowedAt.Offset),
            DueDate = new DateTimeOffset(dueDate.Date, _lending.DueDate.Offset),
            NotifyByEmail = NotifyCheckBox.IsChecked == true
        });
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);
}
