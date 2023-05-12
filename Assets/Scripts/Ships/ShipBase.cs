using BehaviorTree;
using UnityEditor.Timeline;
using UnityEngine;

namespace Ships
{
    interface IShipContext
    {
        public Rigidbody2D Body { get; }
        public Transform Trans { get; }
        public bool PrimaryAttackRecharged { get; }
        public float MaxRotation { get; }
        public float Acceleration { get; }
        public float Deceleration { get; }
        public float MaxSpeed { get; }

        public void PerformPrimaryAttack();
    }

    interface IShipControl
    {
        public Vector2 DirectionStick { get; }
        public bool PrimaryAttackPressed { get; }
        public bool AcceleratePressed { get; }
    }

    public class ShipBase : MonoBehaviour, IShipContext
    {
        [SerializeField] private TextAsset shipBehavior;
        
        private Rigidbody2D _body;
        private ShipContext _context;
        private BehaviorTree<ShipContext> _tree;

        [SerializeField] private float primaryAttackCooldown;
        private float _lastPrimaryAttack;
        [SerializeField] private Projectile primaryAttackProjectile;
        [SerializeField] private Transform[] primaryAttackGenerators;
        [SerializeField] private float rotationPerSecond;
        private float _rotationVelocity;
        
        [SerializeField] private float acceleration;
        [SerializeField] private float maxSpeed;
        [SerializeField] private float deceleration;

        private void Start()
        {
            _body = GetComponent<Rigidbody2D>();
            _context = new ShipContext()
            {
                Ship = this,
                Control = GetComponent<IShipControl>()
            };
            _tree = new BehaviorTree<ShipContext>(_context);
            _tree.RegisterNode("TestPrimaryAttackTrigger", typeof(NodeTestPriAttackTrigger));
            _tree.RegisterNode("TestPrimaryAttackTimer", typeof(NodeTestPriAttackTimer));
            _tree.RegisterNode("PrimaryAttack", typeof(NodePrimaryAttack));
            _tree.RegisterNode("Turn", typeof(NodeTurn));
            _tree.RegisterNode("Accelerate", typeof(NodeAccelerate));
            _tree.RegisterNode("TestTurnDirection", typeof(NodeTestTurnDirection));
            _tree.RegisterNode("TestTurnInput", typeof(NodeTestTurnInput));
            _tree.RegisterNode("SetupTurn", typeof(NodeSetRemainingTurn));
            _tree.RegisterNode("TestAccelerationInput", typeof(NodeTestAccInput));
            _tree.RegisterNode("Decelerate", typeof(NodeDecelerate));
            _tree.SetRoot(shipBehavior.text);
        }

        private void FixedUpdate()
        {
            _body.angularVelocity = 0;
            _tree.Tick();
        }

        public Rigidbody2D Body => _body;
        public Transform Trans => gameObject.transform;
        public bool PrimaryAttackRecharged => (Time.time - _lastPrimaryAttack) >= primaryAttackCooldown;
        public float MaxRotation => rotationPerSecond;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float MaxSpeed => maxSpeed;

        public void PerformPrimaryAttack()
        {
            foreach (var gen in primaryAttackGenerators)
            {
                var obj = Instantiate(primaryAttackProjectile);
                obj.Setup(gen.position, transform.rotation.eulerAngles.z);
            }
            _lastPrimaryAttack = Time.time;
        }
        
        private class ShipContext : TreeContext
        {
            public IShipContext Ship;
            public IShipControl Control;

            public float RemainingTurn;
        }
        
        private class NodeTestPriAttackTrigger : LeafNode<ShipContext>
        {
            public NodeTestPriAttackTrigger(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                return Context.Control.PrimaryAttackPressed ? Status.Success : Status.Failure;
            }
        }

        private class NodeTestPriAttackTimer : LeafNode<ShipContext>
        {
            public NodeTestPriAttackTimer(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                return Context.Ship.PrimaryAttackRecharged ? Status.Success : Status.Failure;
            }
        }

        private class NodePrimaryAttack : LeafNode<ShipContext>
        {
            public NodePrimaryAttack(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                Context.Ship.PerformPrimaryAttack();
                return Status.Success;
            }
        }

        private class NodeTestTurnInput : LeafNode<ShipContext>
        {
            public NodeTestTurnInput(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                return Context.Control.DirectionStick.magnitude.NearZero() ? Status.Failure : Status.Success;
            }
        }

        private class NodeSetRemainingTurn : LeafNode<ShipContext>
        {
            public NodeSetRemainingTurn(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                var stick = Context.Control.DirectionStick.normalized;
                var dir = Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg;
                Context.RemainingTurn = (dir - Context.Ship.Trans.eulerAngles.z).NormalizeAngle();
                return Status.Success;                
            }
        }

        private class NodeTestTurnDirection : LeafNode<ShipContext>
        {
            public NodeTestTurnDirection(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                return Context.RemainingTurn.NearZero() ? Status.Failure : Status.Success;
            }
        }

        private class NodeTurn : LeafNode<ShipContext>
        {
            public NodeTurn(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                var rate = Mathf.Clamp01(Mathf.Abs(Context.RemainingTurn) / (Context.Ship.MaxRotation * Time.fixedDeltaTime));
                Context.Ship.Body.angularVelocity = rate * Context.Ship.MaxRotation * Mathf.Sign(Context.RemainingTurn);
                return Status.Success;
            }
        }

        private class NodeTestAccInput : LeafNode<ShipContext>
        {
            public NodeTestAccInput(ShipContext context) : base(context) { }
            public override Status Tick()
            {
                return Context.Control.AcceleratePressed ? Status.Success : Status.Failure;
            }
        }

        private class NodeAccelerate : LeafNode<ShipContext>
        {
            public NodeAccelerate(ShipContext context) : base(context) { }

            public override Status Tick()
            {
                var angle = Context.Ship.Trans.eulerAngles.z * Mathf.Deg2Rad;
                var accel = Context.Ship.Acceleration * Time.fixedDeltaTime * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var speed = Context.Ship.Body.velocity + accel;
                if (speed.magnitude > Context.Ship.MaxSpeed)
                    speed = Context.Ship.MaxSpeed * speed.normalized;
                Context.Ship.Body.velocity = speed;
                return Status.Success;
            }
        }

        private class NodeDecelerate : LeafNode<ShipContext>
        {
            public NodeDecelerate(ShipContext context) : base(context) { }
            public override Status Tick()
            {
                var vel = Context.Ship.Body.velocity;
                if (vel.magnitude.NearZero()) return Status.Success;
                
                var speed = Mathf.Max(0, vel.magnitude - Context.Ship.Deceleration * Time.fixedDeltaTime);
                Context.Ship.Body.velocity = speed * vel.normalized;
                return Status.Success;
            }
        }
    }
}
