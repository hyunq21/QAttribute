namespace System
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class QBindingAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class QNoBindingAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class QVirtualAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class QEventAttribute : System.Attribute
    {
        public string PropertyChanging;
        public string PropertyChanged;
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class QJsonNameAttribute : System.Attribute
    {
        private string jsonName;
        public QJsonNameAttribute(string name)
        {
            this.jsonName = name;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class QCommandAttribute : System.Attribute
    {
    }
}
