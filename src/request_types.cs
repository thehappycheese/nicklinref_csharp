using Microsoft.AspNetCore.Http;

namespace RequestTypes;

public enum Carriageway {
    LRS, // Default
    LS,
    RS
}

public enum FormatPointResponse {
    json, // Default
    latlon,

    // currently unsupported
    // wkt,
}

public enum FormatLineResponse {
    /// same as the coordinates field of GeoJSON geometry object but without the wrapping {"type":"Feature", "geometry":{"type":.., "coordinates":...}}
    json,

    // currently unsupported:
    //wkt,
    //geojson

}

public record PointRequest {
    public required string      road   { get; set; }
    public required double      slk    { get; set; }
    public double?              offset { get; set; }
    public Carriageway?         cwy    { get; set; }
    public FormatPointResponse? f      { get; set; }

    public static bool try_from_query(IQueryCollection query, out PointRequest output)  {
        output = new PointRequest{
            road   = "",
            slk    = 0.0,
            cwy    = Carriageway.LRS,
            f      = FormatPointResponse.json,
            offset = 0.0
        };

        if (!query.TryGetValue("road", out var ex_road)) {
            return false;
        }else{
            output.road = ex_road.ToString();
        }

        if (!query.TryGetValue("slk", out var ex_slk) || !double.TryParse(ex_slk.ToString(), out var parse_slk)) {
            return false;
        }else{
            output.slk = parse_slk;
        }

        if (query.TryGetValue("offset", out var ex_offset)) {
            if (double.TryParse(ex_offset, out var parse_offset)) {
                output.offset = parse_offset;
            }
        }
        
        if (query.TryGetValue("cwy", out var ex_cwy)) {
            if (Enum.TryParse(typeof(Carriageway), ex_cwy.ToString(), false, out var cwy_parse)) {
                output.cwy = (Carriageway)cwy_parse;
            }
        }

        if (query.TryGetValue("f", out var ex_f)){
            if (Enum.TryParse(typeof(FormatPointResponse), ex_f, false, out var f_parse)){
                output.f = (FormatPointResponse) f_parse;
            }
        }

        return true;
    }
}

public record LineRequest {
    public required string     road     { get; set; }
    public required double     slk_from { get; set; }
    public required double     slk_to   { get; set; }
    public double?             offset   { get; set; }
    public FormatLineResponse? f        { get; set; }
    public Carriageway?        cwy      { get; set; }

    public static bool try_from_query(IQueryCollection query, out LineRequest output)  {
        output = new LineRequest{
            road     = "",
            slk_from = 0.0,
            slk_to   = 0.0,
            cwy      = Carriageway.LRS,
            f        = FormatLineResponse.json,
            offset   = 0.0
        };
        
        if (!query.TryGetValue("road", out var ex_road)) {
            return false;
        }else{
            output.road = ex_road.ToString();
        }

        if (!query.TryGetValue("slk_from", out var ex_slk_from) || !double.TryParse(ex_slk_from.ToString(), out var parse_slk_from)) {
            return false;
        }else{
            output.slk_from = parse_slk_from;
        }

        if (!query.TryGetValue("slk_to", out var ex_slk_to) || !double.TryParse(ex_slk_to.ToString(), out var parse_slk_to)) {
            return false;
        }else{
            output.slk_to = parse_slk_to;
        }

        if (query.TryGetValue("offset", out var ex_offset)) {
            if (double.TryParse(ex_offset, out var parse_offset)) {
                output.offset = parse_offset;
            }
        }

        if (query.TryGetValue("cwy", out var ex_cwy)) {
            if (Enum.TryParse(typeof(Carriageway), ex_cwy.ToString(), false, out var cwy_parse)) {
                output.cwy = (Carriageway)cwy_parse;
            }
        }

        if (query.TryGetValue("f", out var ex_f)){
            if (Enum.TryParse(typeof(FormatPointResponse), ex_f, false, out var f_parse)){
                output.f = (FormatLineResponse) f_parse;
            }
        }

        return true;
    }
}