using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PSLauncher;

internal static class WpfUtils
{
    /// <summary>
    /// Travels upward through the visual tree from the specified <paramref name="child"/> node,
    /// and retrieves the parent element of the first type <typeparamref name="T"/> found.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the parent element to search for. It must inherit from <see cref="DependencyObject"/>.
    /// </typeparam>
    /// <param name="child">
    /// The child node from which to start the search. Allows null (returns false if null).
    /// </param>
    /// <param name="parent">
    /// An output parameter that returns the parent element found.
    /// If no parent element is found, this parameter is set to null (the return value is false).
    /// </param>
    /// <returns>
    /// Returns true if a parent element of the specified type is found, and sets the corresponding element to <paramref name="parent"/>.
    /// If the element is not found, it returns false.
    /// </returns>
    /// <remarks>
    /// Use <see cref="VisualTreeHelper.GetParent(DependencyObject)"/> to retrieve the parent element in the visual tree.
    /// If the specified type is not found even after traversing to the root, returns null.
    /// </remarks>
    public static bool TryFindParent<T>(this DependencyObject? child,
                                        [MaybeNullWhen(false)] out T parent)
        where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T t)
            {
                parent = t;
                return true;
            }
            child = VisualTreeHelper.GetParent(child);
        }
        parent = null;
        return false;
    }

    /// <summary>
    /// Travels upward through the visual tree from the specified <paramref name="child"/> node,
    /// and retrieves the parent element of the first type <typeparamref name="T"/> found.
    /// However, the search stops as soon as an element of type <typeparamref name="TUntil"/> is reached (the element itself is not included in the search).
    /// </summary>
    /// <typeparam name="T">
    /// The type of the parent element to be searched. Must inherit from <see cref="DependencyObject"/>.
    /// </typeparam>
    /// <typeparam name="TUntil">
    /// The type that serves as the boundary at which the search stops. Must inherit from <see cref="DependencyObject"/>.
    /// Once an ancestor matching this type is reached, the search does not proceed any further up the tree.
    /// </typeparam>
    /// <param name="child">
    /// The child node from which the search begins. Allows null (returns false if null).
    /// </param>
    /// <param name="parent">
    /// An output parameter that returns the parent element found.
    /// If none is found, it is set to null (the return value is false). </param>
    /// <returns>
    /// Returns true if a parent element of the specified type is found, and sets the corresponding element to <paramref name="parent"/>.
    /// Returns false if no match is found (or if the boundary type <typeparamref name="TUntil"/> is reached).
    /// </returns>
    /// <remarks>
    /// Boundary types are not included in the search. This is used, for example, when you want to terminate the search at a window or a specific container.
    /// </remarks>
    public static bool TryFindParent<T, TUntil>(this DependencyObject? child,
                                                [MaybeNullWhen(false)] out T parent)
        where T : DependencyObject
        where TUntil : DependencyObject
    {
        while (child is not null and not TUntil)
        {
            if (child is T t)
            {
                parent = t;
                return true;
            }
            child = VisualTreeHelper.GetParent(child);
        }
        parent = null;
        return false;
    }

    /// <summary>
    /// Travels down the visual tree from the specified <paramref name="parent"/> node
    /// and searches for child elements of the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the child elements to search for. It must inherit from <see cref="DependencyObject"/>.
    /// </typeparam>
    /// <param name="parent">
    /// The parent node from which to start the search. null is allowed (returns false if null).
    /// </param>
    /// <param name="child">
    /// Output parameter that returns the found child element.
    /// Set to null if no child is found (return value is false). </param>
    /// <returns>
    /// Returns true if a child element of the specified type is found, and sets the corresponding element to <paramref name="child"/>.
    /// Returns false if none is found.
    /// </returns>
    public static bool TryFindChild<T>(this DependencyObject? parent,
                                       [MaybeNullWhen(false)] out T child)
        where T : DependencyObject
    {
        if (parent is null)
        {
            child = null;
            return false;
        }
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var currentChild = VisualTreeHelper.GetChild(parent, i);
            if (currentChild is T t)
            {
                child = t;
                return true;
            }
            if (currentChild.TryFindChild(out child))
            {
                return true;
            }
        }
        child = null;
        return false;
    }

    #region RichTextBox
    /// <summary>
    /// Retrieves the text content from the specified <see cref="RichTextBox"/> document.
    /// </summary>
    /// <param name="richTextBox">The RichTextBox from which to retrieve the text content. </param>
    /// <returns>The text content of the RichTextBox.</returns>
    public static string GetInnerText(RichTextBox richTextBox)
    {
        var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
        return range.Text.TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Returns the character position of the current caret relative to the beginning of the "current line" (zero-based).
    /// </summary>
    /// <remarks>
    /// Obtains the start of the current line and calculates the length of the text from that point to the current caret position
    /// <see cref="System.String.Length"/> (in number of UTF-16 code units).
    /// Please note that the result may differ from the intuitive “character count”
    /// due to inline elements, ligatures, surrogate pairs, and other factors within rich text.
    /// </remarks>
    /// <value>The character offset of the caret within the current line (starting at 0)</value>
    public static int GetCaretIndex(RichTextBox richTextBox)

    {
        var lineStart = richTextBox.CaretPosition.GetLineStartPosition(0);
        var textRange = new TextRange(lineStart, richTextBox.CaretPosition);
        var index = textRange.Text.Length;
        return index;
    }

    public static TextRange GetRange(RichTextBox richTextBox, int start, int end)
    {
        var startPointer = GetTextPointerAtOffset(richTextBox.Document.ContentStart, start + 1);
        if (start < end)
        {
            var endPointer = GetTextPointerAtOffset(startPointer, end - start);
            return new TextRange(startPointer, endPointer);
        }
        return new TextRange(startPointer, startPointer);
    }

    private static TextPointer GetTextPointerAtOffset(TextPointer start, int offset)
    {
        var pointer = start;
        int currentOffset = 0;
        while (currentOffset < offset)
        {
            var next = pointer.GetNextInsertionPosition(LogicalDirection.Forward);
            if (next is null)
            {
                break;
            }
            pointer = next;
            currentOffset++;
        }

        return pointer;
    }
    #endregion
}
