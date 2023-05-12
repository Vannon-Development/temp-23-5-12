using UnityEngine;

namespace Ships
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed;
        private Rigidbody2D _body;

        private Vector3 _pos;
        private float _rot;
        private Vector2 _vel;

        public void Setup(Vector2 pos, float rotation)
        {
            _pos = pos;
            _rot = rotation;
        }

        private void Start()
        {
            _body = GetComponent<Rigidbody2D>();
            transform.SetPositionAndRotation(_pos, Quaternion.Euler(0, 0, _rot));
            var rotRad = _rot * Mathf.Deg2Rad;
            _vel = new Vector2(Mathf.Cos(rotRad), Mathf.Sin(rotRad)) * speed;
            Invoke(nameof(Timeup), 1.0f);
        }

        private void FixedUpdate()
        {
            _body.velocity = _vel;
        }

        private void Timeup()
        {
            Destroy(gameObject);
        }
        
    }
}
