namespace ItemDeduplication.Application.Matching;

/// <summary>Maps Arabic/English unit words (after normalization) to a base unit.</summary>
internal static class UnitCatalog
{
    private static readonly Dictionary<string, (string Type, decimal Factor)> Units = new(StringComparer.Ordinal)
    {
        // length -> mm
        ["mm"] = ("length", 1m), ["ملم"] = ("length", 1m), ["مم"] = ("length", 1m),
        ["cm"] = ("length", 10m), ["سم"] = ("length", 10m),
        ["m"] = ("length", 1000m), ["meter"] = ("length", 1000m), ["metre"] = ("length", 1000m),
        ["متر"] = ("length", 1000m), ["م"] = ("length", 1000m),
        ["in"] = ("length", 25.4m), ["inch"] = ("length", 25.4m), ["انش"] = ("length", 25.4m), ["بوصه"] = ("length", 25.4m),

        // weight -> g
        ["g"] = ("weight", 1m), ["gm"] = ("weight", 1m), ["gram"] = ("weight", 1m),
        ["جم"] = ("weight", 1m), ["جرام"] = ("weight", 1m), ["غرام"] = ("weight", 1m),
        ["kg"] = ("weight", 1000m), ["كجم"] = ("weight", 1000m), ["كغ"] = ("weight", 1000m),
        ["كيلو"] = ("weight", 1000m), ["كيلوجرام"] = ("weight", 1000m),
        ["lb"] = ("weight", 453.592m),

        // volume -> ml
        ["ml"] = ("volume", 1m), ["مل"] = ("volume", 1m),
        ["l"] = ("volume", 1000m), ["ltr"] = ("volume", 1000m), ["liter"] = ("volume", 1000m),
        ["litre"] = ("volume", 1000m), ["لتر"] = ("volume", 1000m), ["ل"] = ("volume", 1000m),

        // pressure -> bar  (PN10 is converted to "10 bar" by the normalizer)
        ["bar"] = ("pressure", 1m), ["بار"] = ("pressure", 1m),

        // electrical
        ["v"] = ("voltage", 1m), ["volt"] = ("voltage", 1m), ["فولت"] = ("voltage", 1m),
        ["w"] = ("power", 1m), ["watt"] = ("power", 1m), ["واط"] = ("power", 1m),
        ["kw"] = ("power", 1000m), ["كيلوواط"] = ("power", 1000m),
        ["hz"] = ("frequency", 1m), ["هرتز"] = ("frequency", 1m),

        // count
        ["pcs"] = ("count", 1m), ["pc"] = ("count", 1m), ["piece"] = ("count", 1m),
        ["قطعه"] = ("count", 1m), ["قطعة"] = ("count", 1m), ["حبه"] = ("count", 1m), ["حبة"] = ("count", 1m),
    };

    public static bool TryGet(string token, out (string Type, decimal Factor) unit)
        => Units.TryGetValue(token, out unit);
}
