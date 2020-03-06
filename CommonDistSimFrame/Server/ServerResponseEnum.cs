namespace CommonDistSimFrame.Server {
#pragma warning disable S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
    public enum ServerResponseEnum {
#pragma warning restore S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
        NothingToDo,
        ServeCalcJob,
        ServeLpgFiles,
        JobFinishAck,
    }
}