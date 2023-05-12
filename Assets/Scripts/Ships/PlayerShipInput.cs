using UnityEngine;
using UnityEngine.InputSystem;

namespace Ships
{
    public class ShipShipInput : MonoBehaviour, IShipControl
    {
        private Vector2 _direction;
        private bool _primaryAttack;
        private bool _accelerate;

        public Vector2 DirectionStick => _direction;
        public bool PrimaryAttackPressed => _primaryAttack;
        public bool AcceleratePressed => _accelerate;

        private void OnDirection(InputValue value)
        {
            _direction = value.Get<Vector2>();
        }

        private void OnPrimaryAttack(InputValue value)
        {
            _primaryAttack = !value.Get<float>().NearZero();
        }

        private void OnAccelerate(InputValue value)
        {
            _accelerate = !value.Get<float>().NearZero();
        }
    }
}
