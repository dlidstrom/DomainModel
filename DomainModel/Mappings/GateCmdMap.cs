
using DomainModel.Entities;
using FluentNHibernate.Mapping;

namespace DomainModel.Mappings
{
    public class GateCmdMap : ClassMap<GateCmd>
    {
        public GateCmdMap()
        {
            Id(x => x.Id, "gate_command_id");
            Map(x => x.Name, "name");
        }
    }
}
