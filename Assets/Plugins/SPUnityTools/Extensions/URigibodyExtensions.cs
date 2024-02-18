using UnityEngine;

namespace SP.Tools.Unity
{
    public static class URigibodyExtensions
    {
        public static void AddForceX(this Rigidbody2D rb, float force) => rb.AddForce(new Vector2(force, 0));

        public static void AddForceY(this Rigidbody2D rb, float force) => rb.AddForce(new Vector2(0, force));

        public static void SetVelocity(this Rigidbody2D rb, float x, float y) => SetVelocity(rb, new Vector2(x, y));

        public static void SetVelocityX(this Rigidbody2D rb, float x) => SetVelocity(rb, new Vector2(x, rb.velocity.y));

        public static void SetVelocityY(this Rigidbody2D rb, float y) => SetVelocity(rb, new Vector2(rb.velocity.x, y));

        public static void AddVelocity(this Rigidbody2D rb, float x, float y) => SetVelocity(rb, new Vector2(rb.velocity.x + x, rb.velocity.y + y));

        public static void AddVelocity(this Rigidbody2D rb, Vector2 vec) => SetVelocity(rb, new Vector2(rb.velocity.x + vec.x, rb.velocity.y + vec.y));

        public static void AddVelocityX(this Rigidbody2D rb, float x) => SetVelocity(rb, new Vector2(rb.velocity.x + x, rb.velocity.y));

        public static void AddVelocityY(this Rigidbody2D rb, float y) => SetVelocity(rb, new Vector2(rb.velocity.x, rb.velocity.y + y));

        public static void SetVelocity(this Rigidbody2D rb, Vector2 newVelo) => rb.velocity = newVelo;

        public static void SetVelocityNormalized(this Rigidbody2D rb, float x, float y) => SetVelocityNormalized(rb, new Vector2(x, y));

        public static void SetVelocityNormalized(this Rigidbody2D rb, Vector2 newVelo) => rb.velocity = newVelo.normalized;
    }
}
