namespace GameCore
{
    public interface IState
    {
        void OnEnter();
        void OnUpdate();
        void OnExit();
    }

    public class StateMachine
    {
        public IState currentState; //当前状态
        public Entity entity;

        public StateMachine(Entity entity)
        {
            this.entity = entity;
        }

        public void ChangeState(IState state)
        {
            currentState?.OnExit();

            currentState = state;
            currentState.OnEnter();
        }


        public void Update()
        {
            currentState?.OnUpdate();
        }
    }
}