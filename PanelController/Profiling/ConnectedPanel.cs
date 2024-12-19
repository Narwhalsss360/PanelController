using PanelController.PanelObjects;
using PanelController.Controller;
using NStreamCom;

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

        public PanelInfo PanelInfo { get; private set; }

        public IChannel Channel;

        public event EventHandler<InterfaceUpdatedEventArgs>? InterfaceUpdated;

        private StreamCollector collector = new();

        public ConnectedPanel(Guid panelGuid, IChannel channel)
        {
            PanelGuid = panelGuid;
            if (Main.PanelsInfo.Find(panel => panel.PanelGuid == panelGuid) is not PanelInfo info)
                throw new KeyNotFoundException("The panel GUID was not found in the Panel Info collection.");
            Channel = channel;
            PanelInfo = info;
            channel.BytesReceived += BytesReceived;
            collector.Collector.StateChanged += CollectorStateChanged;
        }

        public async Task SendSourceData(uint interfaceID, object? sourceData)
        {
            throw new NotImplementedException();
        }

        private void BytesReceived(object? sender, byte[] bytes) => collector.Write(bytes);

        private void CollectorStateChanged(object? sender, Collector.StateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case Collector.States.Collected:
                    DataReady(collector.Collector.Data);
                    break;
                case Collector.States.MissingSize:
                case Collector.States.MissingData:
                    Logger.Log($"Communication error with panel {PanelInfo.Name}: {e.NewState}", Logger.Levels.Warning, $"ConnectedPanel: {PanelInfo.Name}");
                    break;
                case Collector.States.WaitingSize:
                case Collector.States.WaitingData:
                default:
                    break;
            }
        }

        private void DataReady(byte[] bytes)
        {
            if (bytes.Length == 0)
                return;

            ReceiveIDs id = (ReceiveIDs)bytes[0];

            if (id != ReceiveIDs.AnalogStateUpdate && id != ReceiveIDs.DigitalStateUpdate)
                return;
            uint interfaceID = BitConverter.ToUInt32(bytes, 1);

            switch (id)
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
