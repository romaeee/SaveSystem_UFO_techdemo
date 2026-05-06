public interface ISaveable
{
    int SaveOrder { get; }
    void CaptureState(SaveData saveData);
    void RestoreState(SaveData saveData);
}
