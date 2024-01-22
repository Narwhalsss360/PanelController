using PanelController.PanelObjects;
using NStreamCom;

namespace PanelController.Profiling
{
    public class ConnectedPanel
    {
        enum ReceiveIDs
        {
            Handshake,
            DigitalStateUpdate,
            AnalogStateUpdate
        }

        public class InterfaceUpdatedEventArgs : EventArgs
        {
            public readonly Guid PanelGuid;

            public readonly InterfaceTypes InterfaceType;

            public readonly uint InterfaceID;

            public readonly object State;

            public InterfaceUpdatedEventArgs(Guid panelGuid, InterfaceTypes interfaceType, uint interfaceID, object state)
            {
                PanelGuid = panelGuid;
                InterfaceType = interfaceType;
                InterfaceID = interfaceID;
                State = state;
            }
        }

        public Guid PanelGuid;

        public IChannel Channel;

        public event EventHandler<InterfaceUpdatedEventArgs>? InterfaceUpdated;

        private PacketCollector _collector = new();

        public ConnectedPanel(Guid panelGuid, IChannel channel)
        {
            PanelGuid = panelGuid;
            Channel = channel;
            _collector.PacketsReady += PacketsCollected;
        }

        public async Task SendSourceData(uint interfaceID, object? sourceData)
        {
            throw new NotImplementedException();
        }

        private void BytesReceived(byte[] bytes)
        {
            try
            {
                _collector.Collect(bytes);
            }
            catch (PacketsLost) { }
            catch (SizeMismatch) { }
        }

        private void PacketsCollected(object sender, PacketsReadyEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
