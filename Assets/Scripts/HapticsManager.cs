using UnityEngine;

public class HapticsManager : MonoBehaviour
{
    public static HapticsManager Instance { get; private set; }

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _HapticLight();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _HapticMedium();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _HapticSuccess();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _HapticError();
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PieceMove() => Fire(0); // light tap on each swap
    public void PieceSnap() => Fire(1); // piece lands in correct slot
    public void Win()       => Fire(2); // puzzle solved
    public void Lose()      => Fire(3); // time ran out

    private void Fire(int type)
    {
#if UNITY_IOS && !UNITY_EDITOR
        switch (type)
        {
            case 0: _HapticLight();   break;
            case 1: _HapticMedium();  break;
            case 2: _HapticSuccess(); break;
            case 3: _HapticError();   break;
        }
#endif
    }
}
