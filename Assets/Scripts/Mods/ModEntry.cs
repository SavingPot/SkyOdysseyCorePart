using UnityEngine;

namespace GameCore
{
    public abstract class ModEntry
    {
        public Mod datum;

        public virtual void OnLoaded() { }
        public virtual void OnReconfigured() { }
        public virtual void OnUnloaded() { }//TODO: Finish
    }
}
