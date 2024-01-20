# Panel Controller
I/O Routing

## Desgin Focuses
* Extensible
* Customizable (by user)
* Lightweight
* Simple user experience

# Outline
This outlines the structure of the program.

## Defenitions
ItemName: attribute of an object that defines a user friendly name *OR* defines property that returns the user friendly name.

object-result: an return value that is any object, usage:
* Exception is thrown: return value is the exception
* Other unexepected/invalid: return value is a descriptive string
* Anything else: value

## Class Library: NStreamCom
*dependency*

## Class Library: PanelController
* namespace **PanelObjects**
    * `interface IPanelObject : IFormmatable`
        * Properties
            * `string Status`: General status of object (Default: "OK")
        * Methods
            * `string? ToString()`: returns ItemName
            * `string IFormmatable.ToString(string? _, IFormatProvider? _)`: returns ItemName
    * `interface IPanelAction : IPanelObject`
        * Methods
            * `object? Run()`: Run the action, returns an object-result
    * `interface IPanelSettable : IPanelObject`
         * Methods
            * `object? Set(object? value)`: Set a value, returns an object-result
    * `interface IPanelSource : IPanelObject`
        * Methods
            * `object? Get()`: Get a value, returns object-result
    * `interface IChannel : IPanelObject`
        * Types
            * `delegate void IChannel[] Detect()`: Delegate that detects when new channels are available and returns then new open channels
            * `class DetectorAttribute : Attribute`: Attribute to be applied to public, static functions that fit the Detect delegate type
        * Properties
            * `bool IsOpen`: Whether or not the channel is open for communications.
        * Events
            * `EventHandler<byte[]> BytesReceived`: Event that gets invoked when bytes are received from the channel
        * Methods
            * `object? Open()`: Attempt to open the communication channel, returns object-result
            * `object? Send(byte[] data)`: Attempt to send data through the channel, returns object-result
            * `void Close()`: Close the communication channel
    * `static class MethodInfoExtensions`
        * Methods
            * `bool IsDetector(this MethodInfo method)`: Whether `method` fits the criteria of `delegate Detect` and is `public static`
    * `static class TypeExtensions`
        * Methods
            * `string GetItemName(this Type type)`: Get ItemName with `type.Name` as fallback
            * `bool Implements<T>(this Type type)`: Whether the type implenents the interface `T`
    * namespace **Properties**
        * `class ItemNameAttribute : Attribute`: Applied to delclarations for user-friendly names
        * `static class ItemNameObjectExtensions`
            * Methods
                * `string GetItemName(this object obj)`: Get the ItemName of `obj` for fallback to `ToString()`
        * `class ItemDescriptionAttribute : Attribute`: Applied to delclarations for user descriptions
        * `class UserPropertyAttribute : Attribute` Applied to *properties* that the user may edit
        * `class RangedUserPropertyAttribute : Attribute` Applied to *properties* that the user may edit within a specific range
        * `class ConstrainedUserPropertyAttribute : Attribute` Applied to *properties* that the user may edit to specific values
        * `class RegexConstrainedUserPropertyAttribute : Attribute`
        * `class PanelExtensionAttribute : Attribute`
        * `class AutoLaunchAttribute : Attribute`
* namespace **Profiling**
    * `enum InterfaceTypes`
        * `Digital`
        * `Analog`
        * `Display`
    * `class ConnectedPanel`
        * Types
            * `class InterfaceUpdatedEventArgs : EventArgs`
                * Members
                    * `readonly InterfaceTypes InterfaceType`
                    * `readonly uint InterfaceID`
                    * `object State`
            * `enum ReceiveIDs`
                * `Handshake`
                * `DigitalStateUpdate`
                * `AnalogStateUpdate`
        * Members
            * `IChannel Channel`: The channel the panel is communicating through
            * `Guid PanelGuid`: Guid of the panel
        * Events
            * `EventHandler<InterfaceUpdatedEventArgs> InterfaceUpdated`
        * Methods
            * `ctor(IChannel channel, Guid panelGuid)`
            * `async Task SendSourceData(uint interfaceID, object? sourceData)`
    * `class Macro`
    * `class Mapping`
        * Members
            * `Guid PanelGuid`
            * `InterfaceTypes InterfaceType`
            * `uint InterfaceID`
            * `object? InterfaceOption`
            * `Macro Macro`
    * `class Profile`
        * Members
            * `string Name`
            * `Dictionary<Guid, Mapping> MappingsByPanel`
        * Properties
            * `Guid[] PanelGuids`
            * `Mapping[] Mappings`
        * Methods
            * `void AddMapping(Mapping)`
            * `void RemoveMapping(Mapping)`
            * `Mapping? FindMapping(Guid guid, InterfaceTypes interfaceType, uint interfaceID, object? interfaceOption = null)`
    * `class PanelInfo`
        * Members
            * `Guid Guid`
            * `string Name`
            * `Dictionary<InterfaceTypes, uint> InterfaceCount`
* namespace **Controller**
    * `static class Logger`
        * Types
            * `enum Levels`
                * `Error`
                * `Warning`
                * `Info`
                * `Debug`
            * `readonly struct HistoricalLog`
                * Members
                    * `readonly string Message`
                    * `readonly Levels Level`
                    * `readonly string From`
        * Events
            * `EventHandler<HistoricalLog> Logged`: Invoked when a new message is logged
        * Properties
            * `HistoricalLogs[] Logs`: History of logs
        * Methods
            * `void Log(string message, Levels level, object? from)`
    * `static class Main`
        * Members
            * `ObservableCollection<ConnectedPanel> ConnectedPanels`
            * `ObservableCollection<Profile> Profiles`
        * Properties
            * `bool IsInitialized`
            * `int SelectedProfileIndex`
            * `Profile? CurrentProfile`
            * `CancellationToken DeinitializedCancellationToken`
        * Events
            * `EventHandler Initialized`
            * `EventHandler Deinitialized`
        * Methods
            * `void Initialize()`
            * `void Handshake(IChannel channel)`
            * `async Task HandshakeAsync(IChannel channel)`
            * `void RefreshConnectedPanels()`
            * `async Task RefreshConnectedPanelAsync`
            * `void SendSourcesData()`
            * `async Task SendSourcesDataAsync()`
            * `void InterfaceUpdated(object sender, InterfaceUpdatedEventArgs args)`
            * `void Deinitialize()`
    * `static class Extensions`
        * Types
            * `enum ExtensionCategories`
                * `Generic`
                * `Channel`
                * `Action`
                * `Settable`
                * `Source`
        * Members
            * `ObservableCollection<Tuple<Type, MethodInfo, IChannel.Detect>> Detectors`
            * `Dictionary<ExtensionCategories, ObservableCollection<Type>> ExtensionsByCategory`
            * `ObservableCollection<object> GenericObjects`
        * Properties
            * `Type[] AllExtensions`
        * Methods
            * `ExtensionCategories GetExtensionCategory(this Type type)`
            * `void Load(Type type)`
            * `void Load<T>()`
            * `void Load(Assembly)`

## WPF Program: PanelControllerGUI
...