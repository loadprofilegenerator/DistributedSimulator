namespace CommonDistSimFrame.Common
{
#pragma warning disable S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
    public enum ClientRequestEnum
#pragma warning restore S2344 // Enumeration type names should not have "Flags" or "Enum" suffixes
    {
        RequestForJob,
        RequestForLPGFiles,
        ReportFinish,
        /*R,
        FinishReport,
        FinishAcknowledgement,
        AlreadyExistReport,
        AlreadyExistReportAck,
        CalcFailReport,
        CalcFailReportAck,
        NothingToDo,
        WorkingDirFull,
        WorkingDirFullAck,
        ArchiveDirFull,
        ArchiveDirFullAck*/
        ReportDiskspaceFull,
        ReportFailure,
    }
}
