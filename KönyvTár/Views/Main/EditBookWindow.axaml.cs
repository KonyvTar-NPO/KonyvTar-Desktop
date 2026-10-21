using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
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

    public EditBookWindow(Book book, IEnumerable<Book>? catalog = null) : this()
    {
        var books = catalog?.ToList() ?? [book];
        TitleAutoComplete.ItemsSource = books.Select(item => item.Title).Distinct().ToList();
        AuthorAutoComplete.ItemsSource = books.Select(item => item.Author).Distinct().ToList();
        PublisherAutoComplete.ItemsSource = books.Select(item => item.Publisher).Distinct().ToList();
        IsbnAutoComplete.ItemsSource = books.Select(item => item.ISBN).Distinct().ToList();
        InventoryCodeAutoComplete.ItemsSource = books.Select(item => item.InventoryCode).Distinct().ToList();
        TitleAutoComplete.Text = book.Title;
        AuthorAutoComplete.Text = book.Author;
        PublisherAutoComplete.Text = book.Publisher;
        IsbnAutoComplete.Text = book.ISBN;
        InventoryCodeAutoComplete.Text = book.InventoryCode;
        _originalStatus = book.Status;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleAutoComplete.Text)
            || string.IsNullOrWhiteSpace(AuthorAutoComplete.Text)
            || string.IsNullOrWhiteSpace(PublisherAutoComplete.Text)
            || string.IsNullOrWhiteSpace(IsbnAutoComplete.Text)
            || string.IsNullOrWhiteSpace(InventoryCodeAutoComplete.Text))
        {
            ValidationTextBlock.Text = "Minden mező kitöltése kötelező.";
            ValidationTextBlock.IsVisible = true;
            return;
        }

        Close(new Book(
            TitleAutoComplete.Text.Trim(),
            AuthorAutoComplete.Text.Trim(),
            PublisherAutoComplete.Text.Trim(),
            _originalStatus,
            IsbnAutoComplete.Text.Trim(),
            InventoryCodeAutoComplete.Text.Trim()));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(null);
}
