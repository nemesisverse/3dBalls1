public interface ISwitchable
{
    bool IsPaused { get; set; }
    int SavedIndex { get; }
}