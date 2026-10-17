using System;

namespace KimLongEcoStay.UseCases.PluginInterfaces.StateStore
{
    public interface IStateStore
    {
        void AddStateChangeListeners(Action listeners);
        void RemoveStateChangeListeners(Action listeners);
        void BroadCastStateChange();
    }
}
