namespace System
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class QCloneAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class QNoCloneAttribute : System.Attribute
    {
    }
}
