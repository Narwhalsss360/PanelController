using PanelController.PanelObjects;
using NStreamCom;
using PanelController.Controller;

namespace PanelController.Profiling
{
    public class ConnectedPanel
    {
        public enum ReceiveIDs
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

        private readonly PacketCollector _collector = new();

        public ConnectedPanel(Guid panelGuid, IChannel channel)
        {
            PanelGuid = panelGuid;
            Channel = channel;
            _collector.PacketsReady += PacketsCollected;
            channel.BytesReceived += BytesReceived;
        }

        public async Task SendSourceData(uint interfaceID, object? sourceData)
        {
            throw new NotImplementedException();
        }

        private void BytesReceived(object? sender, byte[] bytes)
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
            Message message = new(args.Packets);
            switch ((ReceiveIDs)message.ID)
            {
                case ReceiveIDs.DigitalStateUpdate:
                    if (message.Data.Length != 5)
                        return;
                    uint interfaceID = BitConverter.ToUInt32(message.Data, 0);
                    bool activate = BitConverter.ToBoolean(message.Data, 4);
                    InterfaceUpdated?.Invoke(this, new InterfaceUpdatedEventArgs(PanelGuid, InterfaceTypes.Digital, interfaceID, activate));
                    break;
                case ReceiveIDs.AnalogStateUpdate:
                    break;
                default:
                    break;
            }
        }
    }
}
