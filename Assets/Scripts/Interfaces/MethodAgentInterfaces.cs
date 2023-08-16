using System;

namespace GameCore
{
    public interface IOnUpdate
    {
        void OnUpdate();
    }
    
    public interface IUpdate
    {
        void Update();
    }

    public interface IFixedUpdate
    {
        void FixedUpdate();
    }
}
