using System.Collections;
using BymlLibrary;

namespace OwlSharp.Merging.Mergers.BinaryYaml;

public class BymlMergeTrackingMapEntry<TKey> : HashSet<TKey> where TKey : notnull;