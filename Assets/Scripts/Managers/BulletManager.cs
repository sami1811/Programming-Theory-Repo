
public class BulletManager : PoolingSystem
{
    public static BulletManager Instance {  get; private set; }

    protected override void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        base.Awake();
    }
}
