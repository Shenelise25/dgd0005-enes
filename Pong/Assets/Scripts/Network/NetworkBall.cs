using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class NetworkBall : NetworkBehaviour
{
    public static NetworkBall Instance { get; private set; }
    public readonly SyncVar<bool> InPlay = new SyncVar<bool>();
    [SerializeField] private float speed = 7f;
    private Vector3 velocity;
    private void Awake() { Instance = this; InPlay.OnChange += PlayChanged; }
    public override void OnStartServer() { Instance = this; ResetBall(); }
    public override void OnStartClient() { Instance = this; Paint(); }
    private void OnDestroy() { InPlay.OnChange -= PlayChanged; if (Instance == this) Instance = null; }
    private void PlayChanged(bool previous, bool next, bool asServer) => Paint();
    private void Paint() => Court.Paint(gameObject, InPlay.Value ? Color.white : Color.gray);
    private void ResetBall()
    {
        velocity = Vector3.zero;
        InPlay.Value = false;
        transform.position = new Vector3(0,Court.BallY,0);
        Paint();
    }
    public void Serve(int towardSide)
    {
        if (!IsServerInitialized || InPlay.Value || (towardSide != 0 && towardSide != 1)) return;
        velocity = new Vector3(towardSide == 0 ? -1 : 1,0,Random.Range(-.6f,.6f)).normalized * speed;
        InPlay.Value = true;
    }
    private void Update()
    {
        if (!IsServerInitialized) return;
        if (!InPlay.Value) return;
        // Small substeps preserve the Starter collision rule even during a slow frame.
        int steps = Mathf.Max(1,Mathf.CeilToInt(speed * Time.deltaTime / .2f));
        float dt = Time.deltaTime / steps;
        for (int i = 0; i < steps && InPlay.Value; i++) MoveBall(dt);
    }
    private void MoveBall(float dt)
    {
        Vector3 p = transform.position + velocity * dt;
        float zLimit = Court.HalfHeight - Court.BallRadius;
        if (Mathf.Abs(p.z) > zLimit) { p.z = Mathf.Sign(p.z)*zLimit; velocity.z = -velocity.z; }
        foreach (PaddleController paddle in PaddleController.All)
        {
            if (paddle == null || !paddle.IsServerInitialized || paddle.Side.Value == 2) continue;
            Vector3 pp = paddle.transform.position;
            bool toward = (pp.x < 0 && velocity.x < 0) || (pp.x > 0 && velocity.x > 0);
            if (!toward) continue;
            if (Mathf.Abs(p.x-pp.x) < .5f && Mathf.Abs(p.z-pp.z) < Court.PaddleHalfLength+Court.BallRadius)
            {
                velocity.x = -velocity.x;
                velocity.z += (p.z-pp.z)*3f;
                velocity = velocity.normalized*speed;
                p.x = pp.x+(pp.x < 0 ? .5f : -.5f);
            }
        }
        transform.position = p;
        if (p.x < -Court.HalfWidth-.5f) Score(1);
        else if (p.x > Court.HalfWidth+.5f) Score(0);
    }
    private void Score(int side)
    {
        GameState.Instance.Goal(side);
        ResetBall();
    }
}
