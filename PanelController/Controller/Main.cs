using PanelController.PanelObjects;
using PanelController.Profiling;
using System.Collections.ObjectModel;
using static PanelController.Profiling.ConnectedPanel;

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

        private static bool s_isInitialized;

        public static bool IsInitialized { get => s_isInitialized; }

        public static EventHandler? Initialized;

        public static EventHandler? Deinitialized;

        private static CancellationTokenSource s_deinitializects = new();

        public static CancellationToken DeinitializedCancellationToken { get => s_deinitializects.Token; }

        public static void Initialize()
        {
            throw new NotImplementedException();
        }

        public static void Handshake(IChannel channel)
        {

        }

        public static async Task HandshakeAsync(IChannel channel) => await Task.Run(() => { Handshake(channel); }, DeinitializedCancellationToken);

        public static void RefreshConnectedPanels()
        {

        }

        public static async Task RefreshConnectedPanelAsync() => await Task.Run(RefreshConnectedPanels, DeinitializedCancellationToken);

        public static void SendSourcesData()
        {

        }

        public static async Task SendSourcesDataAsync() => await Task.Run(SendSourcesData, DeinitializedCancellationToken);

        public static void InterfaceUpdated(object sender, InterfaceUpdatedEventArgs args)
        {
            throw new NotImplementedException();
        }

        public static void Deinitialize()
        {
            throw new NotImplementedException();
        }
    }
}
