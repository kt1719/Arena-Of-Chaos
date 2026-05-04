using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public class LobbyController : MonoBehaviour
{
    // ===== Serialized Fields =====
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button startRoundButton;

    // ===== Private Fields =====
    private Canvas _canvas;
    private GraphicRaycaster _raycaster;

    // ===== Unity Methods =====

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _raycaster = GetComponent<GraphicRaycaster>();
    }

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        clientButton.onClick.AddListener(OnClientClicked);
        startRoundButton.onClick.AddListener(OnStartRoundClicked);
    }

    private void Update()
    {
        bool show = !IsRoundActive();
        if (_canvas.enabled != show)
        {
            _canvas.enabled = show;
            _raycaster.enabled = show;
        }
    }

    // ===== Button Handlers =====

    private void OnHostClicked()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.StartGameHost();
    }

    private void OnClientClicked()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.StartGameClient();
    }

    private void OnStartRoundClicked()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.StartRound();
    }

    // ===== Helpers =====

    private bool IsRoundActive()
    {
        return GameManager.Instance != null
            && GameManager.Instance.Object != null
            && GameManager.Instance.Object.IsValid
            && GameManager.Instance.RoundStarted;
    }
}
