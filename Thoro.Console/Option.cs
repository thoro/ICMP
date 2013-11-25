namespace Thoro.Console
{
    using System;
    using System.Collections.Generic;

    public class Option
    {
        private bool used;

        public event EventHandler IsUsed;

        public string Name { get; set; }

        public string HelpText { get; set; }

        public List<Argument> Arguments { get; set; }

        public bool Used
        {
            get
            {
                return this.used;
            }
            set
            {
                this.used = value;
                if (value && IsUsed != null)
                {
                    IsUsed(this, EventArgs.Empty);
                }
            }
        }

        public Option(string name)
        {
            this.Used = false;
            this.Name = name;
            this.Arguments = new List<Argument>();
        }

        public string Help()
        {
            return string.Format("{0,6} {1,-12} {2}", "-" + this.Name, string.Join(" ", this.Arguments), this.HelpText);
        }

        public override string ToString()
        {
            return string.Format("[-{0} {1}]", this.Name, string.Join(" ", this.Arguments));
        }
    }
}
