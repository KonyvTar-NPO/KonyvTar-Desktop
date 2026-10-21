using Avalonia.Controls;
using Avalonia.Interactivity;
using KönyvTár.Models;

namespace KönyvTár.Views.Main;

public partial class EditBookWindow : Window
{
    private string _originalStatus = string.Empty;

    public EditBookWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public EditBookWindow(Book book) : this()
    {
        TitleTextBox.Text = book.Title;
        AuthorTextBox.Text = book.Author;
        PublisherTextBox.Text = book.Publisher;
        IsbnTextBox.Text = book.ISBN;
        InventoryCodeTextBox.Text = book.InventoryCode;
        _originalStatus = book.Status;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text)
            || string.IsNullOrWhiteSpace(AuthorTextBox.Text)
            || string.IsNullOrWhiteSpace(PublisherTextBox.Text)
            || string.IsNullOrWhiteSpace(IsbnTextBox.Text)
            || string.IsNullOrWhiteSpace(InventoryCodeTextBox.Text))
        {
            ValidationTextBlock.Text = "Minden mező kitöltése kötelező.";
            ValidationTextBlock.IsVisible = true;
            return;
        }

        Close(new Book(
            TitleTextBox.Text.Trim(),
            AuthorTextBox.Text.Trim(),
            PublisherTextBox.Text.Trim(),
            _originalStatus,
            IsbnTextBox.Text.Trim(),
            InventoryCodeTextBox.Text.Trim()));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);
}
