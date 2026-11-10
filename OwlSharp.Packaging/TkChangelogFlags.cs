using CommunityToolkit.Mvvm.ComponentModel;
using OwlSharp.Merging;

namespace OwlSharp.Packaging;

public partial class TkChangelogFlags : ObservableObject
{
    [ObservableProperty]
    private bool _trackRemovedRsDbEntries = false;

    [ObservableProperty]
    private TkResourceSizeOverrideConfiguration _resourceSizeOverrides = new();
    
    public TkChangelogBuilderFlags GetBuilderFlags(IReadOnlyDictionary<string, uint>? resourceSizeOverrides = null)
    {
        return new TkChangelogBuilderFlags(TrackRemovedRsDbEntries, resourceSizeOverrides);
    }
}