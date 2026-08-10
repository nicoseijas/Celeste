namespace Celeste.Wpf.Controls;

/// <summary>
/// Where an <see cref="ImageView"/> is in the business of getting its picture on screen.
/// </summary>
public enum ImageState
{
    /// <summary>No <see cref="ImageView.Source"/> is set. Nothing has been attempted.</summary>
    None,

    /// <summary>The source is being fetched or decoded.</summary>
    Loading,

    /// <summary>The picture is decoded and showing.</summary>
    Loaded,

    /// <summary>
    /// The source could not be turned into an image. Subscribe to
    /// <see cref="ImageView.ImageFailed"/> for the reason.
    /// </summary>
    Failed,
}
