using UnityEngine;
using UnityEngine.UI;
public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance;

    [Header("Shop Controls")]
    public Button rerollButton;

    private void Awake()
    {
        Instance = this;
    }

    // ✅ REMOVE THE START METHOD ENTIRELY
    // private void Start() { ... }
}
