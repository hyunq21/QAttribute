namespace System
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class QBindingAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class QNoBindingAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class QVirtualAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class QEventAttribute : System.Attribute
    {
        public string PropertyChanging;
        public string PropertyChanged;
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class QJsonNameAttribute : System.Attribute
    {
        private string jsonName;
        public QJsonNameAttribute(string name)
        {
            this.jsonName = name;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class QCommandAttribute : System.Attribute
    {
    }
}
