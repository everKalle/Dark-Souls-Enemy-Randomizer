namespace EnemyRandomizer.Core;

/// <summary>Supported releases of Dark Souls.</summary>
public enum GameEdition
{
    /// <summary>Prepare to Die Edition (unpacked with UDSFM, no DCX compression).</summary>
    Ptde,

    /// <summary>Dark Souls Remastered (DCX-compressed files).</summary>
    Remastered,
}

/// <summary>State of the game executable, detected by SHA-256 (PTDE) or by file name (Remastered).</summary>
public enum ExeState
{
    Unknown,
    NotFound,
    Unpacked,
    UnpackedDebug,
    NotUnpacked,
    NotUnpackedDebug,
    Patched,
    PatchedDebug,
    Remastered,
}
