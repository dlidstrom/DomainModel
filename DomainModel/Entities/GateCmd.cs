namespace DomainModel.Entities
{
    public class GateCmd
    {
        public virtual int Id { get; private set; }
        public virtual string Name { get; set; }

        public override string ToString()
        {
            return string.Format("Id={0}, Name={1}", Id, Name);
        }
    }
}
