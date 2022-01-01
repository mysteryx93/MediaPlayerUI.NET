using System.Collections.Generic;
using System.Linq;

namespace HanumanInstitute.MediaPlayer.Avalonia.Bass;

/// <summary>
/// Represents a supported file extension.
/// </summary>
public class FileExtension
{
    public FileExtension(string name, IEnumerable<string> extensions)
    {
        this.Name = name;
        this.Extensions = extensions.ToList();
    }

    /// <summary>
    /// The name of the file format.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// A list of file extensions.
    /// </summary>
    public IList<string> Extensions { get; }

    /// <summary>
    /// Returns a string representation of the file formats and extensions.
    /// </summary>
    public override string ToString() => Name + " | " + string.Join(';', Extensions);
}
