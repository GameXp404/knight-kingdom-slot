using UnityEngine;

public class AutoSpinController : MonoBehaviour
{
    public int RemainingSpins { get; private set; }
    public bool IsActive => RemainingSpins > 0;
    public System.Action<int> OnRemainingChanged;

    public void Begin(int count) { RemainingSpins = count; OnRemainingChanged?.Invoke(RemainingSpins); }
    public void Cancel() { RemainingSpins = 0; OnRemainingChanged?.Invoke(RemainingSpins); }
    public void Decrement() { if (RemainingSpins > 0) { RemainingSpins--; OnRemainingChanged?.Invoke(RemainingSpins); } }
}
