namespace RoadNetworkLinearReferencing;
public class Geometry {
    public required List<List<List<double>>> Paths { get; set; }
}

public class Feature {
    public required Attributes Attributes { get; set; }
    public required Geometry Geometry { get; set; }
}

public class Attributes {
    public required string Road { get; set; }
    public required double Start_slk { get; set; }
    public required double End_slk { get; set; }
    public required string Cwy { get; set; }
}

public class RoadAssetsResponse {
    public required List<Feature> Features { get; set; }
    public bool ExceededTransferLimit { get; set; }
}