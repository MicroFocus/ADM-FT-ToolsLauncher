namespace HpToolsLauncher.RTS
{
    /// <summary>
    /// Model class which describes the structure of an additional attribute
    /// </summary>
    public class AdditionalAttributeModel
    {
        private string name;
        private string value;
        private string description;

        public AdditionalAttributeModel(string name, string value, string description)
        {
            this.name = name;
            this.value = value;
            this.description = description;
        }

        public string GetName()
        {
            return name;
        }

        public string GetValue()
        {
            return value;
        }

        public string GetDescription()
        {
            return description;
        }
    }
}
