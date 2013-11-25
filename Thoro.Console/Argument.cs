
namespace Thoro.Console
{

    public class Argument
    {
        public string Name { get; set; }

        public Argument(string name)
        {
            this.Name = name;
        }

        public virtual bool Parse(string value)
        {
            return false;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
