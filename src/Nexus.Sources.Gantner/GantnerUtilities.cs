using Nexus.DataModel;
using UDBF.NET;

namespace Nexus.Sources;

internal static class GantnerUtilities
{
    public static NexusDataType GetNexusDataTypeFromUdbfDataType(UDBFDataType dataType)
    {
        return dataType switch
        {
            UDBFDataType.No => 0,
            UDBFDataType.Boolean => 0,
            UDBFDataType.SignedInt8 => NexusDataType.Int8,
            UDBFDataType.UnSignedInt8 => NexusDataType.UInt8,
            UDBFDataType.SignedInt16 => NexusDataType.Int16,
            UDBFDataType.UnSignedInt16 => NexusDataType.UInt16,
            UDBFDataType.SignedInt32 => NexusDataType.Int32,
            UDBFDataType.UnSignedInt32 => NexusDataType.UInt32,
            UDBFDataType.Float => NexusDataType.Float32,
            UDBFDataType.BitSet8 => NexusDataType.UInt8,
            UDBFDataType.BitSet16 => NexusDataType.UInt16,
            UDBFDataType.BitSet32 => NexusDataType.UInt32,
            UDBFDataType.Double => NexusDataType.Float64,
            UDBFDataType.SignedInt64 => NexusDataType.Int64,
            UDBFDataType.UnSignedInt64 => NexusDataType.UInt64,
            UDBFDataType.BitSet64 => NexusDataType.UInt64,
            _ => 0
        };
    }
}
