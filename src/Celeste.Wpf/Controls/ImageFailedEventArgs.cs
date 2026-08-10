namespace Celeste.Wpf.Controls;

/// <summary>
/// Why an <see cref="ImageView"/> could not show its source.
/// </summary>
/// <remarks>
/// The control turns a failure into <see cref="ImageState.Failed"/> and reports it here rather than
/// letting it reach the application as an unhandled exception on a background thread. Nothing
/// listening means nothing logged, which is the one case where a swallowed exception is the
/// application's choice instead of the library's.
/// </remarks>
public class ImageFailedEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of the <see cref="ImageFailedEventArgs"/> class.</summary>
    /// <param name="source">The absolute URI that was being loaded.</param>
    /// <param name="exception">The failure.</param>
    public ImageFailedEventArgs(Uri source, Exception exception)
    {
        Source = source;
        Exception = exception;
    }

    /// <summary>
    /// Gets the URI that failed, resolved to an absolute one — not necessarily the value assigned to
    /// <see cref="ImageView.Source"/>, which may have been relative.
    /// </summary>
    public Uri Source { get; }

    /// <summary>Gets the exception the load threw.</summary>
    public Exception Exception { get; }
}
