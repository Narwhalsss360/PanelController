using PanelController.PanelObjects;

namespace PanelController.Profiling
{
    public class ConnectedPanel
    {
        public enum ReceiveIDs : byte
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

        public ConnectedPanel(Guid panelGuid, IChannel channel)
        {
            PanelGuid = panelGuid;
            Channel = channel;
            channel.BytesReceived += BytesReceived;
        }

        public async Task SendSourceData(uint interfaceID, object? sourceData)
        {
            throw new NotImplementedException();
        }

        private void BytesReceived(object? sender, byte[] bytes)
        {
        }

        private void DataReady(object? sender, EventArgs args)
        {
            byte[] bytes = Array.Empty<byte>();
            if (bytes.Length == 0)
                return;

            byte id = bytes[0];

            if (id != (byte)ReceiveIDs.AnalogStateUpdate || id != (byte)ReceiveIDs.DigitalStateUpdate)
                return;

            uint interfaceID = BitConverter.ToUInt32(bytes, 1);
            switch ((ReceiveIDs)id)
            {
                case ReceiveIDs.DigitalStateUpdate:
                    if (bytes.Length != 6)
                        return;
                    bool activate = BitConverter.ToBoolean(bytes, 5);
                    InterfaceUpdated?.Invoke(this, new InterfaceUpdatedEventArgs(PanelGuid, InterfaceTypes.Digital, interfaceID, activate));
                    break;
                case ReceiveIDs.AnalogStateUpdate:
                    byte[] data = new byte[bytes.Length - 5];
                    Array.Copy(bytes, 5, data, 0, data.Length);
                    if (!IPanelSettable.SettableValue.IsValidSettableData(data))
                        return;
                    InterfaceUpdated?.Invoke(this, new InterfaceUpdatedEventArgs(PanelGuid, InterfaceTypes.Digital, interfaceID, new IPanelSettable.SettableValue(data)));
                    break;
                default:
                    break;
            }
        }
    }
}
