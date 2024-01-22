using PanelController.PanelObjects;
using PanelController.Profiling;
using System.Collections.ObjectModel;
using NStreamCom;
using static PanelController.Profiling.ConnectedPanel;
using System.Diagnostics;
using PanelController.PanelObjects.Properties;

namespace PanelController.Controller
{
    public static class Main
    {
        public static ObservableCollection<ConnectedPanel> ConnectedPanels = new();

        public static ObservableCollection<Profile> Profiles = new();

        private static int s_selectedProfileIndex;

        public static int SelectedProfileIndex
        {
            get { return s_selectedProfileIndex; }
            set { s_selectedProfileIndex = value; }
        }

        public static Profile? CurrentProfile { get => s_selectedProfileIndex < 0 ? null : Profiles[s_selectedProfileIndex]; }

        public static List<PanelInfo> PanelsInfo = new();

        private static bool s_isInitialized;

        public static bool IsInitialized { get => s_isInitialized; }

        public static EventHandler? Initialized;

        public static EventHandler? Deinitialized;

        private static CancellationTokenSource s_deinitializects = new();

        public static CancellationToken DeinitializedCancellationToken { get => s_deinitializects.Token; }

        private static Thread? s_RefreshThread;

        public static void Initialize()
        {
            if (s_isInitialized)
                return;
            s_isInitialized = true;
            s_RefreshThread = new Thread(RefreshConnectedPanelsThread);
            s_RefreshThread.Start();
            Process.GetCurrentProcess().Exited += ProcessExited;
            Logger.Log($"Initialized, PID: {Process.GetCurrentProcess().Id}", Logger.Levels.Info, "Main");
        }

        public static void Handshake(IChannel channel)
        {
            if (!channel.IsOpen)
            {
                if (channel.Open() is object result)
                {
                    Logger.Log($"A problem occured openning channel {channel.GetItemName()}: {result}.", Logger.Levels.Info, "Channel Handshaker");
                    return;
                }

                if (!channel.IsOpen)
                {
                    Logger.Log("The channel could not open.", Logger.Levels.Debug, "Channel Handshaker");
                    return;
                }
            }

            CancellationTokenSource cts = new();
            void receiver(object? sender, byte[] data)
            {
                // Data: { 16-Bytes Guid} { 4 bytes Digital Count } { 4 bytes Analog Count } { 4 bytes Display Count }
                if (data.Length != 28)
                    return;

                channel.BytesReceived -= receiver;
                cts.Cancel();

                Guid guid = new Guid(data.Take(16).ToArray());
                PanelInfo info = new();
                info.InterfaceCount[InterfaceTypes.Digital] = BitConverter.ToUInt32(data, 16);
                info.InterfaceCount[InterfaceTypes.Analog] = BitConverter.ToUInt32(data, 20);
                info.InterfaceCount[InterfaceTypes.Display] = BitConverter.ToUInt32(data, 24);

                ConnectedPanels.Add(new(guid, channel));
                PanelsInfo.Add(info);
                Logger.Log($"Connected panel ({guid}) through {channel.GetItemName()}", Logger.Levels.Info, "Channel Handshaker");
            }

            channel.BytesReceived += receiver;
            channel.Send(new Message(0, new byte[] { }).GetPackets(1).GetPacketsBytes()[0]);

            Task delay = Task.Delay(1000);
            var token = cts.Token;
            try
            {
                delay.Wait(token);
            }
            catch (OperationCanceledException e)
            {
                if (e.CancellationToken != token)
                    throw;
                return;
            }
            channel.BytesReceived -= receiver;
        }

        public static async Task HandshakeAsync(IChannel channel) => await Task.Run(() => { Handshake(channel); }, DeinitializedCancellationToken);

        public static void RefreshConnectedPanels()
        {
            List<Task> handshakers = new();
            for (int i = 0; i < Extensions.Detectors.Count; i++)
            {

                if (!Extensions.Detectors[i].Item1)
                    continue;
                foreach (IChannel detectedChannel in Extensions.Detectors[i].Item3())
                    handshakers.Add(HandshakeAsync(detectedChannel));
            }
            Task whenAll = Task.WhenAll(handshakers);
            whenAll.Wait(DeinitializedCancellationToken);
        }

        public static async Task RefreshConnectedPanelsAsync() => await Task.Run(RefreshConnectedPanels, DeinitializedCancellationToken);

        private static void RefreshConnectedPanelsThread()
        {
            CancellationToken cancellationToken = DeinitializedCancellationToken;
            while (!cancellationToken.IsCancellationRequested)
                RefreshConnectedPanels();
        }

        public static void SendSourcesData()
        {
            if (CurrentProfile is null)
                return;

            foreach (var panel in ConnectedPanels)
            {
                if (!CurrentProfile.MappingsByGuid.ContainsKey(panel.PanelGuid))
                    continue;

                foreach (var mapping in CurrentProfile.MappingsByGuid[panel.PanelGuid])
                {
                    foreach (var obj in mapping.Objects)
                    {
                        if (obj is not IPanelSource source)
                            continue;
                        panel.SendSourceData(mapping.InterfaceID, source.Get()).Wait();
                    }
                }
            }
        }

        public static async Task SendSourcesDataAsync() => await Task.Run(SendSourcesData, DeinitializedCancellationToken);

        public static void InterfaceUpdated(object sender, InterfaceUpdatedEventArgs args)
        {
            if (CurrentProfile is null)
                return;
            CurrentProfile.FindMapping(args.PanelGuid, args.InterfaceType, args.InterfaceID, args.State)?.Execute(args.State);
        }

        private static void ProcessExited(object? sender, EventArgs args)
        {
            Deinitialize();
        }

        public static void Deinitialize()
        {
            if (!s_isInitialized)
                return;
            s_isInitialized = false;
            s_deinitializects.Cancel();
            Process.GetCurrentProcess().Exited -= ProcessExited;
            s_RefreshThread?.Join();
            Logger.Log($"Deinitialized", Logger.Levels.Info, "Main");
        }
    }
}
