using PanelController.PanelObjects;
using PanelController.Profiling;
using System.Collections.ObjectModel;
using NStreamCom;
using static PanelController.Profiling.ConnectedPanel;
using System.Diagnostics;
using PanelController.PanelObjects.Properties;
using System.Collections.Specialized;

namespace PanelController.Controller
{
    public static class Main
    {
        public static ObservableCollection<ConnectedPanel> ConnectedPanels = new();

        public static ObservableCollection<Profile> Profiles = new();

        public static ObservableCollection<PanelInfo> PanelsInfo = new();

        private static int s_selectedProfileIndex = -1;

        public static int SelectedProfileIndex
        {
            get { return s_selectedProfileIndex; }
            set { s_selectedProfileIndex = value; }
        }

        public static Profile? CurrentProfile
        {
            get => s_selectedProfileIndex < 0 ? null : Profiles[s_selectedProfileIndex];
            set
            {
                if (value is null)
                {
                    SelectedProfileIndex = -1;
                    return;
                }

                for (int i = 0; i < Profiles.Count; i++)
                {
                    if (ReferenceEquals(value, Profiles[i]))
                    {
                        SelectedProfileIndex = i;
                        return;
                    }
                }

                Profiles.Add(value);
                SelectedProfileIndex = Profiles.Count - 1;
            }
        }

        private static bool s_isInitialized;

        public static bool IsInitialized { get => s_isInitialized; }

        public static event EventHandler? Initialized;

        public static event EventHandler? Deinitialized;

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
            PanelsInfo.CollectionChanged += EnsureUniqeGuids;
        }

        private static void EnsureUniqeGuids(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if ((args.Action == NotifyCollectionChangedAction.Replace || args.Action == NotifyCollectionChangedAction.Add) && args.NewItems is not null)
            {
                foreach (var newInfoObj in args.NewItems)
                {
                    if (newInfoObj is not PanelInfo newInfo)
                        continue;

                    for (int i = 0; i < PanelsInfo.Count; i++)
                    {
                        if (ReferenceEquals(PanelsInfo[i], newInfo))
                            continue;
                        if (PanelsInfo[i].PanelGuid != newInfo.PanelGuid)
                            continue;
                        PanelsInfo.Remove(PanelsInfo[i]);
                    }
                }
            }
        }

        public static PanelInfo? FindPanelInfo(this Guid guid)
        {
            for (int i = 0; i < PanelsInfo.Count; i++)
                if (PanelsInfo[i].PanelGuid == guid)
                    return PanelsInfo[i];
            return null;
        }

        public static PanelInfo? FindPanelInfo(this string panelName)
        {
            for (int i = 0; i < PanelsInfo.Count; i++)
                if (PanelsInfo[i].Name == panelName)
                    return PanelsInfo[i];
            return null;
        }

        public static string PanelInfoOrGuid(this Guid guid)
        {
            if (guid.FindPanelInfo() is PanelInfo panelInfo)
                return panelInfo.Name;
            return guid.ToString();
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
                info.DigitalCount = BitConverter.ToUInt32(data, 16);
                info.AnalogCount = BitConverter.ToUInt32(data, 20);
                info.DisplayCount = BitConverter.ToUInt32(data, 24);
                info.PanelGuid = guid;

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
            Thread.Sleep(1000);
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
            Logger.Log($"Received Giud:{args.PanelGuid} Type:{args.InterfaceType} ID:{args.InterfaceID} state:{args.State}", Logger.Levels.Debug, "Main");
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
            PanelsInfo.CollectionChanged -= EnsureUniqeGuids;
            s_RefreshThread?.Join();
            Logger.Log($"Deinitialized", Logger.Levels.Info, "Main");
        }
    }
}
