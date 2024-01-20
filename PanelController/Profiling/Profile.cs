using System.Data;

namespace PanelController.Profiling
{
    public class Profile
    {
        public string Name;

        public Dictionary<Guid, List<Mapping>> MappingsByGuid = new();

        public Guid[] Guids { get => MappingsByGuid.Keys.ToArray(); }

        public Mapping[] Mappings
        {
            get
            {
                List<Mapping> mappings = new();
                foreach (var mappingList in MappingsByGuid.Values)
                    mappings.AddRange(mappingList);
                return mappings.ToArray();
            }
        }

        public void AddMapping(Mapping mapping)
        {
            if (!MappingsByGuid.ContainsKey(mapping.PanelGuid))
                MappingsByGuid.Add(mapping.PanelGuid, new());
            else if (MappingsByGuid[mapping.PanelGuid].Contains(mapping))
                throw new InvalidOperationException("Mapping with interface already exists");
            MappingsByGuid[mapping.PanelGuid].Add(mapping);
        }

        public Mapping? FindMapping(Guid guid, InterfaceTypes interfaceType, uint interfaceID, object? interfaceOption = null)
        {
            if (!MappingsByGuid.ContainsKey(guid))
                return null;
            return MappingsByGuid[guid].Find(mapping => mapping == new Mapping() { PanelGuid = guid, InterfaceType = interfaceType, InterfaceID = interfaceID, InterfaceOption = interfaceOption });
        }

        public void RemoveMapping(Mapping mapping)
        {
            MappingsByGuid[mapping.PanelGuid].Remove(mapping);
        }
    }
}
