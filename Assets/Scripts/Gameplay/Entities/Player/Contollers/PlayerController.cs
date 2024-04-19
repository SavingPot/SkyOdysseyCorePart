namespace GameCore
{
    public abstract class PlayerController
    {
        public Player player { get; }



        public abstract bool Jump();
        public abstract bool HoldingJump();
        public abstract float Move();
        public abstract PlayerOrientation SetPlayerOrientation();
        public abstract bool ClickingAttack();
        public abstract bool HoldingAttack();



        public enum PlayerOrientation
        {
            Right,
            Left,
            Previous
        }



        public PlayerController(Player player)
        {
            this.player = player;
        }
    }
}