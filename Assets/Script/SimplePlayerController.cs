using UnityEngine;
using Unity.Netcode;

public class SimplePlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float speed = 5.0f;
    private Rigidbody rb;
    private Vector2 moveInput;

    [Header("Combat Stats")]
    public int maxHealth = 100;
    public NetworkVariable<int> health = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> attack = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Skin Customization")]
    [SerializeField] private GameObject[] mainBodies;
    [SerializeField] private GameObject[] bodyParts;
    [SerializeField] private GameObject[] eyes;
    [SerializeField] private GameObject[] gloves;
    [SerializeField] private GameObject[] headParts;
    [SerializeField] private GameObject[] mouthAndNoses;
    [SerializeField] private GameObject[] tails;

    public NetworkVariable<int> mainBodyIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> bodyPartIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> eyeIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> gloveIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> headPartIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> mouthAndNoseIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> tailIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
            attack.Value = 1;
        }

        mainBodyIndex.OnValueChanged += OnMainBodyChanged;
        bodyPartIndex.OnValueChanged += OnBodyPartChanged;
        eyeIndex.OnValueChanged += OnEyeChanged;
        gloveIndex.OnValueChanged += OnGloveChanged;
        headPartIndex.OnValueChanged += OnHeadPartChanged;
        mouthAndNoseIndex.OnValueChanged += OnMouthAndNoseChanged;
        tailIndex.OnValueChanged += OnTailChanged;

        OnMainBodyChanged(0, mainBodyIndex.Value);
        OnBodyPartChanged(-1, bodyPartIndex.Value);
        OnEyeChanged(0, eyeIndex.Value);
        OnGloveChanged(-1, gloveIndex.Value);
        OnHeadPartChanged(-1, headPartIndex.Value);
        OnMouthAndNoseChanged(0, mouthAndNoseIndex.Value);
        OnTailChanged(-1, tailIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        mainBodyIndex.OnValueChanged -= OnMainBodyChanged;
        bodyPartIndex.OnValueChanged -= OnBodyPartChanged;
        eyeIndex.OnValueChanged -= OnEyeChanged;
        gloveIndex.OnValueChanged -= OnGloveChanged;
        headPartIndex.OnValueChanged -= OnHeadPartChanged;
        mouthAndNoseIndex.OnValueChanged -= OnMouthAndNoseChanged;
        tailIndex.OnValueChanged -= OnTailChanged;
    }

    void Update()
    {
        if (!IsOwner || !enabled) return;
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");
    }

    void FixedUpdate()
    {
        if (!IsOwner || !enabled) return;
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        rb.linearVelocity = move * speed;
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        health.Value -= damage;
        if (health.Value <= 0)
        {
            health.Value = maxHealth;
        }
    }

    private void UpdatePart(GameObject[] parts, int newIndex)
    {
        foreach (var part in parts) part.SetActive(false);
        if (newIndex >= 0 && newIndex < parts.Length)
        {
            parts[newIndex].SetActive(true);
        }
    }

    private void OnMainBodyChanged(int prev, int current) => UpdatePart(mainBodies, current);
    private void OnBodyPartChanged(int prev, int current) => UpdatePart(bodyParts, current);
    private void OnEyeChanged(int prev, int current) => UpdatePart(eyes, current);
    private void OnGloveChanged(int prev, int current) => UpdatePart(gloves, current);
    private void OnHeadPartChanged(int prev, int current) => UpdatePart(headParts, current);
    private void OnMouthAndNoseChanged(int prev, int current) => UpdatePart(mouthAndNoses, current);
    private void OnTailChanged(int prev, int current) => UpdatePart(tails, current);

    [ServerRpc] public void ChangeMainBodyServerRpc(int newIndex) { if (newIndex >= 0 && newIndex < mainBodies.Length) mainBodyIndex.Value = newIndex; }
    [ServerRpc] public void ChangeBodyPartServerRpc(int newIndex) { if (newIndex >= -1 && newIndex < bodyParts.Length) bodyPartIndex.Value = newIndex; }
    [ServerRpc] public void ChangeEyeServerRpc(int newIndex) { if (newIndex >= 0 && newIndex < eyes.Length) eyeIndex.Value = newIndex; }
    [ServerRpc] public void ChangeGloveServerRpc(int newIndex) { if (newIndex >= -1 && newIndex < gloves.Length) gloveIndex.Value = newIndex; }
    [ServerRpc] public void ChangeHeadPartServerRpc(int newIndex) { if (newIndex >= -1 && newIndex < headParts.Length) headPartIndex.Value = newIndex; }
    [ServerRpc] public void ChangeMouthAndNoseServerRpc(int newIndex) { if (newIndex >= 0 && newIndex < mouthAndNoses.Length) mouthAndNoseIndex.Value = newIndex; }
    [ServerRpc] public void ChangeTailServerRpc(int newIndex) { if (newIndex >= -1 && newIndex < tails.Length) tailIndex.Value = newIndex; }
}