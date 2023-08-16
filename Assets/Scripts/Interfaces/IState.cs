using GameCore.High;

namespace GameCore
{
    public interface IState<T> : IOnEnter, IOnExit, IOnUpdate
    {
        T machine { get; set; }
    }
}
